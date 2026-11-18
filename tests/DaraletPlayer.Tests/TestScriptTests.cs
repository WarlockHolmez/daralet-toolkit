using DaraletPlayer.Feed;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DaraletPlayer.Tests {
    [TestClass]
    public class TestScriptTests {
        [TestMethod]
        public void ReadsEveryKindOfStep() {
            var steps = TestScript.Parse(new[] {
                "# a comment line",
                "set scenery 13   # with a comment",
                "",
                "settle 60",
                "wait 2.5",
                "measure 10",
                "chat /loc",
                "log density test",
                "restart",
            });

            Assert.AreEqual(7, steps.Count);
            Assert.AreEqual("set", steps[0].Kind);
            Assert.AreEqual("scenery", steps[0].Field);
            Assert.AreEqual(13, steps[0].Number);
            Assert.AreEqual(2, steps[0].Line);
            Assert.AreEqual(2.5, steps[2].Number);
            Assert.AreEqual("/loc", steps[4].Words);
            Assert.AreEqual("restart", steps[6].Kind);
        }

        [TestMethod]
        public void SaysWhatItCantRead() {
            var steps = TestScript.Parse(new[] { "set trees 5", "wait", "jump 3", "chat" });
            Assert.IsTrue(steps.TrueForAll(s => s.Kind == "bad"));
            StringAssert.Contains(steps[0].Words, "scenery");
            StringAssert.Contains(steps[2].Words, "jump");
        }

        [TestMethod]
        public void IgnoresCaseOfTheStepAndField() {
            var step = TestScript.Parse(new[] { "SET Density 12" })[0];
            Assert.AreEqual("set", step.Kind);
            Assert.AreEqual("density", step.Field);
        }
    }
}
