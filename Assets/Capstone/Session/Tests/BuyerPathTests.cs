using NUnit.Framework;

namespace Capstone.Session.Tests
{
    public class BuyerPathTests
    {
        [Test]
        public void ASessionStartsOnTagsAndTheCameraStaysClosed()
        {
            string step = BuyerPath.Start();

            Assert.AreEqual(BuyerPath.Tags, step);
            Assert.IsFalse(BuyerPath.OpensCamera(step));
        }

        [Test]
        public void TheBuyerReachesTheWallOnlyAfterTagsAndAWork()
        {
            string step = BuyerPath.Start();
            step = BuyerPath.Next(step, BuyerPath.Continue);
            Assert.AreEqual(BuyerPath.Catalogue, step);
            Assert.IsFalse(BuyerPath.OpensCamera(step));

            step = BuyerPath.Next(step, BuyerPath.Place);
            Assert.AreEqual(BuyerPath.Wall, step);
            Assert.IsTrue(BuyerPath.OpensCamera(step));
        }

        [Test]
        public void PlaceThenAskOrSkipThenRank()
        {
            string placed = BuyerPath.Next(BuyerPath.Wall, BuyerPath.Placed);
            Assert.AreEqual(BuyerPath.Question, placed);
            Assert.IsFalse(BuyerPath.OpensCamera(placed));

            Assert.AreEqual(BuyerPath.Rank, BuyerPath.Next(placed, BuyerPath.Send));
            Assert.AreEqual(BuyerPath.Rank, BuyerPath.Next(placed, BuyerPath.Skip));
        }

        [Test]
        public void AQuestionBeforePlacementDoesNotSkipTheWall()
        {
            Assert.AreEqual(BuyerPath.Wall, BuyerPath.Next(BuyerPath.Wall, BuyerPath.Send));
            Assert.AreEqual(BuyerPath.Wall, BuyerPath.Next(BuyerPath.Wall, BuyerPath.Skip));
        }

        [Test]
        public void ARecommendationReturnsToTheWall()
        {
            string step = BuyerPath.Next(BuyerPath.Rank, BuyerPath.Place);

            Assert.AreEqual(BuyerPath.Wall, step);
            Assert.IsTrue(BuyerPath.OpensCamera(step));
        }

        [Test]
        public void AfterTheRankTheBuyerSetsTagsAgainOrEnds()
        {
            Assert.AreEqual(BuyerPath.Tags, BuyerPath.Next(BuyerPath.Rank, BuyerPath.ChangeTags));
            Assert.AreEqual(BuyerPath.Ended, BuyerPath.Next(BuyerPath.Rank, BuyerPath.End));
            Assert.IsFalse(BuyerPath.OpensCamera(BuyerPath.Ended));
        }

        [Test]
        public void AnUnknownActionStaysOnTheSameStep()
        {
            Assert.AreEqual(BuyerPath.Catalogue, BuyerPath.Next(BuyerPath.Catalogue, "end"));
            Assert.AreEqual(BuyerPath.Tags, BuyerPath.Next(BuyerPath.Tags, BuyerPath.Place));
        }
    }
}
