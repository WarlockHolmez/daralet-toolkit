using DaraletPlayer.Feed;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DaraletPlayer.Tests {
    [TestClass]
    public class DrawDistanceRampTests {
        [TestMethod]
        public void GoesUpAStepAtATime() {
            var steps = new System.Collections.Generic.List<int>();
            var at = 1;

            while (at != 16) {
                at = DrawDistanceRamp.Next(at, 16, DrawDistanceRamp.SceneryStep);
                steps.Add(at);
            }

            CollectionAssert.AreEqual(new[] { 3, 5, 7, 9, 11, 13, 15, 16 }, steps);
        }

        [TestMethod]
        public void GoesDownAllAtOnce() {
            Assert.AreEqual(1, DrawDistanceRamp.Next(16, 1, DrawDistanceRamp.SceneryStep));
        }

        [TestMethod]
        public void StaysWhenThere() {
            Assert.AreEqual(24, DrawDistanceRamp.Next(24, 24, DrawDistanceRamp.LandscapeStep));
        }
    }
}
