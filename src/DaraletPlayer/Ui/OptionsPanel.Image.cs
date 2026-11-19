using ImGuiNET;

namespace DaraletPlayer.Ui {
    /// <summary>
    /// The Image rows (Ui/DepthShading.Image.cs): anti-aliasing and sharpening of the 3D world, the game's own art seen
    /// more cleanly. Neither touches the interface.
    /// </summary>
    internal sealed partial class OptionsPanel {
        private int? draggingSharpen;
        private int? draggingGlow;
        private int? draggingAnisotropy;

        private void RenderImage() {
            var options = ImageSettings;
            Form.Label("Anti-aliasing", "Smooths the world's jagged edges. The interface is left alone.");

            if (ImGui.Checkbox("Smooth edges##image-antialias", ref options.AntiAlias)) {
                Changed();
            }

            Form.Label("Sharpen", "Brings back soft texture detail. Kept mild, it doesn't change the look.");
            var sharpen = draggingSharpen ?? options.ClampedSharpen;

            if (DragSlider("##image-sharpen", ref sharpen, 0, 100, sharpen == 0 ? "None" : "%d%%", ref draggingSharpen)) {
                options.Sharpen = sharpen;
                Changed();
            }

            Form.Label("Glow", "Fires, lanterns, crystals and portals glow softly in their own colour.");
            var glow = draggingGlow ?? options.ClampedGlow;

            if (DragSlider("##image-glow", ref glow, 0, 200, glow == 0 ? "None" : "%d%%", ref draggingGlow)) {
                options.Glow = glow;
                Changed();
            }

            Form.Label("Texture filtering", "Ground and water seen nearly edge-on stay sharp instead of smearing into "
                + "streaks (anisotropic filtering).");
            var anisotropy = draggingAnisotropy ?? options.ClampedAnisotropy;

            if (DragSlider("##image-anisotropy", ref anisotropy, 0, 16, anisotropy <= 1 ? "The game's" : "%dx",
                ref draggingAnisotropy)) {
                options.Anisotropy = anisotropy;
                Changed();
            }
        }
    }
}
