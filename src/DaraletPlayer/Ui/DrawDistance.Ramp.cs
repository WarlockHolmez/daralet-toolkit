using System;
using DaraletPlayer.Feed;
using Decal.Adapter;
using Decal.Adapter.Wrappers;

namespace DaraletPlayer.Ui {
    /// <summary>
    /// The draw distance raised a step at a time (Feed/DrawDistanceRamp.cs, which says why): at login, after a teleport,
    /// and whenever a setting goes up. In portal space it goes back to the game's own (Decal's ChangePortalMode), so the
    /// new place loads at the game's size, and steps up once you're there. `[draw-distance]` lines in log.txt.
    /// Untested in the game when written.
    /// </summary>
    internal sealed partial class DrawDistance {
        /// <summary>Whether you're in portal space: the far settings wait until you're out.</summary>
        private bool inPortal;

        /// <summary>When the next step up may be taken.</summary>
        private DateTime nextStepAt = DateTime.MinValue;

        /// <summary>When you last logged in or came out of a portal: steps are small for a while after.</summary>
        private DateTime arrivedAt = DateTime.UtcNow;

        private bool wasLoggedIn;

        /// <summary>When the last step up was taken, and when the game last took over HitchMs over a frame.</summary>
        private DateTime lastStepAt = DateTime.MinValue;
        private DateTime lastHitchAt = DateTime.MinValue;
        private readonly System.Diagnostics.Stopwatch frameClock = System.Diagnostics.Stopwatch.StartNew();
        private double lastFrameMs = -1;

        /// <summary>
        /// Each of Decal's RenderFrames is one of the game's frames: a slow one means it's still loading (or behind
        /// another window), and the next step waits for a second without one.
        /// </summary>
        private void NoteFrame() {
            NoteFrameTime();
        }

        /// <summary>
        /// When the last landscape load began (a whole reload or a refresh in place); at first, when this copy of the
        /// plugin started, as a hot reload follows the old copy's own reload at shutdown.
        /// </summary>
        private DateTime lastLoadAt = DateTime.UtcNow;

        /// <summary>How long after a load the next may begin, at least.</summary>
        private const double LoadQuietSeconds = 2;

        /// <summary>
        /// Whether a landscape load may begin: LoadQuietSeconds since the last, and a second with no slow frame. Every
        /// black screen (the game drawing nothing, its landscape empty, a crash in make_plane later) came from two loads
        /// close together: brought down in place then reloaded the next frame; and after a hot reload, the old copy's
        /// reload at shutdown then the new copy's for a density it took for changed (November 2026: "0 in full detail"
        /// at every look after, the screenshot all black).
        /// </summary>
        private bool LoadSettled() {
            var now = DateTime.UtcNow;
            return (now - lastLoadAt).TotalSeconds >= LoadQuietSeconds
                && (now - lastHitchAt).TotalSeconds >= DrawDistanceRamp.SettledSeconds;
        }

        private void NoteFrameTime() {
            var ms = frameClock.Elapsed.TotalMilliseconds;

            if (lastFrameMs >= 0 && ms - lastFrameMs > DrawDistanceRamp.HitchMs) {
                lastHitchAt = DateTime.UtcNow;
            }

            lastFrameMs = ms;
        }

        private void ListenForPortals() {
            CoreManager.Current.CharacterFilter.ChangePortalMode += CharacterFilter_ChangePortalMode;
        }

        private void StopListeningForPortals() {
            CoreManager.Current.CharacterFilter.ChangePortalMode -= CharacterFilter_ChangePortalMode;
        }

        /// <summary>
        /// Whether portal space takes the draw distance back to the game's (a test run's `set portalreload 0` turns it off,
        /// to see whether that reload, in the same second as the game's own load of where you're going, is what
        /// crashed a teleport, November 2026).
        /// </summary>
        internal static bool PortalReload = true;

        private void CharacterFilter_ChangePortalMode(object sender, ChangePortalModeEventArgs e) {
            try {
                if (!PortalReload) {
                    PluginCore.LogFile("[draw-distance] portal space: left as it is (portal reload off)");
                    return;
                }

                inPortal = e.Type == PortalEventType.EnterPortal;

                // out of portal space, the place you've come to loads at the game's size first, then steps up, gently
                // while the world streams in (DrawDistanceRamp.Cautious...)
                arrivedAt = DateTime.UtcNow;
                nextStepAt = arrivedAt.AddSeconds(DrawDistanceRamp.CautiousWaitSeconds);
                PluginCore.LogFile(inPortal ? "[draw-distance] portal space: the game's own until you're out"
                    : "[draw-distance] out of portal space: stepping up again");
                Soon();
            }
            catch (Exception ex) {
                PluginCore.LogFile("[draw-distance] " + ex);
            }
        }

        /// <summary>
        /// This look's scenery and landscape radius on the way to the targets: down at once, up a step at a time, no
        /// sooner than StepSeconds after the last.
        /// </summary>
        private (int Scenery, int Landscape) NextStep(int scenery, int targetScenery, int landscape,
            int targetLandscape) {
            var goingUp = targetScenery > scenery || targetLandscape > landscape;
            var now = DateTime.UtcNow;

            // login: the moment the character's in, the same gentle start as out of a portal
            var loggedIn = CoreManager.Current.CharacterFilter.LoginStatus == 3;

            if (loggedIn && !wasLoggedIn) {
                arrivedAt = now;
                nextStepAt = now.AddSeconds(DrawDistanceRamp.CautiousWaitSeconds);
                PluginCore.LogFile("[draw-distance] logged in: stepping up gently");
            }

            wasLoggedIn = loggedIn;
            var cautious = (now - arrivedAt).TotalSeconds < DrawDistanceRamp.CautiousSeconds;

            // the last step's loading done: its time gone by, and a second without a slow frame
            var settled = now >= nextStepAt && DrawDistanceRamp.MayStep(
                (now - lastStepAt).TotalSeconds, (now - lastHitchAt).TotalSeconds);

            if (goingUp && !settled) {
                return (Math.Min(scenery, targetScenery), Math.Min(landscape, targetLandscape));
            }

            var next = (
                DrawDistanceRamp.Next(scenery, targetScenery,
                    cautious ? DrawDistanceRamp.CautiousSceneryStep : DrawDistanceRamp.SceneryStep),
                DrawDistanceRamp.Next(landscape, targetLandscape,
                    cautious ? DrawDistanceRamp.CautiousLandscapeStep : DrawDistanceRamp.LandscapeStep));
            next = DrawDistanceRamp.OneAtATime(scenery, landscape, next.Item1, next.Item2);

            if (next.Item1 > scenery || next.Item2 > landscape) {
                lastStepAt = now;
                nextStepAt = now.AddSeconds(DrawDistanceRamp.StepSeconds);

                if (next.Item1 != targetScenery || next.Item2 != targetLandscape) {
                    PluginCore.LogFile($"[draw-distance] stepping up: scenery {next.Item1} of {targetScenery}, "
                        + $"landscape {next.Item2} of {targetLandscape}");
                }
            }

            return next;
        }
    }
}
