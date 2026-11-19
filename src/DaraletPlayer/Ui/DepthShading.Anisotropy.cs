using Microsoft.DirectX.Direct3D;

namespace DaraletPlayer.Ui {
    /// <summary>
    /// Anisotropic filtering for the game's own textures (ImageOptions.Anisotropy; the user's call, November 2026: "why is
    /// the background water so distorted?"). Ground and water seen nearly edge-on smear into streaks: a screen pixel there
    /// covers a long thin stretch of the texture, which plain filtering blurs as if it were square. The game's own drawing
    /// did it at its own draw distance as much as at ours (seen in the game).
    /// <para>
    /// The game has it built in: RenderDeviceD3D::SetSamplerFilterMode (0x5A4460), which it calls for every draw, turns a
    /// linear filter into anisotropic when the texture filtering preference (the int at 0x81FFA0, registered with the
    /// client's preferences at start, 0x54F6B8, and set by the graphics presets, Render::SetOverallGraphicsQuality) is 3
    /// and the card can; setting the samplers' filter from outside did nothing, the game setting its own on every draw.
    /// So the preference is set to 3 while this is on (the old value kept and put back when it goes off or the plugin
    /// stops), and the samplers' MaxAnisotropy, which the game only sets when it makes the device (0x5A5347, from its own
    /// field), is set each frame: as many samples as the card offers, at most the setting. Untested in the game when
    /// written.
    /// </para>
    /// </summary>
    internal sealed partial class DepthShading {
        private const int AnisotropicStages = 4;
        private const int TextureFilterPreference = 0x81FFA0;
        private const int AnisotropicFilter = 3;

        private int appliedAnisotropy;
        private int gameTextureFilter = -1;

        private unsafe void ApplyAnisotropy(Device device) {
            var wanted = settings.Data.Image.ClampedAnisotropy;

            if (wanted == 0) {
                PutBackTextureFilter();
                return;
            }

            if (gameTextureFilter < 0) {
                gameTextureFilter = *(int*)TextureFilterPreference;
            }

            *(int*)TextureFilterPreference = AnisotropicFilter;
            var samples = System.Math.Min(wanted, System.Math.Max(1, device.DeviceCaps.MaxAnisotropy));

            for (var s = 0; s < AnisotropicStages; s++) {
                device.SamplerState[s].MaxAnisotropy = samples;
            }

            if (samples != appliedAnisotropy) {
                appliedAnisotropy = samples;
                PluginCore.LogFile($"[image] anisotropic filtering at {samples}x (the card offers "
                    + $"{device.DeviceCaps.MaxAnisotropy}x; the game's texture filtering was {gameTextureFilter})");
            }
        }

        /// <summary>The game's own texture filtering back, if this changed it.</summary>
        private unsafe void PutBackTextureFilter() {
            if (gameTextureFilter < 0) {
                return;
            }

            *(int*)TextureFilterPreference = gameTextureFilter;
            PluginCore.LogFile($"[image] the game's texture filtering ({gameTextureFilter}) back");
            gameTextureFilter = -1;
            appliedAnisotropy = 0;
        }
    }
}
