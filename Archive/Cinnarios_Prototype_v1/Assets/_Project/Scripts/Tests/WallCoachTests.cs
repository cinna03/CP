using Capstone.App.AR;
using Capstone.App.Data;
using NUnit.Framework;

namespace Capstone.App.Tests
{
    public class WallCoachTests
    {
        [Test]
        public void AFirstTimeBuyerIsWalkedThroughThreeSteps()
        {
            var coach = new WallCoach(false);
            Assert.AreEqual(WallCoach.Step1, coach.Line(false, false, 0));
            Assert.AreEqual(WallCoach.Step2, coach.Line(true, false, 0));
            Assert.AreEqual(WallCoach.Step3, coach.Line(true, true, 0));
        }

        [Test]
        public void OnceLearnedTheBarStaysOutOfTheWayWhileAWorkIsUp()
        {
            var coach = new WallCoach(false);
            coach.MarkLearned();
            Assert.IsNull(coach.Line(true, true, 0));
            Assert.AreEqual(WallCoach.Locked, coach.Line(true, false, 2));
        }
    }

    public class VisitOutcomeTests
    {
        [Test]
        public void NoTriedWorksMeansNoPick()
        {
            VisitOutcome outcome = VisitOutcome.From(null, piece => 0);
            Assert.IsNull(outcome.Work);
            Assert.AreEqual(0, outcome.Tried);
        }
    }
}
