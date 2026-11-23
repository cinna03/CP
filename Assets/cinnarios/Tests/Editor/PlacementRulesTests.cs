using System;
using Cinnarios.Catalogue.Pricing;
using Cinnarios.Launch;
using Cinnarios.Painting.Data;
using Cinnarios.Placement;
using NUnit.Framework;

namespace Cinnarios.Tests
{
    public sealed class PlacementRulesTests
    {
        [Test]
        public void CentimetresBecomeMetresInOnePlace()
        {
            var size = new PaintingDimensions(60f, 80f, 2f);

            Assert.AreEqual(0.6f, size.SizeInMetres.x, 0.0001f);
            Assert.AreEqual(0.8f, size.SizeInMetres.y, 0.0001f);
            Assert.AreEqual(0.02f, size.SizeInMetres.z, 0.0001f);
        }

        [Test]
        public void AMissingSizeIsRefused()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PaintingDimensions(0f, 80f, 2f));
        }

        [Test]
        public void ASixtyByEightyPrintCostsThreeHundredAndFortyDollars()
        {
            var price = new AreaPricingRule().PriceFor(new PaintingDimensions(60f, 80f, 2f));

            Assert.AreEqual(340m, price);
        }

        [Test]
        public void ATryLinkKeepsThePaintingAndTheShop()
        {
            bool parsed = TryLink.TryParse(
                "cinnarios-ar://try/436535?api=https%3A%2F%2Fcinnarios.onrender.com",
                out TryLink link,
                out string error);

            Assert.IsTrue(parsed, error);
            Assert.AreEqual("436535", link.SourceId);
            Assert.AreEqual("https://cinnarios.onrender.com", link.ApiBaseUrl);
        }

        [Test]
        public void AnOwnPieceLinkIsNotATry()
        {
            bool parsed = TryLink.TryParse(
                "cinnarios-ar://own/photo?w=40&h=50&api=https%3A%2F%2Fcinnarios.onrender.com",
                out _,
                out _);

            Assert.IsFalse(parsed);
        }

        [Test]
        public void ScanningCanAimAndCannotLock()
        {
            var states = new PlacementStateMachine();

            Assert.IsFalse(states.TryTransition(PlacementState.Locked));
            Assert.AreEqual(PlacementState.Scanning, states.Current);
            Assert.IsTrue(states.TryTransition(PlacementState.Aiming));
            Assert.IsTrue(states.TryTransition(PlacementState.Positioning));
            Assert.IsTrue(states.TryTransition(PlacementState.Locked));
            Assert.IsFalse(states.TryTransition(PlacementState.Aiming));
            Assert.IsTrue(states.TryTransition(PlacementState.Positioning));
        }
    }
}
