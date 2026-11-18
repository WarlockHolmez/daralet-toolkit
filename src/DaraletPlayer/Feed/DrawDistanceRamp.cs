using System;

namespace DaraletPlayer.Feed {
    /// <summary>
    /// Raising the draw distance a step at a time (Ui/DrawDistance.cs). Loading a lot the game has never read crashed it,
    /// with memory to spare: at login, on a teleport, and on going from scenery 1 straight to 16, each time in its own
    /// code reading the dats (CCellStruct::UnPack's CPolygon::UnPack at 0x5393C6 and 0x5393E4 running past its data,
    /// CPolygon::make_plane at 0x539153; November 2026). Raised 5, 7, 10, 15, 16, a step every few seconds, it held:
    /// each step only reads one more ring, the rest still in the game's cache. So a raise goes up a few rings a step,
    /// and a lowering goes all the way at once. Kept apart so it can be tested.
    /// </summary>
    public static class DrawDistanceRamp {
        /// <summary>Scenery rings added a step.</summary>
        public const int SceneryStep = 2;

        /// <summary>Landscape rings added a step: the ground is lighter than its trees and buildings.</summary>
        public const int LandscapeStep = 3;

        /// <summary>Seconds between steps, for each one's loading to finish.</summary>
        public const double StepSeconds = 4;

        /// <summary>The next value on the way to the target: straight there going down, up by at most step going up.</summary>
        public static int Next(int current, int target, int step) {
            return target <= current ? target : Math.Min(target, current + Math.Max(1, step));
        }
    }
}
