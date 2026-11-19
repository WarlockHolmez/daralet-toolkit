using System;
using System.Collections.Generic;
using DaraletPlayer.Feed;
using ImGuiNET;

namespace DaraletPlayer.Ui {
    /// <summary>
    /// The Config tab's Client UI Override sub tab: every switch that puts one of the plugin's windows in place of one of
    /// the game's. The window switches (WindowOverrides) are the plugin's own doing; Bank window and Loot window have the
    /// server leave the game's bank and corpse windows out (PlayerFeed.WantWindow), so they ask before going on.
    /// </summary>
    internal class ClientUiOverride {
        private const string EnableAllId = "Turn on every override?##client-ui-enable-all";
        private const string DisableAllId = "Overrides are off##client-ui-disable-all";

        // every game panel a row names, by key (GamePanel.Key): Floating ends with the rest
        private static readonly HashSet<string> Grouped = new HashSet<string> {
            "toolbar", "spellbook", "attributes", "options", "book", "indicators", "link", "buffs", "debuffs", "vitae",
            "radar", "power-bar", "spell-bar", "vitals", "vendor", "creature-appraisal",
        };

        private readonly Settings settings;
        private readonly PlayerFeed feed;
        private readonly FloatingWindow corpseWindow;
        private readonly SharedWindow sharedWindow;
        private readonly GamePanels gamePanels;

        public ClientUiOverride(Settings settings, PlayerFeed feed, FloatingWindow corpseWindow, SharedWindow sharedWindow,
            GamePanels gamePanels) {
            this.sharedWindow = sharedWindow;
            this.gamePanels = gamePanels;
            this.settings = settings;
            this.feed = feed;
            this.corpseWindow = corpseWindow;
        }

        /// <summary>The Config tab's Client UI Override sub tab.</summary>
        public void Render() {
            Form.Hint("Puts the plugin's windows in place of the game's own. Hover a row's name, or a checkbox, for what "
                + "it does.");

            RenderAllButtons();
            ImGui.Spacing();
            RenderGroups();

            // on a line of its own, set apart from the rows above (the user's call, November 2026): the shared window's
            // switches are about where the windows go, not which of the game's they replace
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            var first = true;
            sharedWindow.RenderSettings(ref first);
            ImGui.Spacing();

            gamePanels.RenderSettings();
            ImGui.Spacing();
        }

        /// <summary>
        /// The checkboxes in rows by where their windows open from, each row's name on the left (the user's call,
        /// November 2026; one long row of all of them, wrapping as it liked, was hard to read, and rows by theme didn't
        /// say what worked with what): Toolbar is the Toolbar window and what its buttons open, Indicator the Indicator
        /// Bar window and what its indicators open, and Floating the windows that come up by themselves. A game panel no
        /// row names goes at the end of Floating.
        /// </summary>
        private void RenderGroups() {
            var rows = new[] { "Toolbar", "Indicator", "Floating" };
            var labelWidth = 0f;

            foreach (var row in rows) {
                labelWidth = Math.Max(labelWidth, ImGui.CalcTextSize(row).X);
            }

            if (!ImGui.BeginTable("client-ui-groups", 2, ImGuiTableFlags.None)) {
                return;
            }

            ImGui.TableSetupColumn("label", ImGuiTableColumnFlags.WidthFixed,
                labelWidth + ImGui.GetStyle().ItemSpacing.X * 2);
            ImGui.TableSetupColumn("checks", ImGuiTableColumnFlags.WidthStretch);

            Form.Label("Toolbar", "Toolbar puts the plugin's toolbar in place of the game's; the rest are what its "
                + "buttons open, the plugin's window where ticked and the game's panel where not, and Hide hotbar, the "
                + "game's shortcut bar on its toolbar.");
            var first = true;
            gamePanels.RenderToggles(ref first, new[] { "toolbar" });
            WindowOverrides.RenderInventory(ref first, settings);
            WindowOverrides.RenderSocial(ref first, settings);
            gamePanels.RenderToggles(ref first, new[] { "spellbook", "attributes" });
            WindowOverrides.RenderQuests(ref first, settings);
            WindowOverrides.RenderMap(ref first, settings);
            gamePanels.RenderToggles(ref first, new[] { "options" });
            GameHotbar.RenderCheckbox(ref first, settings);

            Form.Label("Indicator", "Indicator Bar puts the plugin's indicators in place of the game's; the rest are what "
                + "they open: link status, your character stats (the burden pack), buffs, debuffs and vitae.");
            first = true;
            gamePanels.RenderToggles(ref first, new[] { "indicators", "link" });
            WindowOverrides.RenderCharacterStats(ref first, settings);
            gamePanels.RenderToggles(ref first, new[] { "buffs", "debuffs", "vitae" });

            Form.Label("Floating", "Windows that come up by themselves: the radar, combat bars and vitals, banks, vendors "
                + "and corpses, the salvage panel, appraisals, chat and books. Bank window and Loot window have the server "
                + "leave out the game's bank and corpse windows, so the plugin's open instead. Chat hides the game's chat "
                + "windows for the plugin's (their settings: Chat, beside this).");
            first = true;
            gamePanels.RenderToggles(ref first, new[] { "radar", "power-bar", "spell-bar", "vitals" });
            Form.Flow(ref first, Form.CheckWidth("Bank window"));
            RenderBankWindow();
            gamePanels.RenderToggles(ref first, new[] { "vendor" });
            Form.Flow(ref first, Form.CheckWidth("Loot window"));
            RenderLootWindow();
            SalvagePanel.RenderToggle(ref first, settings);
            gamePanels.RenderToggles(ref first, new[] { "creature-appraisal" });
            RenderItemAppraisal(ref first);
            ChatTab.RenderHideGameChat(ref first, settings);
            gamePanels.RenderToggles(ref first, new[] { "book" });
            gamePanels.RenderTogglesExcept(ref first, Grouped);

            ImGui.EndTable();
        }

        /// <summary>
        /// Item Appraisal (SettingsData.AppraiseInPlugin): the plugin's Appraisal window for items. It was Plugin appraisal,
        /// on the main window's top line, until it joined the other overrides beside Creature Appraisal (the user's call,
        /// November 2026).
        /// </summary>
        private void RenderItemAppraisal(ref bool first) {
            if (Form.FlowCheck(ref first, "Item Appraisal##client-ui-item-appraisal", ref settings.Data.AppraiseInPlugin,
                "Appraising an item you carry, wear, have banked or see in an open container (and, on servers that "
                + "allow it, one on the ground or someone else's) shows it in the plugin's Appraisal window instead of "
                + "the game's panel, from the appraise key, the magnifier, a right-click or the item menus' Appraise: "
                + "every stat, how well each rolled, what your loot rules make of it and how it compares with what "
                + "you're wearing. Appraise in new window opens another, to keep several. Off, items go to the game's "
                + "own appraisal panel.")) {
                settings.Changed();
            }
        }

        private bool BankSupported => feed.Supports(PlayerFeed.KindBankWindow);
        private bool LootSupported => feed.Supports(PlayerFeed.KindLootWindow);

        /// <summary>
        /// Enable all and Disable all, on a line of their own above the toggles. A server too old for Bank window or Loot
        /// window has them left off.
        /// </summary>
        private void RenderAllButtons() {
            var data = settings.Data;
            var allOn = WindowOverrides.AllOn(data) && (data.BankWindow || !BankSupported)
                && (data.LootWindow || !LootSupported);
            var anyOn = WindowOverrides.AnyOn(data) || data.BankWindow || data.LootWindow;
            var first = true;

            ImGui.BeginDisabled(allOn);

            if (Form.FlowSmallButton(ref first, "Enable all##client-ui", "Ticks all seven: the five windows, Bank "
                + "window and Loot window. Bank window and Loot window have the server leave the game's windows out, so "
                + "this asks first when either is about to go on, as their own checkboxes do.")) {
                var windowsGoingOn = (BankSupported && !data.BankWindow) || (LootSupported && !data.LootWindow);

                if (windowsGoingOn) {
                    // not on until it's confirmed
                    ImGui.OpenPopup(EnableAllId);
                }
                else {
                    SetAll(true);
                }
            }

            ImGui.EndDisabled();
            ImGui.BeginDisabled(!anyOn);

            if (Form.FlowSmallButton(ref first, "Disable all##client-ui", "Unticks all seven, so the Toolbar window's "
                + "buttons open the game's panels again, and the game's bank and corpse windows are back.")) {
                var windowsWereOn = data.BankWindow || data.LootWindow;
                SetAll(false);

                if (windowsWereOn) {
                    ImGui.OpenPopup(DisableAllId);
                }
            }

            ImGui.EndDisabled();
            RenderAllPopups();
        }

        /// <summary>Enable all's question and Disable all's notice, drawn where the buttons opened them.</summary>
        private void RenderAllPopups() {
            var open = true;

            if (ImGui.BeginPopupModal(EnableAllId, ref open, ImGuiWindowFlags.AlwaysAutoResize)) {
                Widgets.PopupText(Windows.ConfirmAllOn());

                if (ImGui.Button("Turn on")) {
                    SetAll(true);
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();

                if (ImGui.Button("Cancel")) {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }

            open = true;

            if (ImGui.BeginPopupModal(DisableAllId, ref open, ImGuiWindowFlags.AlwaysAutoResize)) {
                Widgets.PopupText(Windows.NoticeAllOff());

                if (ImGui.Button("OK")) {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }
        }

        private void SetAll(bool on) {
            var data = settings.Data;
            WindowOverrides.SetAll(data, on);
            data.BankWindow = on && BankSupported;
            data.LootWindow = on && LootSupported;

            // Loot window makes the Corpse window the only one for a corpse, so it has to be allowed to show
            if (data.LootWindow) {
                corpseWindow.Visible = true;
            }

            settings.Changed();
        }

        private void RenderBankWindow() {
            var supported = BankSupported;
            var tip = supported
                ? "Opening a bank opens this plugin's Bank window instead of the game's (the server leaves the game's "
                    + "out). Closing the window closes the bank, and walking away closes both. Its size and background "
                    + "are under the Inventory tab's Settings."
                : "This server is too old for the plugin's own bank window.";

            ImGui.BeginDisabled(!supported);

            if (Widgets.GuardedCheckbox("Bank window", ref settings.Data.BankWindow, tip,
                Windows.ConfirmOn("bank", "bank window"), Windows.NoticeOff("bank"))) {
                settings.Changed();
            }

            ImGui.EndDisabled();
        }

        private void RenderLootWindow() {
            var supported = LootSupported;
            var tip = supported
                ? "Opening a corpse or a chest shows everything in it in the Corpse window instead of the game's window "
                    + "(the server leaves the game's out), with what your rules match marked. Click an item to loot it. "
                    + "Closing the window closes the corpse, and walking away closes both. Works with the looter on or "
                    + "off. Its size and background are under the Inventory tab's Looter, in its Settings."
                : "This server is too old for the plugin's own loot window.";

            ImGui.BeginDisabled(!supported);

            if (Widgets.GuardedCheckbox("Loot window", ref settings.Data.LootWindow, tip,
                Windows.ConfirmOn("corpses and chests", "Corpse window"), Windows.NoticeOff("corpse and chest"))) {
                settings.Changed();

                // it's now the only window for a corpse, so it has to be allowed to show
                if (settings.Data.LootWindow) {
                    corpseWindow.Visible = true;
                }
            }

            ImGui.EndDisabled();
        }
    }
}
