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
        /// <summary>
        /// Scenery rings added a step. 2 every 4 seconds held; the user found 1 to 13 in 25 seconds too slow, and 3 a
        /// second crashed the game at login on the step from 10 to 13 (288 blocks of trees, a second after the last
        /// step): "OUT OF MEMORY: Failed to allocate -1178753584 bytes", a size gone wrong, likely one load running into
        /// the last (November 2026). So 2, as soon as the last step's loading is done (MayStep).
        /// </summary>
        public const int SceneryStep = 2;

        /// <summary>Landscape rings added a step: the ground is lighter than its trees and buildings.</summary>
        public const int LandscapeStep = 3;

        /// <summary>The least seconds between steps.</summary>
        public const double StepSeconds = 1.5;

        /// <summary>A frame this slow is the game still loading.</summary>
        public const double HitchMs = 100;

        /// <summary>How long the game has to run with no frame that slow before the next step: the last one's done.</summary>
        public const double SettledSeconds = 1;

        /// <summary>
        /// Whether the next step may be taken: StepSeconds since the last, and SettledSeconds with no hitch, so a step
        /// never starts while the last is still loading. Behind another window the game runs at about 9 fps, every frame
        /// a "hitch", so stepping waits until it's in front again.
        /// </summary>
        public static bool MayStep(double secondsSinceStep, double secondsSinceHitch) {
            return secondsSinceStep >= StepSeconds && secondsSinceHitch >= SettledSeconds;
        }

        /// <summary>
        /// How many landblocks the scenery's full detail has to stop short of the landscape's edge. The game never draws
        /// its outer ring in full detail (its own least landscape is 3 with full detail out to 1): a full-detail block's
        /// edges are fitted to its neighbours' (CLandBlockStruct::AdjPlanes, CPolygon::make_plane at 0x5320F6), and past
        /// the edge there's no neighbour. Landscape 10 with scenery 14 crashed the game there (0x539153), with 1.7 GB
        /// free, November 2026.
        /// </summary>
        public const int SceneryInsideLandscape = 2;

        /// <summary>The scenery radius the landscape allows: at least two short of its edge, and never under the game's 1.</summary>
        public static int SceneryFor(int scenery, int landscape) {
            return Math.Max(1, Math.Min(scenery, landscape - SceneryInsideLandscape));
        }

        /// <summary>The next value on the way to the target: straight there going down, up by at most step going up.</summary>
        public static int Next(int current, int target, int step) {
            return target <= current ? target : Math.Min(target, current + Math.Max(1, step));
        }
    }
}
