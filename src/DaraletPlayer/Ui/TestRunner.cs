using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using DaraletPlayer.Feed;
using Decal.Adapter;

namespace DaraletPlayer.Ui {
    /// <summary>
    /// Runs a test script in the game (Feed/TestScript.cs says what a script holds), so a build can be checked without
    /// someone at the keyboard (the user's call, November 2026). The developer launches the game once from
    /// ShadowLauncher with a default character and Auto relaunch on; a build lands by the loader's hot reload; a script
    /// in Documents\Decal Plugins\Daralet Toolkit\Test Runs\run.txt is run once logged in, one step at a time on Decal's
    /// RenderFrame, and what happened goes in results.txt beside it, each line timed.
    /// <para>
    /// Crashes: the step being run is written to progress.txt before it starts and the file deleted when the script
    /// ends. Found at login (after Auto relaunch brought the game back), it means the game died on that step: that goes
    /// in the results and the script is dropped, so a step that crashes the game can't do it again and again.
    /// restart is the one deliberate exit: the steps after it are written back to run.txt, progress.txt deleted, and
    /// the game exited (the Options window's Exit Game, ClientMenu.ExitGame), so they run after the relaunch.
    /// </para>
    /// `[test-run]` lines in log.txt too. Nothing happens without a run.txt, so players never see any of it.
    /// </summary>
    internal sealed class TestRunner : IDisposable {
        private const double SettleQuietSeconds = 3;

        private static readonly string Folder = DataFolder.For("Test Runs");
        private static readonly string RunFile = Path.Combine(Folder, "run.txt");
        private static readonly string ResultsFile = Path.Combine(Folder, "results.txt");
        private static readonly string ProgressFile = Path.Combine(Folder, "progress.txt");

        private readonly Settings settings;
        private readonly DrawDistance drawDistance;
        private readonly Action changed;

        private List<TestStep> steps;
        private int index;
        private DateTime stepStarted;
        private DateTime atTargetSince = DateTime.MaxValue;
        private DateTime nextLookForRun;
        private bool disposed;

        /// <summary>
        /// Set by restart: the game is closing, so nothing more is run in it. The first build looked for run.txt again
        /// in the moment before the game closed, started the steps meant for after the relaunch, and the relaunched game
        /// then took the half-run step for a crash.
        /// </summary>
        private bool exiting;

        public TestRunner(Settings settings, DrawDistance drawDistance, Action changed) {
            this.settings = settings;
            this.drawDistance = drawDistance;
            this.changed = changed;
            CoreManager.Current.RenderFrame += Core_RenderFrame;
        }

        private void Core_RenderFrame(object sender, EventArgs e) {
            try {
                if (disposed || exiting || CoreManager.Current.CharacterFilter.LoginStatus != 3) {
                    return;
                }

                if (steps == null) {
                    LookForRun();
                    return;
                }

                Step();
            }
            catch (Exception ex) {
                Result("stopped: the runner threw " + ex.GetType().Name + ": " + ex.Message);
                PluginCore.LogFile("[test-run] " + ex);
                End();
            }
        }

        /// <summary>Once a second: a run.txt waiting, or a run the game died in.</summary>
        private void LookForRun() {
            if (DateTime.UtcNow < nextLookForRun) {
                return;
            }

            nextLookForRun = DateTime.UtcNow.AddSeconds(1);

            if (File.Exists(ProgressFile)) {
                var died = File.ReadAllText(ProgressFile).Trim();
                Result("the game stopped during: " + died + " (the script is dropped; see log.txt and the event log)");
                File.Delete(ProgressFile);

                if (File.Exists(RunFile)) {
                    File.Delete(RunFile);
                }

                return;
            }

            if (!File.Exists(RunFile)) {
                return;
            }

            steps = TestScript.Parse(File.ReadAllLines(RunFile));
            index = 0;
            stepStarted = DateTime.MinValue;
            Result($"started: {steps.Count} steps, {drawDistance.Loaded}, {settings.Data.DrawDistance.Summary()}");
        }

        private void Step() {
            if (index >= steps.Count) {
                Result("done");
                End();
                return;
            }

            var step = steps[index];
            var now = DateTime.UtcNow;

            if (stepStarted == DateTime.MinValue) {
                stepStarted = now;
                atTargetSince = DateTime.MaxValue;
                File.WriteAllText(ProgressFile, $"line {step.Line}: {step.Text}");

                if (!Begin(step)) {
                    return;
                }
            }

            if (!Finished(step, now)) {
                return;
            }

            index++;
            stepStarted = DateTime.MinValue;
        }

        /// <summary>A step's start; false when it ended the run (bad or restart).</summary>
        private bool Begin(TestStep step) {
            switch (step.Kind) {
                case "bad":
                    Result($"line {step.Line} can't be read ({step.Words}): stopped");
                    End();
                    return false;
                case "set":
                    Set(step.Field, step.Number);
                    Result($"set {step.Field} {step.Number.ToString(CultureInfo.InvariantCulture)}: "
                        + settings.Data.DrawDistance.Summary());
                    return true;
                case "option": {
                    var option = uint.Parse(step.Field);

                    if (step.Number < 0) {
                        Result($"option {option} is {ClientOptions.Get(option)?.ToString() ?? "unknown"}");
                        return true;
                    }

                    var set = ClientOptions.Set(option, step.Number != 0);
                    Result($"option {option} {(step.Number != 0 ? "on" : "off")}: "
                        + (set ? $"now {ClientOptions.Get(option)}" : "couldn't be set"));
                    return true;
                }
                case "chat":
                    CoreManager.Current.Actions.InvokeChatParser(step.Words);
                    Result("chat: " + step.Words);
                    return true;
                case "log":
                    Result(step.Words);
                    return true;
                case "restart":
                    Restart();
                    return false;
                case "compare":
                    Result(drawDistance.CompareScenery());
                    return true;
                case "screenshot": {
                    var name = string.Concat(step.Words.Split(Path.GetInvalidFileNameChars()));
                    var path = Path.Combine(Folder, "shots", $"{DateTime.Now:yyyy-MM-dd HH-mm-ss} {name}.png");
                    var problem = GameScreenshot.Save(path);
                    Result(problem == null ? "screenshot: " + path : "screenshot failed: " + problem);
                    return true;
                }
                default:
                    return true;
            }
        }

        /// <summary>Whether a step that takes time is done.</summary>
        private bool Finished(TestStep step, DateTime now) {
            var elapsed = (now - stepStarted).TotalSeconds;

            switch (step.Kind) {
                case "wait":
                    return elapsed >= step.Number;
                case "measure":
                    if (elapsed < step.Number) {
                        return false;
                    }

                    Measure(step.Number);
                    return true;
                case "settle":
                    // reached what's set, then a few quiet seconds for the last step's loading
                    if (drawDistance.AtTarget) {
                        atTargetSince = atTargetSince == DateTime.MaxValue ? now : atTargetSince;
                    }
                    else {
                        atTargetSince = DateTime.MaxValue;
                    }

                    if (atTargetSince != DateTime.MaxValue && (now - atTargetSince).TotalSeconds >= SettleQuietSeconds) {
                        Result($"settled after {elapsed:0} s: {drawDistance.Loaded}");
                        return true;
                    }

                    if (elapsed >= step.Number) {
                        Result($"didn't settle in {step.Number:0} s: {drawDistance.Loaded}");
                        return true;
                    }

                    return false;
                default:
                    return true;
            }
        }

        private void Set(string field, double number) {
            var options = settings.Data.DrawDistance;
            var value = (int)Math.Round(number);

            switch (field) {
                case "on":
                    options.On = value != 0;
                    break;
                case "scenery":
                    options.SceneryRadius = value;
                    break;
                case "density":
                    options.SceneryDensity = value;
                    break;
                case "landscape":
                    options.LandscapeRadius = value;
                    break;
                case "detail":
                    options.DetailDistance = value;
                    break;
                case "far":
                    options.LowDetailDistance = value;
                    break;
                case "fog":
                    options.Fog = value;
                    break;
                case "billboards":
                    options.Billboards = value != 0;
                    break;
            }

            changed();
        }

        private void Measure(double seconds) {
            var frames = FrameRateWindow.Recent(seconds);
            var rate = frames == null ? "no frame rate (Frame Rate window not running)"
                : $"{frames.Fps:0.0} fps, 1% low {frames.LowFps:0.0}, worst {frames.WorstMs:0} ms, {frames.Hitches} pauses";
            Result($"measure {seconds:0} s: {rate}; {DrawDistance.FreeMegabytes()} MB free; {drawDistance.Loaded}; "
                + drawDistance.BillboardState);
        }

        /// <summary>The steps after this one written back for after the relaunch, and the game exited.</summary>
        private void Restart() {
            var rest = steps.Skip(index + 1).Select(s => s.Text).ToList();

            if (rest.Count > 0) {
                File.WriteAllLines(RunFile, rest);
            }
            else if (File.Exists(RunFile)) {
                File.Delete(RunFile);
            }

            if (File.Exists(ProgressFile)) {
                File.Delete(ProgressFile);
            }

            Result($"restart: exiting the game, {rest.Count} steps left for after Auto relaunch");
            steps = null;
            exiting = true;
            ClientMenu.ExitGame();
        }

        /// <summary>The run over: its files cleared, ready for the next run.txt.</summary>
        private void End() {
            steps = null;

            foreach (var file in new[] { RunFile, ProgressFile }) {
                if (File.Exists(file)) {
                    File.Delete(file);
                }
            }
        }

        private static void Result(string line) {
            Directory.CreateDirectory(Folder);
            File.AppendAllText(ResultsFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {line}{Environment.NewLine}");
            PluginCore.LogFile("[test-run] " + line);
        }

        public void Dispose() {
            if (disposed) {
                return;
            }

            disposed = true;
            CoreManager.Current.RenderFrame -= Core_RenderFrame;

            // a hot reload (or the plugin stopping) mid-run isn't a crash: the step running and those after it are
            // handed to the next copy, which starts them afresh. The first build left progress.txt, and the new copy
            // dropped the script as if the game had died
            try {
                if (steps != null && !exiting) {
                    var rest = steps.Skip(index).Select(s => s.Text).ToList();
                    File.WriteAllLines(RunFile, rest);

                    if (File.Exists(ProgressFile)) {
                        File.Delete(ProgressFile);
                    }

                    Result($"the plugin stopped (a reload?): {rest.Count} steps handed on");
                }
            }
            catch (Exception ex) {
                PluginCore.LogFile("[test-run] " + ex);
            }
        }
    }
}
