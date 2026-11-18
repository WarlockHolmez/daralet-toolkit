using System.Runtime.Serialization;

namespace DaraletPlayer.Feed {
    /// <summary>
    /// Draw distance past the game's own settings (Ui/DrawDistance.cs), in settings.json as "drawDistance". Off to start;
    /// each value's 0 (or the game's 1 for scenery) leaves the game's own.
    /// </summary>
    [DataContract]
    public class DrawDistanceOptions {
        /// <summary>The game's own scenery radius: only the landblock you're in and the eight round it have trees.</summary>
        public const int GameSceneryRadius = 1;
        /// <summary>
        /// 5 at first; raised to see how far it goes with scenery density down (the user's call, November 2026). The
        /// rings patched are R, R+1 and R+3, each a signed byte in a cmp, so up to 124 would fit; the landscape's own
        /// radius (MaxLandscapeRadius) is the real limit, and memory before it (DrawDistance.Memory.cs).
        /// </summary>
        public const int MaxSceneryRadius = 16;

        /// <summary>The game's Landscape Draw Distance choices reach a radius of 3, 5, 8, 11 or 15 landblocks.</summary>
        public const int MinLandscapeRadius = 3;
        public const int MaxLandscapeRadius = 24;

        public const int MinDetailDistance = 0;
        public const int MaxDetailDistance = 1000;
        public const int MaxLowDetailDistance = 3000;

        /// <summary>Whether any of this is used: off, the game's own settings are, as if the plugin weren't there.</summary>
        [DataMember(Name = "on")] public bool On;

        /// <summary>
        /// How many landblocks out from yours are drawn in full detail, with their trees, bushes and rocks: the game's is
        /// 1 (yours and the eight round it). The rings past it, in less detail and with no scenery, move out with it.
        /// </summary>
        [DataMember(Name = "sceneryRadius")] public int SceneryRadius;

        /// <summary>How many landblocks out the landscape is drawn; 0 for the game's Landscape Draw Distance.</summary>
        [DataMember(Name = "landscapeRadius")] public int LandscapeRadius;

        /// <summary>
        /// The game's Degrade Distance, in meters, past what its slider allows: objects keep their detail this much
        /// further out. 0 for the game's own.
        /// </summary>
        [DataMember(Name = "detailDistance")] public int DetailDistance;

        /// <summary>
        /// How far out, in meters, objects the game would stop drawing are drawn with their cheapest model instead
        /// (Ui/DrawDistance.Lod.cs): far trees stay, without Detail's cost of finer models everywhere. 0 for none.
        /// </summary>
        [DataMember(Name = "lowDetailDistance")] public int LowDetailDistance;

        /// <summary>
        /// The percent of the game's trees, bushes and rocks made (Ui/DrawDistance.Density.cs), for frame rate: 100 for
        /// all of them, the game's. Buildings and placed objects always stay.
        /// </summary>
        [DataMember(Name = "sceneryDensity")] public int SceneryDensity;

        public const int MinSceneryDensity = 10;

        /// <summary>
        /// Flat pictures of the trees past the scenery radius, out to the landscape's edge (Ui/DrawDistance.Billboards.cs).
        /// </summary>
        [DataMember(Name = "billboards")] public bool Billboards;

        // Settings that came and went in November 2026, still in a settings.json saved then and read by nothing (the
        // reader skips names it doesn't know): "modelDetail"..., three detail bands, and "levelDistance1"..., three
        // model level distances, which "detailFalloff" replaced, then that too (with Far scenery on, a tree's one real
        // model came back as its plainest, so it changed nothing; wanted again once far trees can be billboards);
        // "sceneryCutOff", "farThinFrom", "farThinPercent", "slowModelsFrom" and "slowModelsEvery", whose hooks only
        // ever caught the sky (thinning it turned it black), never a tree. CLAUDE.md has how each worked.

        public int ClampedSceneryDensity => SceneryDensity <= 0 || SceneryDensity >= 100 ? 100
            : SceneryDensity < MinSceneryDensity ? MinSceneryDensity : SceneryDensity;

        public DrawDistanceOptions() {
            SetDefaults();
        }

        public int ClampedSceneryRadius => SceneryRadius < GameSceneryRadius ? GameSceneryRadius
            : SceneryRadius > MaxSceneryRadius ? MaxSceneryRadius : SceneryRadius;

        /// <summary>The landscape radius wanted, or 0 for the game's own.</summary>
        public int ClampedLandscapeRadius => LandscapeRadius <= 0 ? 0
            : LandscapeRadius < MinLandscapeRadius ? MinLandscapeRadius
            : LandscapeRadius > MaxLandscapeRadius ? MaxLandscapeRadius : LandscapeRadius;

        public int ClampedDetailDistance => DetailDistance <= 0 ? 0
            : DetailDistance > MaxDetailDistance ? MaxDetailDistance : DetailDistance;

        public int ClampedLowDetailDistance => LowDetailDistance <= 0 ? 0
            : LowDetailDistance > MaxLowDetailDistance ? MaxLowDetailDistance : LowDetailDistance;

        /// <summary>The settings in a line, for the Frame Rate window's marks: "Scenery 5, landscape 24, ...".</summary>
        public string Summary() {
            if (!On) {
                return "Draw distance off";
            }

            return $"Scenery {ClampedSceneryRadius} at {ClampedSceneryDensity}%, landscape "
                + (ClampedLandscapeRadius == 0 ? "game's" : ClampedLandscapeRadius.ToString())
                + $", detail {(ClampedDetailDistance == 0 ? "game's" : ClampedDetailDistance + " m")}"
                + $", far {ClampedLowDetailDistance} m" + (Billboards ? ", billboards" : "");
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context) {
            SetDefaults();
        }

        private void SetDefaults() {
            On = false;
            SceneryRadius = GameSceneryRadius;
            LandscapeRadius = 0;
            DetailDistance = 0;
            LowDetailDistance = 0;
            SceneryDensity = 100;
            Billboards = false;
        }
    }
}
