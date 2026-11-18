using System;
using System.Numerics;
using Decal.Adapter;
using DaraletPlayer.Feed;
using ImGuiNET;

namespace DaraletPlayer.Ui {
    /// <summary>
    /// Two ways to open and close the /dap window besides UtilityBelt's bar: a button in a window of its own (launcher,
    /// hidden to start), which can go anywhere and be any size, and a key (SettingsData.ToolkitKey, none to start). The key
    /// is read from Windows on Decal's RenderFrame, as Hide game UI's is, since with every window closed nothing draws to
    /// read it through Hotkeys.Pressed.
    /// </summary>
    internal sealed class Launcher : IDisposable {
        private const float ButtonSize = 44;

        private readonly Settings settings;
        private readonly Func<bool> mainVisible;
        private readonly Action toggleMain;
        private readonly FloatingWindow window;

        private bool keyWasDown;
        private bool capturing;
        private bool disposed;

        public Launcher(Settings settings, Func<bool> mainVisible, Action toggleMain) {
            this.settings = settings;
            this.mainVisible = mainVisible;
            this.toggleMain = toggleMain;

            window = new FloatingWindow("launcher", "Toolkit button", settings, new Vector2(20, 120), RenderButton,
                visibleByDefault: false, padding: new Vector2(3, 3));
            CoreManager.Current.RenderFrame += Core_RenderFrame;
        }

        private HotkeyBinding Key => settings.Data.ToolkitKey;

        private string KeyLabel => Key?.IsSet == true ? Key.Label : null;

        private void RenderButton() {
            var size = ButtonSize * window.Scale;
            var open = mainVisible();

            if (Form.LitButton("Toolkit##launcher", open, new Vector2(size * 1.6f, size))) {
                toggleMain();
            }

            var key = KeyLabel != null ? $" ({KeyLabel})" : "";
            Widgets.Tip((open ? "Closes" : "Opens") + $" the Daralet Toolkit window{key}. Right-click to lock it, size it or "
                + "hide it; the Config tab's Toolkit button section has the rest.");
        }

        private void Core_RenderFrame(object sender, EventArgs e) {
            try {
                if (disposed || CoreManager.Current.CharacterFilter.LoginStatus != 3) {
                    keyWasDown = false;
                    return;
                }

                ReadKey();
            }
            catch (Exception ex) {
                PluginCore.Log(ex);
            }
        }

        /// <summary>The key going down, with exactly its modifiers, in the game and not while typing, opens or closes it.</summary>
        private void ReadKey() {
            var key = Key;

            if (key?.IsSet != true || capturing) {
                keyWasDown = false;
                return;
            }

            var down = Hotkeys.IsDown(key.Key)
                && Hotkeys.IsDown(Keys.Control) == key.Ctrl
                && Hotkeys.IsDown(Keys.Shift) == key.Shift
                && Hotkeys.IsDown(Keys.Alt) == key.Alt;

            if (down && !keyWasDown && Hotkeys.ForTheGame()) {
                toggleMain();
            }

            keyWasDown = down;
        }

        /// <summary>
        /// The Config tab's Toolkit button row, in a form of the caller's: the button's window, its size and background.
        /// Once a section of its own with the key; the key is now with the others (RenderKeyRow).
        /// </summary>
        public void RenderWindowRow() {
            Form.Label("Toolkit button", "A button you can put anywhere on screen and make any size, that opens and "
                + "closes this window, as UtilityBelt's bar and /dap do.");
            window.RenderCompactControls("A button in a window of its own that opens and closes this window, like "
                + "UtilityBelt's bar button. Unlock it to drag it anywhere; Size makes it bigger or smaller.");
        }

        /// <summary>The Config tab's Toolkit key row, in a form of the caller's.</summary>
        public void RenderKeyRow() {
            Form.Label("Toolkit key", "Opens and closes this window, as UtilityBelt's bar and /dap do.");
            RenderKey();
            Widgets.Tip("Opens and closes this window. Click, then press the key (with Ctrl, Shift or Alt if you "
                + "like); Escape cancels, right-click clears it. The game hears it too, and ignores Ctrl and Shift on its "
                + "own keys, so pick a key it doesn't use at all.");
        }

        private void RenderKey() {
            var label = capturing ? "Press a key..." : KeyLabel ?? "none";

            if (Form.LitButton($"{label}##toolkit-key", capturing)) {
                capturing = !capturing;
            }

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
                capturing = false;
                settings.Data.ToolkitKey = new HotkeyBinding();
                settings.Changed();
            }

            if (!capturing || !Hotkeys.GameInFront()) {
                return;
            }

            foreach (var key in Hotkeys.Pressed()) {
                if (key != Keys.Escape) {
                    settings.Data.ToolkitKey = new HotkeyBinding(key, Hotkeys.Ctrl, Hotkeys.Shift, Hotkeys.Alt);
                    settings.Changed();
                }
                else {
                    EscapeCloses.Taken();
                }

                capturing = false;

                // still held as the capture ends: not a press of the new key
                keyWasDown = true;
                break;
            }
        }

        public void Dispose() {
            disposed = true;
            CoreManager.Current.RenderFrame -= Core_RenderFrame;
            window.Dispose();
        }
    }
}
