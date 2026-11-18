using System;
using System.Collections.Generic;
using System.Globalization;

namespace DaraletPlayer.Feed {
    /// <summary>
    /// A test run for the plugin in the game (Ui/TestRunner.cs; the user's call, November 2026: "have you launch the
    /// game, login to my character, and run checks for each build"). A text file of steps, one a line, '#' starting a
    /// comment, put in Documents\Decal Plugins\Daralet Toolkit\Test Runs\run.txt by the developer (or Claude); the
    /// plugin runs it once logged in and writes what happened to results.txt beside it. Kept apart from the game so the
    /// reading can be tested.
    /// <code>
    /// set on 1            # Draw distance on (0 off)
    /// set scenery 13      # also density, landscape, detail, far
    /// settle 60           # until the draw distance has reached what's set, at most 60 seconds
    /// wait 5              # seconds
    /// measure 10          # frame rate, memory and where the draw distance is, over 10 seconds
    /// chat /something     # a chat command, through the game's chat parser
    /// log some words      # a line in the results
    /// restart             # exit the game; ShadowLauncher's Auto relaunch starts it again, and the rest runs then
    /// </code>
    /// </summary>
    public static class TestScript {
        public static readonly string[] Fields = { "on", "scenery", "density", "landscape", "detail", "far" };

        /// <summary>Each step in order; a line it can't read is a step of kind "bad", which the run reports and stops on.</summary>
        public static List<TestStep> Parse(IEnumerable<string> lines) {
            var steps = new List<TestStep>();
            var number = 0;

            foreach (var raw in lines) {
                number++;
                var hash = raw.IndexOf('#');
                var line = (hash >= 0 ? raw.Substring(0, hash) : raw).Trim();

                if (line.Length == 0) {
                    continue;
                }

                steps.Add(ParseLine(line, number));
            }

            return steps;
        }

        private static TestStep ParseLine(string line, int number) {
            var space = line.IndexOf(' ');
            var kind = (space < 0 ? line : line.Substring(0, space)).ToLowerInvariant();
            var rest = space < 0 ? "" : line.Substring(space + 1).Trim();

            switch (kind) {
                case "wait":
                case "measure":
                case "settle":
                    return Number(rest, out var seconds) && seconds > 0
                        ? new TestStep(number, line, kind, null, seconds, null)
                        : Bad(number, line, kind + " takes a number of seconds");
                case "set": {
                    var parts = rest.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length != 2 || Array.IndexOf(Fields, parts[0].ToLowerInvariant()) < 0
                        || !Number(parts[1], out var value)) {
                        return Bad(number, line, "set takes one of " + string.Join(", ", Fields) + " and a number");
                    }

                    return new TestStep(number, line, kind, parts[0].ToLowerInvariant(), value, null);
                }
                case "chat":
                case "log":
                    return rest.Length > 0 ? new TestStep(number, line, kind, null, 0, rest)
                        : Bad(number, line, kind + " takes some text");
                case "restart":
                    return new TestStep(number, line, kind, null, 0, null);
                default:
                    return Bad(number, line, "no step called " + kind);
            }
        }

        private static bool Number(string text, out double value) {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static TestStep Bad(int number, string line, string why) {
            return new TestStep(number, line, "bad", null, 0, why);
        }
    }
}
