namespace DaraletPlayer.Ui {
    /// <summary>
    /// What the Bank window and Loot window switches say when they're turned on and off. The server, not the plugin, leaves
    /// the game's windows out, and it only hears otherwise from the plugin: with the plugin gone (switched off in Decal,
    /// removed, or failed to load), nothing tells it, so the game's windows stay away until the next login, when the server
    /// forgets.
    /// </summary>
    internal static class Windows {
        private const string WhileLoggedIn = "The server keeps doing this for as long as you're logged in. If this plugin "
            + "stops running (switched off in Decal, removed, or failing to load), nothing tells the server, so you'll see "
            + "neither window until you log out and back in.";

        public static string ConfirmOn(string what, string window) {
            return $"The game's own window for {what} won't open any more: the server leaves it out, and this plugin's {window} "
                + "opens instead.\n\n" + WhileLoggedIn;
        }

        /// <summary>For Enable all in Client UI Override: Bank window and Loot window at once.</summary>
        public static string ConfirmAllOn() {
            return "This turns on every override. The game's own bank window, and its window for corpses and chests, won't "
                + "open any more: the server leaves them out, and this plugin's Bank and Corpse windows open instead. The "
                + "Toolbar and Indicator Bar windows' buttons open the plugin's Inventory, Quests, Social, Map and Character Stats "
                + "windows too.\n\n" + WhileLoggedIn;
        }

        public static string NoticeOff(string what) {
            return $"The game's {what} window opens again from the next one you open; one open now stays in this plugin's window "
                + "until it's closed.\n\n"
                + "If the game's window still doesn't come back (the server didn't hear, say), log out and back in: the server "
                + "forgets this at logout.";
        }

        /// <summary>For Disable all in Client UI Override, when Bank window or Loot window was on.</summary>
        public static string NoticeAllOff() {
            return "The game's own bank, corpse and chest windows open again from the next one you open; one open now stays "
                + "in this plugin's window until it's closed. The Toolbar and Indicator Bar windows' buttons open the game's "
                + "panels again.\n\n"
                + "If the game's windows still don't come back (the server didn't hear, say), log out and back in: the "
                + "server forgets this at logout.";
        }
    }
}
