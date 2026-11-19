using System;

namespace DaraletPlayer.Feed {
    /// <summary>
    /// The pictures grass clumps are drawn with (Ui/Grass.cs), made here rather than taken from anywhere else, so their
    /// colour is all the game's: the blades are near white, darker toward the base, and take their colour from the
    /// ground texture they grow from (GrassPicture.Average of the terrain's texture, multiplied in when drawn). Grass is
    /// a tuft of tapering, leaning blades; reeds are taller, straighter and fewer, some with a seed head. Cut out by
    /// alpha (opaque or clear, as the alpha test wants), 0xAARRGGBB rows from the top. Kept apart so it can be tested.
    /// </summary>
    public static class GrassPicture {
        /// <summary>A clump's picture, width by height pixels, the same every time for a seed.</summary>
        /// <param name="lean">How far the blades lean, as a share of the width (kept grass stands up, wild leans).</param>
        /// <param name="shortest">The shortest blade, as a share of the height (kept grass is even, as mown).</param>
        public static uint[] Make(int width, int height, int blades, int seed, bool reeds, double lean = 0.7,
            double shortest = 0.45) {
            var pixels = new uint[width * height];
            var random = new Random(seed);

            for (var b = 0; b < blades; b++) {
                var baseX = width * (0.2 + random.NextDouble() * 0.6);
                var tilt = width * (random.NextDouble() - 0.5) * (reeds ? 0.25 : lean);
                var tall = height * (reeds ? 0.7 + random.NextDouble() * 0.3
                    : shortest + random.NextDouble() * (1 - shortest));
                var halfWidth = reeds ? 1.0 + random.NextDouble() * 0.6 : 1.4 + random.NextDouble() * 1.2;
                var shade = 0.85 + random.NextDouble() * 0.15;
                var seedHead = reeds && random.NextDouble() < 0.4;

                for (var row = 0; row < tall; row++) {
                    var t = row / tall;
                    var centre = baseX + tilt * Math.Pow(t, 1.6);
                    var half = halfWidth * (1 - t) + 0.35;
                    var head = seedHead && t > 0.72 && t < 0.92;

                    if (head) {
                        half = Math.Max(half, 1.8);
                    }

                    // darker at the base, as grass is in its own shade
                    var bright = head ? 0.5 : (0.72 + 0.28 * t) * shade;
                    var level = (int)(255 * Math.Min(1, bright));
                    var colour = head ? Argb(255, level, (int)(level * 0.8), (int)(level * 0.6))
                        : Argb(255, level, level, level);
                    var y = height - 1 - row;

                    for (var x = (int)Math.Floor(centre - half); x <= (int)Math.Ceiling(centre + half); x++) {
                        if (x >= 0 && x < width && y >= 0 && Math.Abs(x + 0.5 - centre) <= half) {
                            pixels[y * width + x] = colour;
                        }
                    }
                }
            }

            return pixels;
        }

        /// <summary>The average colour (0xFFRRGGBB) of a picture's opaque pixels, or mid grey for none.</summary>
        public static uint Average(uint[] pixels) {
            long r = 0, g = 0, b = 0, count = 0;

            foreach (var p in pixels) {
                if (p >> 24 < 128) {
                    continue;
                }

                r += (p >> 16) & 255;
                g += (p >> 8) & 255;
                b += p & 255;
                count++;
            }

            return count == 0 ? 0xFF808080 : Argb(255, (int)(r / count), (int)(g / count), (int)(b / count));
        }

        private static uint Argb(int a, int r, int g, int b) {
            return (uint)(a << 24 | r << 16 | g << 8 | b);
        }
    }
}
