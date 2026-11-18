using System;
using System.Collections.Generic;

namespace DaraletPlayer.Feed {
    /// <summary>
    /// The scenery's textures as 32-bit colour (0xAARRGGBB), for billboard pictures (Feed/ImpostorRaster.cs): the trees'
    /// are DXT1 (trunks) and DXT5 (foliage cut out by its alpha), and small scenery's 16-bit palette indexes, as a look
    /// at the far scenery round the user showed (November 2026). UtilityBelt's Texture.GetImageColorArray doesn't do DXT.
    /// The DXT formats are the standard S3TC blocks: 4x4 pixels, two RGB565 end colours and 2-bit indexes, DXT5 with
    /// two end alphas and 3-bit alpha indexes before them.
    /// </summary>
    public static class TextureDecode {
        public static uint[] Dxt1(byte[] data, int width, int height) {
            var pixels = new uint[width * height];

            for (int by = 0, block = 0; by < height; by += 4) {
                for (var bx = 0; bx < width; bx += 4, block += 8) {
                    if (block + 8 > data.Length) {
                        return pixels;
                    }

                    ColourBlock(data, block, pixels, width, height, bx, by, true, null);
                }
            }

            return pixels;
        }

        public static uint[] Dxt5(byte[] data, int width, int height) {
            var pixels = new uint[width * height];
            var alphas = new byte[16];

            for (int by = 0, block = 0; by < height; by += 4) {
                for (var bx = 0; bx < width; bx += 4, block += 16) {
                    if (block + 16 > data.Length) {
                        return pixels;
                    }

                    AlphaBlock(data, block, alphas);
                    ColourBlock(data, block + 8, pixels, width, height, bx, by, false, alphas);
                }
            }

            return pixels;
        }

        /// <summary>16-bit indexes into a palette of 0xAARRGGBB colours (missing ones transparent).</summary>
        public static uint[] Index16(byte[] data, int width, int height, IReadOnlyList<uint> palette) {
            var pixels = new uint[width * height];

            for (var i = 0; i < pixels.Length && i * 2 + 1 < data.Length; i++) {
                var index = data[i * 2] | (data[i * 2 + 1] << 8);
                pixels[i] = palette != null && index < palette.Count ? palette[index] : 0;
            }

            return pixels;
        }

        private static void AlphaBlock(byte[] data, int at, byte[] alphas) {
            var a0 = data[at];
            var a1 = data[at + 1];
            var table = new byte[8];
            table[0] = a0;
            table[1] = a1;

            for (var i = 2; i < 8; i++) {
                table[i] = a0 > a1 ? (byte)(((8 - i) * a0 + (i - 1) * a1) / 7)
                    : i < 6 ? (byte)(((6 - i) * a0 + (i - 1) * a1) / 5) : (byte)(i == 6 ? 0 : 255);
            }

            ulong bits = 0;

            for (var i = 0; i < 6; i++) {
                bits |= (ulong)data[at + 2 + i] << (8 * i);
            }

            for (var i = 0; i < 16; i++) {
                alphas[i] = table[(bits >> (3 * i)) & 7];
            }
        }

        private static void ColourBlock(byte[] data, int at, uint[] pixels, int width, int height, int bx, int by,
            bool punchThrough, byte[] alphas) {
            var c0 = (ushort)(data[at] | (data[at + 1] << 8));
            var c1 = (ushort)(data[at + 2] | (data[at + 3] << 8));
            var colours = new uint[4];
            colours[0] = Rgb565(c0);
            colours[1] = Rgb565(c1);

            if (c0 > c1 || !punchThrough) {
                colours[2] = Mix(colours[0], colours[1], 2, 1, 3);
                colours[3] = Mix(colours[0], colours[1], 1, 2, 3);
            }
            else {
                colours[2] = Mix(colours[0], colours[1], 1, 1, 2);
                colours[3] = 0;
            }

            var indexes = (uint)(data[at + 4] | (data[at + 5] << 8) | (data[at + 6] << 16) | (data[at + 7] << 24));

            for (var i = 0; i < 16; i++) {
                var x = bx + (i & 3);
                var y = by + (i >> 2);

                if (x >= width || y >= height) {
                    continue;
                }

                var colour = colours[(indexes >> (2 * i)) & 3];

                if (alphas != null) {
                    colour = (colour & 0x00FFFFFF) | ((uint)alphas[i] << 24);
                }
                else if (colour != 0) {
                    colour |= 0xFF000000;
                }

                pixels[y * width + x] = colour;
            }
        }

        private static uint Rgb565(ushort c) {
            var r = (c >> 11) & 31;
            var g = (c >> 5) & 63;
            var b = c & 31;
            return (uint)(((r * 255 / 31) << 16) | ((g * 255 / 63) << 8) | (b * 255 / 31));
        }

        private static uint Mix(uint a, uint b, int wa, int wb, int total) {
            uint Channel(int shift) => (uint)((((a >> shift) & 255) * wa + ((b >> shift) & 255) * wb) / total) << shift;
            return Channel(16) | Channel(8) | Channel(0);
        }
    }
}
