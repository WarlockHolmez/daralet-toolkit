using System;
using DaraletPlayer.Feed;
using ImGuiNET;

namespace DaraletPlayer.Ui {
    /// <summary>
    /// The Config page's Draw Distance group, the plugin's own (DrawDistance): scenery further out than the game makes
    /// it, a landscape radius past the game's choices, and a degrade distance past its slider. Changes are used once a
    /// slider's let go, not while it's dragged, since each loads the landscape again; they don't wait for Apply, which is
    /// the game's own preferences'.
    /// </summary>
    internal sealed partial class OptionsPanel {
        private readonly DrawDistance drawDistance;
        private readonly FrameRateWindow frameRate;

        /// <summary>Each draw distance slider's value while it's held: used once let go.</summary>
        private int? draggingScenery;
        private int? draggingDensity;
        private int? draggingLandscape;
        private int? draggingDetail;
        private int? draggingFar;

        private void RenderDrawDistance() {
            Widgets.TextColored(HeadingColor, "Draw Distance (Daralet Toolkit)");
            var options = settings.Data.DrawDistance;

            if (drawDistance.Problem != null) {
                WarningText(drawDistance.Problem);
                return;
            }

            RenderMemoryWarning(options);

            if (Form.Check("Draw further than the game allows##draw-distance-on", ref options.On, "Uses the "
                + "settings below in place of the game's own. Off, everything is as the game has it. Drawing further "
                + "costs frame rate and memory: try a step at a time. Changing these loads the landscape again, a "
                + "moment's pause.")) {
                Changed();
            }

            if (!Form.BeginForm("draw-distance-form")) {
                return;
            }

            Form.Label("Frame Rate", "A window of the game's frame rate: now, over the last ten seconds with its 1% "
                + "low, a graph of the last minute, and before and after each change you make here, so you can tell "
                + "whether a setting helps. Every second is also written to a .csv in Documents\\Decal Plugins\\"
                + "Daralet Toolkit\\Frame Rate Logs, with the settings at the time, to look through afterwards.");
            frameRate.Window.RenderCompactControls("Shows the Frame Rate window.");

            ImGui.BeginDisabled(!options.On);

            Form.Label("Scenery", "How many landblocks out from yours have their trees, bushes and rocks, drawn in full "
                + "detail. The game's is 1: yours and the eight round it, so past about 200 to 400 meters the land is "
                + "bare. Each step out is many more objects to draw: 2 is 25 landblocks, 3 is 49, 5 is 121, 10 is 441, "
                + "16 is 1,089. Past 5, lower Scenery density with it, and keep Landscape at least as far, or the "
                + "scenery has no land to stand on. If the game runs low on memory, it's put back by itself.");
            var scenery = draggingScenery ?? options.ClampedSceneryRadius;

            if (DragSlider("##draw-distance-scenery", ref scenery, DrawDistanceOptions.GameSceneryRadius,
                DrawDistanceOptions.MaxSceneryRadius, scenery == DrawDistanceOptions.GameSceneryRadius ? "1 (the game's)"
                    : "%d landblocks", ref draggingScenery)) {
                options.SceneryRadius = scenery;
                Changed();
            }

            Form.Label("Scenery density", "How many of the game's trees, bushes and rocks are made, as a percent: fewer "
                + "objects is the biggest gain in frame rate, much more than plainer models. The same ones always go, "
                + "spread evenly, and it's the same everywhere, the landblock you're in too. Buildings and other placed "
                + "objects always stay.");
            var density = draggingDensity ?? options.ClampedSceneryDensity;

            if (DragSlider("##draw-distance-density", ref density, DrawDistanceOptions.MinSceneryDensity, 100,
                density >= 100 ? "100%% (the game's)" : "%d%%", ref draggingDensity)) {
                options.SceneryDensity = density;
                Changed();
            }

            Form.Label("Landscape", "How many landblocks out the land is drawn at all. The game's Landscape Draw "
                + "Distance reaches 3, 5, 8, 11 or 15; this goes up to 24. Left at the start, the game's own is used.");
            var landscape = draggingLandscape ?? options.ClampedLandscapeRadius;

            if (DragSlider("##draw-distance-landscape", ref landscape, 0, DrawDistanceOptions.MaxLandscapeRadius,
                landscape == 0 ? "The game's" : "%d landblocks", ref draggingLandscape)) {
                options.LandscapeRadius = landscape < DrawDistanceOptions.MinLandscapeRadius && landscape > 0
                    ? DrawDistanceOptions.MinLandscapeRadius : landscape;
                Changed();
            }

            Form.Label("Detail", "How much further out objects keep their detail, in meters: the game's Degrade "
                + "Distance, past what its slider allows. Left at the start, the game's own is used.");
            var detail = draggingDetail ?? options.ClampedDetailDistance;

            if (DragSlider("##draw-distance-detail", ref detail, DrawDistanceOptions.MinDetailDistance,
                DrawDistanceOptions.MaxDetailDistance, detail == 0 ? "The game's" : "%d m", ref draggingDetail)) {
                options.DetailDistance = detail;
                Changed();
            }

            Form.Label("Far scenery", "How far out, in meters, trees, rocks and other objects the game would stop "
                + "drawing are drawn with their plainest model instead. Much cheaper than Detail, which draws everything "
                + "out to its distance in finer models: leave Detail at the game's and raise this with Scenery. Left at "
                + "the start, the game hides them as ever.");
            var far = draggingFar ?? options.ClampedLowDetailDistance;

            if (DragSlider("##draw-distance-far", ref far, 0, DrawDistanceOptions.MaxLowDetailDistance,
                far == 0 ? "The game's" : "%d m", ref draggingFar)) {
                options.LowDetailDistance = far;
                Changed();
            }

            ImGui.EndDisabled();
            ImGui.EndTable();
        }

        /// <summary>
        /// The game short of memory: why, wrapped to the window, and buttons that turn the far settings down (the
        /// user's call, November 2026): back to the last ones that loaded before those that ran out, or the game's own.
        /// </summary>
        private void RenderMemoryWarning(DrawDistanceOptions options) {
            var warning = drawDistance.MemoryWarning;

            if (warning == null) {
                return;
            }

            WarningText(warning);
            var first = true;
            var fallBack = drawDistance.FallBack;

            if (fallBack.HasValue) {
                var (scenery, density, landscape) = fallBack.Value;
                var label = $"Back to scenery {scenery} at {density}%"
                    + (landscape > 0 ? $", landscape {landscape}" : "") + "##draw-distance-fallback";

                if (Form.FlowSmallButton(ref first, label, "Sets Scenery, Scenery density and Landscape back to the "
                    + "last settings that loaded before these, and loads the landscape again.")) {
                    options.SceneryRadius = scenery;
                    options.SceneryDensity = density;
                    options.LandscapeRadius = landscape;
                    Changed();
                }
            }

            if (Form.FlowSmallButton(ref first, "Game's own##draw-distance-game", "Sets Scenery, Scenery density and "
                + "Landscape back to the game's own (the other settings stay), and loads the landscape again.")) {
                options.SceneryRadius = DrawDistanceOptions.GameSceneryRadius;
                options.SceneryDensity = 100;
                options.LandscapeRadius = 0;
                Changed();
            }
        }

        private static void WarningText(string text) {
            ImGui.PushStyleColor(ImGuiCol.Text, Colors.Warning);
            Widgets.TextWrapped(text);
            ImGui.PopStyleColor();
        }

        /// <summary>A slider whose value is only given (true) once it's let go; while held, it's kept in dragging.</summary>
        private static bool DragSlider(string id, ref int value, int min, int max, string format, ref int? dragging,
            float width = 0) {
            ImGui.SetNextItemWidth(width > 0 ? width : Form.ControlWidth);
            Sliders.SliderInt(id, ref value, min, max, format);
            Widgets.Tip("Used once you let go. Ctrl+click to type a number.");

            if (ImGui.IsItemActive()) {
                dragging = value;
                return false;
            }

            if (!dragging.HasValue) {
                return false;
            }

            value = dragging.Value;
            dragging = null;
            return true;
        }

        private void Changed() {
            settings.Changed();
            drawDistance.Soon();
            FrameRateWindow.Mark(settings.Data.DrawDistance.Summary());
        }
    }
}
