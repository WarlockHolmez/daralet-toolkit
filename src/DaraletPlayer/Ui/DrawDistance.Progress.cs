using System;
using System.Numerics;
using Decal.Adapter;
using ImGuiNET;
using UtilityBelt.Service;
using UtilityBelt.Service.Views;

namespace DaraletPlayer.Ui {
    /// <summary>
    /// A loading bar while the draw distance steps up (the user's call, November 2026: "it's a bad UX how long it takes to
    /// load"): at the top of the screen, how far the landscape and scenery have come from where the step-up began, with
    /// what's loading in words; it says Done for a moment once there, then goes. Drawn from a hud of its own that takes no
    /// mouse (NoInputs), as CombatText's. Hidden with the plugin's windows (Hide game UI). Untested in the game when
    /// written.
    /// </summary>
    internal sealed partial class DrawDistance {
        private const ImGuiWindowFlags ProgressFlags = ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoTitleBar
            | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoSavedSettings
            | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNav;

        /// <summary>How long Done stays up.</summary>
        private const double DoneSeconds = 1.5;

        private Hud progressHud;
        private bool stepping;
        private int startScenery;
        private int startLandscape;
        private int progressScenery;
        private int progressLandscape;
        private int progressTargetScenery;
        private int progressTargetLandscape;
        private DateTime doneAt = DateTime.MinValue;

        /// <summary>From Apply: what's loaded now against what it's stepping up to.</summary>
        private void NoteProgress(int scenery, int landscape, int targetScenery, int targetLandscape, bool there) {
            if (there) {
                if (stepping) {
                    stepping = false;
                    doneAt = DateTime.UtcNow;
                    progressScenery = targetScenery;
                    progressLandscape = targetLandscape;
                }

                return;
            }

            var goingUp = targetScenery > scenery || targetLandscape > landscape;

            if (!goingUp) {
                return;
            }

            // a new target that's further (or the first) starts the bar from where things are now
            if (!stepping || targetScenery != progressTargetScenery || targetLandscape != progressTargetLandscape) {
                if (!stepping) {
                    startScenery = scenery;
                    startLandscape = landscape;
                }

                stepping = true;
                doneAt = DateTime.MinValue;
            }

            progressScenery = scenery;
            progressLandscape = landscape;
            progressTargetScenery = targetScenery;
            progressTargetLandscape = targetLandscape;
        }

        /// <summary>From RenderFrame: the bar's hud made once, shown while there's something to show.</summary>
        private void UpdateProgressHud() {
            var showing = (stepping || (DateTime.UtcNow - doneAt).TotalSeconds < DoneSeconds)
                && !HideGameUi.PluginWindowsHidden && CoreManager.Current.CharacterFilter.LoginStatus == 3;

            if (progressHud == null) {
                if (!showing) {
                    return;
                }

                progressHud = UBService.Huds.CreateHud("Draw distance loading");
                progressHud.ShowInBar = false;
                progressHud.OnPreRender += ProgressHud_OnPreRender;
                progressHud.OnRender += ProgressHud_OnRender;
            }

            if (progressHud.Visible != showing) {
                progressHud.Visible = showing;
            }
        }

        private void ProgressHud_OnPreRender(object sender, EventArgs e) {
            try {
                progressHud.WindowSettings = ProgressFlags;
                ImGui.SetNextWindowPos(Vector2.Zero, ImGuiCond.Always);
                ImGui.SetNextWindowSize(Vector2.One, ImGuiCond.Always);
            }
            catch (Exception ex) {
                PluginCore.LogFile("[draw-distance] " + ex);
            }
        }

        private void ProgressHud_OnRender(object sender, EventArgs e) {
            try {
                if (disposed) {
                    return;
                }

                var total = Math.Max(0, progressTargetScenery - startScenery)
                    + Math.Max(0, progressTargetLandscape - startLandscape);
                var done = Math.Max(0, progressScenery - startScenery) + Math.Max(0, progressLandscape - startLandscape);
                var fraction = !stepping ? 1f : total <= 0 ? 0f : Math.Min(1f, (float)done / total);
                var text = !stepping ? "Draw distance loaded"
                    : done == 0 ? "Draw distance: waiting for the area to load..."
                    : $"Loading draw distance: landscape {progressLandscape} of {progressTargetLandscape}, "
                        + $"scenery {progressScenery} of {progressTargetScenery}";

                var screen = ImGui.GetIO().DisplaySize;
                var line = ImGui.GetTextLineHeight();
                var width = Math.Max(ImGui.CalcTextSize(text).X + line * 2, line * 22);
                var top = new Vector2((screen.X - width) / 2, line * 3);
                var barTop = top + new Vector2(line * 0.5f, line * 1.6f);
                var barEnd = new Vector2(top.X + width - line * 0.5f, barTop.Y + line * 0.5f);
                var draw = ImGui.GetForegroundDrawList();
                var rounding = line * 0.25f;

                draw.AddRectFilled(top, new Vector2(top.X + width, barEnd.Y + line * 0.5f),
                    ImGui.GetColorU32(new Vector4(0.05f, 0.04f, 0.03f, 0.75f)), rounding);
                draw.AddText(top + new Vector2(line, line * 0.3f), ImGui.GetColorU32(Colors.Text), text);
                draw.AddRectFilled(barTop, barEnd, ImGui.GetColorU32(new Vector4(1, 1, 1, 0.12f)), rounding);
                draw.AddRectFilled(barTop, new Vector2(barTop.X + (barEnd.X - barTop.X) * fraction, barEnd.Y),
                    ImGui.GetColorU32(Colors.Warning), rounding);
            }
            catch (Exception ex) {
                PluginCore.LogFile("[draw-distance] " + ex);
            }
        }

        private void DisposeProgress() {
            if (progressHud == null) {
                return;
            }

            progressHud.OnPreRender -= ProgressHud_OnPreRender;
            progressHud.OnRender -= ProgressHud_OnRender;
            progressHud.Visible = false;
            progressHud.Dispose();
            progressHud = null;
        }
    }
}
