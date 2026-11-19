using System;
using System.Collections.Generic;

namespace DaraletPlayer.Feed {
    /// <summary>
    /// Where grass grows round you (Ui/Grass.cs; the user's call, November 2026: grass that grows out of Asheron's Call's
    /// own ground). Only on the grass terrains, Lush Grass fullest, then Grassland and Patchy Grassland, and reeds on
    /// Marsh; never on a road, in a cell with a building, or on a slope too steep to hold it. The terrain under a spot is
    /// its nearest vertex's (the ground's texture blends between vertices, so that's what you see there).
    /// <para>
    /// Clumps sit on a grid of Spacing meters fixed to the landblock, each moved within its square by a hash of the block
    /// and the square, and kept by another under its terrain's share and the density: so the same clump is always in
    /// the same place, whatever order blocks are built in or where you stand. Heights are the ground's at full detail
    /// (SceneryLayout.Ground), since grass is only ever near you. With a wildness (Feed/Wildness.cs), kept ground near
    /// towns grows a little sparser and short and even, wild ground fuller, taller and uneven (the user's call, November
    /// 2026). Kept apart from the dats so it can be tested.
    /// </para>
    /// </summary>
    public static class GrassLayout {
        public const int Grassland = 0x1;
        public const int MarshSparseSwamp = 0x4;
        public const int LushGrass = 0x3;
        public const int PatchyGrassland = 0x9;

        /// <summary>Meters between grid squares: a clump each at 100% density on wild lush grass.</summary>
        public const float Spacing = 0.6f;

        /// <summary>The steepest ground grass grows on: the normal's upward part, about 40 degrees.</summary>
        public const float SteepestUpward = 0.76f;

        private const float BlockSize = 192;
        private const float CellSize = 24;

        /// <summary>How full a terrain grows, 0 to 1.</summary>
        public static float Share(int terrainType) {
            switch (terrainType) {
                case LushGrass:
                    return 1;
                case Grassland:
                    return 0.75f;
                case PatchyGrassland:
                    return 0.4f;
                case MarshSparseSwamp:
                    return 0.35f;
                default:
                    return 0;
            }
        }

        /// <summary>The terrain type of the vertex nearest a place in the block.</summary>
        public static int TerrainAt(IReadOnlyList<ushort> terrain, float x, float y) {
            var vx = Math.Min(8, Math.Max(0, (int)Math.Round(x / CellSize)));
            var vy = Math.Min(8, Math.Max(0, (int)Math.Round(y / CellSize)));
            return (terrain[vx * 9 + vy] >> 2) & 0x1F;
        }

        /// <summary>
        /// The clumps of one block within radius of (centreX, centreY), in the block's meters (the centre may be outside
        /// it); density 0 to 1.
        /// </summary>
        public static List<GrassClump> Place(uint landblock, IReadOnlyList<ushort> terrain, IReadOnlyList<byte> heights,
            IReadOnlyList<float> heightTable, float density, ISet<int> buildingCells, float centreX, float centreY,
            float radius, Func<float, float, float> wildnessAt = null) {
            var clumps = new List<GrassClump>();

            if (terrain == null || terrain.Count < 81 || density <= 0) {
                return clumps;
            }

            var squares = (int)Math.Ceiling(BlockSize / Spacing);
            var firstX = Math.Max(0, (int)Math.Floor((centreX - radius) / Spacing));
            var lastX = Math.Min(squares - 1, (int)Math.Floor((centreX + radius) / Spacing));
            var firstY = Math.Max(0, (int)Math.Floor((centreY - radius) / Spacing));
            var lastY = Math.Min(squares - 1, (int)Math.Floor((centreY + radius) / Spacing));
            var block = landblock >> 16;

            for (var sx = firstX; sx <= lastX; sx++) {
                for (var sy = firstY; sy <= lastY; sy++) {
                    var x = (sx + Fraction(Hash(block, sx, sy, 1))) * Spacing;
                    var y = (sy + Fraction(Hash(block, sx, sy, 2))) * Spacing;

                    if (x >= BlockSize || y >= BlockSize) {
                        continue;
                    }

                    var dx = x - centreX;
                    var dy = y - centreY;

                    if (dx * dx + dy * dy > radius * radius) {
                        continue;
                    }

                    var type = TerrainAt(terrain, x, y);

                    // kept ground (near towns) grows a little sparser, wild ground fuller (Feed/Wildness.cs); 0.5 with
                    // no towns known
                    var wild = wildnessAt?.Invoke(x, y) ?? 0.5f;

                    // wild ground at full density takes every square, kept ground 60%: room to be fuller at any density
                    if (Fraction(Hash(block, sx, sy, 3)) >= Share(type) * density * (0.6f + 0.4f * wild)) {
                        continue;
                    }

                    var cell = (int)(x / CellSize) * 8 + (int)(y / CellSize);

                    if (SceneryLayout.OnRoad(terrain, x, y) || buildingCells != null && buildingCells.Contains(cell)) {
                        continue;
                    }

                    var ground = SceneryLayout.Ground(landblock, heights, heightTable, x, y);

                    if (ground.Upward < SteepestUpward) {
                        continue;
                    }

                    var yaw = Fraction(Hash(block, sx, sy, 4)) * (float)(Math.PI * 2);
                    // short and even near towns, tall and uneven in the wilds
                    var spread = 0.2f + 0.5f * wild;
                    var scale = (0.55f + 0.8f * wild) * (1 + (Fraction(Hash(block, sx, sy, 5)) - 0.5f) * spread);
                    clumps.Add(new GrassClump(x, y, ground.Z, yaw, scale, type, type == MarshSparseSwamp, wild));
                }
            }

            return clumps;
        }

        /// <summary>A well-mixed number from a block, a square and which number it is (no pattern a player would see).</summary>
        public static uint Hash(uint block, int x, int y, int which) {
            unchecked {
                var h = block * 0x9E3779B1u ^ (uint)x * 0x85EBCA77u ^ (uint)y * 0xC2B2AE3Du ^ (uint)which * 0x27D4EB2Fu;
                h ^= h >> 15;
                h *= 0x2C1B3C6Du;
                h ^= h >> 12;
                h *= 0x297A2D39u;
                h ^= h >> 15;
                return h;
            }
        }

        /// <summary>A hash as a fraction from 0 up to 1.</summary>
        public static float Fraction(uint hash) {
            return (hash >> 8) / 16777216f;
        }
    }
}
