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
        private readonly TestRunner testRunner;

        /// <summary>Each draw distance slider's value while it's held: used once let go.</summary>
        private int? draggingScenery;
        private int? draggingDensity;
        private int? draggingLandscape;
        private int? draggingDetail;
        private int? draggingFar;
        private int? draggingFog;

        private void RenderDrawDistance() {
            Widgets.TextColored(HeadingColor, "Draw Distance (Daralet Toolkit)");
            var options = settings.Data.DrawDistance;

            if (drawDistance.Problem != null) {
                WarningText(drawDistance.Problem);
                return;
            }

            RenderMemoryWarning(options);

            if (Form.Check("Draw further than the game allows##draw-distance-on", ref options.On,
                "Uses the settings below instead of the game's. Costs frame rate and memory.")) {
                Changed();
            }

            if (!Form.BeginForm("draw-distance-form")) {
                return;
            }

            Form.Label("Frame Rate", "The game's frame rate, with before and after each change here. Also logged to "
                + "Frame Rate Logs.");
            frameRate.Window.RenderCompactControls("Shows the Frame Rate window.");

            ImGui.BeginDisabled(!options.On);

            Form.Label("Scenery", "How many landblocks out have trees. The game's is 1. Kept 2 inside Landscape.");
            var scenery = draggingScenery ?? options.ClampedSceneryRadius;

            // the setting is kept as chosen, so raising Landscape gives it back; the slider says what's used meanwhile
            var landscapeInUse = drawDistance.LandscapeFor(options);
            var allowed = DrawDistanceRamp.SceneryFor(scenery, landscapeInUse);
            var sceneryFormat = scenery == DrawDistanceOptions.GameSceneryRadius ? "1 (the game's)"
                : allowed < scenery ? $"%d ({allowed} with Landscape {landscapeInUse})" : "%d landblocks";

            if (DragSlider("##draw-distance-scenery", ref scenery, DrawDistanceOptions.GameSceneryRadius,
                DrawDistanceOptions.MaxSceneryRadius, sceneryFormat, ref draggingScenery)) {
                options.SceneryRadius = scenery;
                Changed();
            }

            Form.Label("Scenery density", "The share of trees, bushes and rocks drawn. Lower is the biggest frame rate "
                + "gain.");
            var density = draggingDensity ?? options.ClampedSceneryDensity;

            if (DragSlider("##draw-distance-density", ref density, DrawDistanceOptions.MinSceneryDensity, 100,
                density >= 100 ? "100%% (the game's)" : "%d%%", ref draggingDensity)) {
                options.SceneryDensity = density;
                Changed();
            }

            Form.Label("Landscape", "How many landblocks out the land is drawn. The game's goes up to 15.");
            var landscape = draggingLandscape ?? options.ClampedLandscapeRadius;

            if (DragSlider("##draw-distance-landscape", ref landscape, 0, DrawDistanceOptions.MaxLandscapeRadius,
                landscape == 0 ? "The game's" : "%d landblocks", ref draggingLandscape)) {
                options.LandscapeRadius = landscape < DrawDistanceOptions.MinLandscapeRadius && landscape > 0
                    ? DrawDistanceOptions.MinLandscapeRadius : landscape;
                Changed();
            }

            Form.Label("Detail", "How far out objects keep their finest model. Costly when raised.");
            var detail = draggingDetail ?? options.ClampedDetailDistance;

            if (DragSlider("##draw-distance-detail", ref detail, DrawDistanceOptions.MinDetailDistance,
                DrawDistanceOptions.MaxDetailDistance, detail == 0 ? "The game's" : "%d m", ref draggingDetail)) {
                options.DetailDistance = detail;
                Changed();
            }

            Form.Label("Far scenery", "How far out trees stay drawn, in their plainest model, instead of vanishing.");
            var far = draggingFar ?? options.ClampedLowDetailDistance;

            if (DragSlider("##draw-distance-far", ref far, 0, DrawDistanceOptions.MaxLowDetailDistance,
                far == 0 ? "The game's" : "%d m", ref draggingFar)) {
                options.LowDetailDistance = far;
                Changed();
            }

            Form.Label("Billboards", "Flat pictures of the trees past Scenery, out to Landscape's edge.");

            if (ImGui.Checkbox("Draw far trees as billboards##draw-distance-billboards", ref options.Billboards)) {
                Changed();
            }

            Form.Label("Fog", "How thick the distance fog is. Lower pushes it further out.");
            var fog = draggingFog ?? options.ClampedFog;

            if (DragSlider("##draw-distance-fog", ref fog, DrawDistanceOptions.MinFog, 100,
                fog >= 100 ? "100%% (the game's)" : "%d%%", ref draggingFog)) {
                options.Fog = fog;
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

                if (Form.FlowSmallButton(ref first, label, "Back to the last settings that loaded.")) {
                    options.SceneryRadius = scenery;
                    options.SceneryDensity = density;
                    options.LandscapeRadius = landscape;
                    Changed();
                }
            }

            if (Form.FlowSmallButton(ref first, "Game's own##draw-distance-game",
                "Scenery, density and Landscape back to the game's.")) {
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
            Widgets.Tip("Ctrl+click to type a number.");

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
