using NUnit.Framework;

namespace Capstone.Session.Tests
{
    public class PrototypeScreenTests
    {
        [Test]
        public void ANewScreenOpensOnWorks()
        {
            Assert.AreEqual(PrototypeScreen.Works, PrototypeScreen.Open(StudyCondition.Prototype, null));
            Assert.AreEqual(PrototypeScreen.Works, PrototypeScreen.Open(StudyCondition.Prototype, "other"));
            Assert.IsTrue(PrototypeScreen.ShowsWorks(PrototypeScreen.Works));
        }

        [Test]
        public void ThePrototypeOpensWallAndRecommend()
        {
            Assert.AreEqual(PrototypeScreen.Wall, PrototypeScreen.Open(StudyCondition.Prototype, PrototypeScreen.Wall));
            Assert.AreEqual(PrototypeScreen.Recommend, PrototypeScreen.Open(StudyCondition.Prototype, PrototypeScreen.Recommend));
            Assert.IsTrue(PrototypeScreen.ShowsWall(PrototypeScreen.Wall));
            Assert.IsTrue(PrototypeScreen.ShowsRecommend(PrototypeScreen.Recommend));
            Assert.IsFalse(PrototypeScreen.ShowsWorks(PrototypeScreen.Wall));
        }

        [Test]
        public void BaselineStaysOnWorks()
        {
            Assert.AreEqual(PrototypeScreen.Works, PrototypeScreen.Open(StudyCondition.Baseline, PrototypeScreen.Wall));
            Assert.AreEqual(PrototypeScreen.Works, PrototypeScreen.Open(StudyCondition.Baseline, PrototypeScreen.Recommend));
            Assert.AreEqual(PrototypeScreen.Works, PrototypeScreen.Open(StudyCondition.Baseline, PrototypeScreen.Works));
        }
    }
}
