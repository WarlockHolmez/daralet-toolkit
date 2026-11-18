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

        private void ListenForPortals() {
            CoreManager.Current.CharacterFilter.ChangePortalMode += CharacterFilter_ChangePortalMode;
        }

        private void StopListeningForPortals() {
            CoreManager.Current.CharacterFilter.ChangePortalMode -= CharacterFilter_ChangePortalMode;
        }

        private void CharacterFilter_ChangePortalMode(object sender, ChangePortalModeEventArgs e) {
            try {
                inPortal = e.Type == PortalEventType.EnterPortal;

                // out of portal space, the place you've come to loads at the game's size first, then steps up
                nextStepAt = DateTime.UtcNow.AddSeconds(DrawDistanceRamp.StepSeconds);
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

            if (goingUp && DateTime.UtcNow < nextStepAt) {
                return (Math.Min(scenery, targetScenery), Math.Min(landscape, targetLandscape));
            }

            var next = (DrawDistanceRamp.Next(scenery, targetScenery, DrawDistanceRamp.SceneryStep),
                DrawDistanceRamp.Next(landscape, targetLandscape, DrawDistanceRamp.LandscapeStep));

            if (next.Item1 > scenery || next.Item2 > landscape) {
                nextStepAt = DateTime.UtcNow.AddSeconds(DrawDistanceRamp.StepSeconds);

                if (next.Item1 != targetScenery || next.Item2 != targetLandscape) {
                    PluginCore.LogFile($"[draw-distance] stepping up: scenery {next.Item1} of {targetScenery}, "
                        + $"landscape {next.Item2} of {targetLandscape}");
                }
            }

            return next;
        }
    }
}
