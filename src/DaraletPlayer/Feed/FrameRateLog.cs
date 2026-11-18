using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace DaraletPlayer.Feed {
    /// <summary>
    /// The Frame Rate window's log (Ui/FrameRateWindow.cs): a .csv in Documents\Decal Plugins\Daralet Toolkit\Frame Rate
    /// Logs, one a session, so the frame rate under each setting can be read afterwards, by the player or by Claude
    /// (the user's call, November 2026: "if I just mess around with various settings you'll get the data"). A
    /// "second" row once a second (the last second's frames, free memory, the draw distance settings), and a "change"
    /// row about 18 seconds after each change (FrameStats.Around: the 15 seconds before it and the 15 after, the
    /// reload's first 3 left out). Kept apart from the writing so it can be tested.
    /// </summary>
    public static class FrameRateLog {
        public const string Header = "time,kind,fps,low_1pct_fps,worst_ms,avg_ms,pauses,free_mb,"
            + "before_fps,before_low_1pct_fps,note";

        /// <summary>The file's name: the character's (without what a name can't hold) and when the session began.</summary>
        public static string FileName(string character, DateTime at) {
            var name = new StringBuilder();

            foreach (var letter in character ?? "") {
                name.Append(Array.IndexOf(Path.GetInvalidFileNameChars(), letter) >= 0 ? '_' : letter);
            }

            return $"{(name.Length == 0 ? "Unknown" : name.ToString())} {at:yyyy-MM-dd HH-mm-ss}.csv";
        }

        /// <summary>A second of frames, with the free memory and the settings then.</summary>
        public static string Second(DateTime at, FrameSummary second, long freeMegabytes, string settings) {
            return Row(at, "second", second, freeMegabytes, null, settings);
        }

        /// <summary>A change: the frames after it, those before it, and what it was.</summary>
        public static string Change(DateTime at, FrameSummary before, FrameSummary after, long freeMegabytes,
            string label) {
            return Row(at, "change", after, freeMegabytes, before, label);
        }

        private static string Row(DateTime at, string kind, FrameSummary frames, long freeMegabytes,
            FrameSummary before, string note) {
            var culture = CultureInfo.InvariantCulture;
            return string.Join(",",
                at.ToString("yyyy-MM-dd HH:mm:ss", culture),
                kind,
                frames.Fps.ToString("0.0", culture),
                frames.LowFps.ToString("0.0", culture),
                frames.WorstMs.ToString("0.0", culture),
                frames.AverageMs.ToString("0.00", culture),
                frames.Hitches.ToString(culture),
                freeMegabytes.ToString(culture),
                before == null ? "" : before.Fps.ToString("0.0", culture),
                before == null ? "" : before.LowFps.ToString("0.0", culture),
                Quote(note));
        }

        /// <summary>A field as .csv wants it: in quotes, a quote doubled, when it has a comma, a quote or a line end.</summary>
        public static string Quote(string text) {
            if (string.IsNullOrEmpty(text)) {
                return "";
            }

            return text.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0 ? text : "\"" + text.Replace("\"", "\"\"") + "\"";
        }
    }
}
