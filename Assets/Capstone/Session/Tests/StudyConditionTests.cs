using NUnit.Framework;

namespace Capstone.Session.Tests
{
    public class StudyConditionTests
    {
        [Test]
        public void ANewSessionStartsInThePrototype()
        {
            BuyerSession session = new BuyerSession("local-preview");

            Assert.AreEqual(StudyCondition.Prototype, session.Condition);
            Assert.IsTrue(StudyCondition.Allows(session.Condition, StudyCondition.Placement));
            Assert.IsTrue(StudyCondition.Allows(session.Condition, StudyCondition.Dialogue));
            Assert.IsTrue(StudyCondition.Allows(session.Condition, StudyCondition.Ranking));
            Assert.IsTrue(StudyCondition.Allows(session.Condition, StudyCondition.Catalogue));
        }

        [Test]
        public void BaselineKeepsTheCatalogueAndTurnsTheRestOff()
        {
            BuyerSession session = new BuyerSession("local-preview");
            session.UseBaseline();

            Assert.AreEqual(StudyCondition.Baseline, session.Condition);
            Assert.IsTrue(StudyCondition.Allows(session.Condition, StudyCondition.Catalogue));
            Assert.IsFalse(StudyCondition.Allows(session.Condition, StudyCondition.Placement));
            Assert.IsFalse(StudyCondition.Allows(session.Condition, StudyCondition.Dialogue));
            Assert.IsFalse(StudyCondition.Allows(session.Condition, StudyCondition.Ranking));
        }

        [Test]
        public void PrototypeTurnsTheFeaturesBackOn()
        {
            BuyerSession session = new BuyerSession("local-preview");
            session.UseBaseline();
            session.UsePrototype();

            Assert.AreEqual(StudyCondition.Prototype, session.Condition);
            Assert.IsTrue(StudyCondition.Allows(session.Condition, StudyCondition.Placement));
        }

        [Test]
        public void AnUnknownConditionStaysOn()
        {
            Assert.IsTrue(StudyCondition.Allows(string.Empty, StudyCondition.Placement));
            Assert.IsTrue(StudyCondition.Allows(null, StudyCondition.Ranking));
        }
    }
}
