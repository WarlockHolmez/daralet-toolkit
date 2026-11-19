using DaraletPlayer.Feed;

namespace DaraletPlayer.Ui {
    /// <summary>
    /// Whether the plugin's Inventory, Quests, Social, Map and Character Stats windows stand in for the game's panels,
    /// each in its row of Client UI Override. Each is that window's own switch: the Toolbar window's buttons
    /// (ToolbarPanel) open the plugin's window for those ticked and the game's panel for the rest, and the Indicator Bar
    /// window's burden pack does the same for Character Stats. Until November 2026 these drew the game toolbar's buttons
    /// again over its own; the Toolbar window, which replaces the whole toolbar, took their place (the user's call).
    /// Their settings keep their names.
    /// </summary>
    internal static class WindowOverrides {
        public static void RenderInventory(ref bool first, Settings settings) {
            Setting(ref first, settings, ref settings.Data.ToolbarInventory, "Inventory", "Inventory", "the inventory",
                "backpack");
        }

        public static void RenderQuests(ref bool first, Settings settings) {
            Setting(ref first, settings, ref settings.Data.ToolbarQuests, "Quests", "Quests", "the Contracts panel",
                "Contracts button");
        }

        public static void RenderSocial(ref bool first, Settings settings) {
            Setting(ref first, settings, ref settings.Data.ToolbarSocial, "Social", "Social", "the social panel",
                "Social button");
        }

        public static void RenderMap(ref bool first, Settings settings) {
            Setting(ref first, settings, ref settings.Data.ToolbarMap, "Map", "Map", "the map panel", "Map button");
        }

        /// <summary>The Character Stats window.</summary>
        public static void RenderCharacterStats(ref bool first, Settings settings) {
            if (Form.FlowCheck(ref first, "Character Stats##window-override-character", ref settings.Data.ToolbarCharacter,
                "The Indicator Bar window's burden pack opens Daralet Toolkit's Character Stats window (your character "
                + "information and stats) instead of the game's Character Information panel. Takes effect while "
                + "Indicator Bar replaces the game's indicators; the game's own key for the panel still opens the "
                + "game's.")) {
                settings.Changed();
            }
        }

        /// <summary>Whether all five are on.</summary>
        public static bool AllOn(SettingsData data) {
            return data.ToolbarInventory && data.ToolbarQuests && data.ToolbarMap && data.ToolbarCharacter
                && data.ToolbarSocial;
        }

        /// <summary>Whether any of the five is on.</summary>
        public static bool AnyOn(SettingsData data) {
            return data.ToolbarInventory || data.ToolbarQuests || data.ToolbarMap || data.ToolbarCharacter
                || data.ToolbarSocial;
        }

        /// <summary>Turns all five on or off, for Enable all and Disable all. The caller saves.</summary>
        public static void SetAll(SettingsData data, bool on) {
            data.ToolbarInventory = on;
            data.ToolbarQuests = on;
            data.ToolbarMap = on;
            data.ToolbarCharacter = on;
            data.ToolbarSocial = on;
        }

        /// <summary>The checkbox for one window opened from the Toolbar window, in a Form.Flow row.</summary>
        private static void Setting(ref bool first, Settings settings, ref bool value, string label, string window,
            string panel, string button) {
            if (Form.FlowCheck(ref first, $"{label}##window-override-{window}", ref value, $"The Toolbar window's "
                + $"{button} opens Daralet Toolkit's {window} window instead of {panel}. Takes effect while Toolbar "
                + "replaces the game's toolbar; the game's own key for the panel still opens the game's.")) {
                settings.Changed();
            }
        }
    }
}
