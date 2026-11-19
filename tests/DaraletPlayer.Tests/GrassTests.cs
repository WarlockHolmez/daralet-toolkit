using System.Collections.Generic;
using System.Linq;
using DaraletPlayer.Feed;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DaraletPlayer.Tests {
    [TestClass]
    public class GrassTests {
        private const uint Block = 0x7F7F0000;

        private static readonly float[] Table = Enumerable.Range(0, 256).Select(i => i * 2f).ToArray();

        private static List<ushort> Terrain(int type) {
            return Enumerable.Repeat((ushort)(type << 2), 81).ToList();
        }

        private static List<byte> Flat() {
            return Enumerable.Repeat((byte)10, 81).ToList();
        }

        private static List<GrassClump> Place(IReadOnlyList<ushort> terrain, IReadOnlyList<byte> heights = null,
            float density = 1, ISet<int> buildings = null) {
            return GrassLayout.Place(Block, terrain, heights ?? Flat(), Table, density, buildings, 96, 96, 40);
        }

        [TestMethod]
        public void OnlyGrassyGroundGrows() {
            Assert.IsTrue(Place(Terrain(GrassLayout.LushGrass)).Count > 0);
            Assert.IsTrue(Place(Terrain(GrassLayout.Grassland)).Count > 0);
            Assert.IsTrue(Place(Terrain(GrassLayout.PatchyGrassland)).Count > 0);
            Assert.AreEqual(0, Place(Terrain(0x0)).Count, "barren rock");
            Assert.AreEqual(0, Place(Terrain(0xA)).Count, "yellow sand");
            Assert.AreEqual(0, Place(Terrain(0xF)).Count, "snow");
            Assert.AreEqual(0, Place(Terrain(0x11)).Count, "water");
        }

        [TestMethod]
        public void LushGrowsFullerThanPatchy() {
            var lush = Place(Terrain(GrassLayout.LushGrass)).Count;
            var patchy = Place(Terrain(GrassLayout.PatchyGrassland)).Count;
            Assert.IsTrue(lush > patchy * 2, $"lush {lush}, patchy {patchy}");
        }

        [TestMethod]
        public void MarshGrowsReedsAndGrassDoesnt() {
            Assert.IsTrue(Place(Terrain(GrassLayout.MarshSparseSwamp)).All(c => c.Reeds));
            Assert.IsTrue(Place(Terrain(GrassLayout.LushGrass)).All(c => !c.Reeds));
        }

        [TestMethod]
        public void TheSameClumpsGrowEveryTime() {
            var first = Place(Terrain(GrassLayout.Grassland));
            var again = Place(Terrain(GrassLayout.Grassland));
            Assert.AreEqual(first.Count, again.Count);
            Assert.AreEqual(first[10].X, again[10].X);
            Assert.AreEqual(first[10].Yaw, again[10].Yaw);
        }

        [TestMethod]
        public void OnlyWithinTheRadius() {
            foreach (var clump in Place(Terrain(GrassLayout.LushGrass))) {
                var dx = clump.X - 96;
                var dy = clump.Y - 96;
                Assert.IsTrue(dx * dx + dy * dy <= 40 * 40 + 0.01f);
            }
        }

        [TestMethod]
        public void ACentreOutsideTheBlockStillReachesIn() {
            // standing 20 m west of the block's edge, 40 m round you reaches 20 m into it
            var clumps = GrassLayout.Place(Block, Terrain(GrassLayout.LushGrass), Flat(), Table, 1, null, -20, 96, 40);
            Assert.IsTrue(clumps.Count > 0);
            Assert.IsTrue(clumps.All(c => c.X <= 20.01f));
        }

        [TestMethod]
        public void NotOnRoadsOrInBuildingCells() {
            var roads = Terrain(GrassLayout.LushGrass);

            for (var i = 0; i < roads.Count; i++) {
                roads[i] |= 0x3;
            }

            Assert.AreEqual(0, Place(roads).Count, "every cell a road");

            var everyCell = new HashSet<int>(Enumerable.Range(0, 64));
            Assert.AreEqual(0, Place(Terrain(GrassLayout.LushGrass), buildings: everyCell).Count);
        }

        [TestMethod]
        public void NotOnSteepSlopes() {
            // heights rising 40 a vertex east: 80 m every 24 m, far too steep
            var steep = new List<byte>();

            for (var x = 0; x < 9; x++) {
                for (var y = 0; y < 9; y++) {
                    steep.Add((byte)(x * 30));
                }
            }

            Assert.AreEqual(0, Place(Terrain(GrassLayout.LushGrass), steep).Count);
        }

        [TestMethod]
        public void DensityThinsIt() {
            var full = Place(Terrain(GrassLayout.LushGrass)).Count;
            var half = Place(Terrain(GrassLayout.LushGrass), density: 0.5f).Count;
            Assert.IsTrue(half > full * 0.35 && half < full * 0.65, $"full {full}, half {half}");
        }

        [TestMethod]
        public void ClumpsStandOnTheGround() {
            // every vertex 10 high: 20 m in the table
            Assert.IsTrue(Place(Terrain(GrassLayout.LushGrass)).All(c => System.Math.Abs(c.Z - 20) < 0.001f));
        }

        [TestMethod]
        public void PicturesAreCutOutAndPaleSoTheGroundColoursThem() {
            var pixels = GrassPicture.Make(64, 64, 14, 7, false);
            Assert.IsTrue(pixels.All(p => p >> 24 == 0 || p >> 24 == 255), "opaque or clear only");
            var opaque = pixels.Count(p => p >> 24 == 255);
            Assert.IsTrue(opaque > 200 && opaque < 64 * 64 / 2, $"{opaque} opaque pixels");
            CollectionAssert.AreEqual(pixels, GrassPicture.Make(64, 64, 14, 7, false), "the same for a seed");
            Assert.AreEqual(0xFF102030u, GrassPicture.Average(new uint[] { 0xFF102030, 0x00FFFFFF }), "clear ignored");
        }

        [TestMethod]
        public void GrassSettingsHaveDefaultsAndLimits() {
            var options = Json.Read<GrassOptions>("{}");
            Assert.IsFalse(options.On);
            Assert.IsTrue(options.Reeds && options.Wind);
            options.Distance = 500;
            options.Density = 0;
            Assert.AreEqual(GrassOptions.MaxDistance, options.ClampedDistance);
            Assert.AreEqual(1, options.ClampedDensity);
            Assert.IsTrue(TestScript.Parse(new[] { "set grass 1", "set grassdensity 70", "set wind 0" })
                .TrueForAll(s => s.Kind == "set"));
        }
    }
}
