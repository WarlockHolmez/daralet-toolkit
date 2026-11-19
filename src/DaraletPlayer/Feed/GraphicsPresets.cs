namespace DaraletPlayer.Feed {
    /// <summary>
    /// Ready-made draw distances (the user's call, November 2026): Scenery is kept at 1 or 2, the game's trees being the
    /// cost of both kinds, and the budget goes to Ground detail (roads and shores, frame rate) and Landscape (how far,
    /// memory). Each keeps 800 MB of the game's memory spare by the estimate (GraphicsCost). Retail is the game's own.
    /// The rest of the draw distance (Detail, Far scenery, Fog) is left as it is.
    /// </summary>
    public static class GraphicsPresets {
        public sealed class Preset {
            public string Name;
            public string Tip;
            public int Scenery;
            public int Ground;
            public int Landscape;

            /// <summary>Retail: the plugin's draw distance off.</summary>
            public bool Off;
        }

        public static readonly Preset[] All = {
            new Preset { Name = "Retail", Tip = "The game's own draw distance.", Off = true },
            new Preset {
                Name = "Performance", Scenery = 1, Ground = 6, Landscape = 16,
                Tip = "The game's trees, roads and shores to 6 landblocks, land to 16.",
            },
            new Preset {
                Name = "Balanced", Scenery = 2, Ground = 6, Landscape = 20,
                Tip = "Trees to 2 landblocks, roads and shores to 6, land to 20.",
            },
            new Preset {
                Name = "Quality", Scenery = 2, Ground = 8, Landscape = 20,
                Tip = "Trees to 2 landblocks, roads and shores to 8, land to 20.",
            },
        };

        /// <summary>Puts a preset's settings on (3D trees, billboards and far buildings with it).</summary>
        public static void Apply(Preset preset, DrawDistanceOptions o) {
            if (preset.Off) {
                o.On = false;
                return;
            }

            o.On = true;
            o.SceneryRadius = preset.Scenery;
            o.SceneryDensity = 100;
            o.GroundRadius = preset.Ground;
            o.LandscapeRadius = preset.Landscape;
            o.FarTrees = 2;
            o.Billboards = true;
            o.FarBuildings = true;
        }

        /// <summary>The preset these settings are, or null for Custom.</summary>
        public static Preset Matching(DrawDistanceOptions o) {
            foreach (var preset in All) {
                if (preset.Off ? !o.On
                    : o.On && o.ClampedSceneryRadius == preset.Scenery && o.ClampedSceneryDensity == 100
                    && o.ClampedGroundRadius == preset.Ground && o.ClampedLandscapeRadius == preset.Landscape
                    && o.ClampedFarTrees == 2 && o.Billboards && o.FarBuildings) {
                    return preset;
                }
            }

            return null;
        }
    }
}
