using Capstone.App.Data;
using NUnit.Framework;
using UnityEngine;

namespace Capstone.App.Tests
{
    public class OwnPieceRulesTests
    {
        [TestCase("60", 60f)]
        [TestCase("60.5", 60.5f)]
        [TestCase("60,5", 60.5f)]
        [TestCase(" 45 cm ", 45f)]
        public void CentimetresAreReadWithAPointOrAComma(string text, float expected)
        {
            Assert.IsTrue(OwnPieceRules.TryParseCentimetres(text, out float value));
            Assert.AreEqual(expected, value, 1e-4f);
        }

        [Test]
        public void APhotoIsRequiredFirst()
        {
            Assert.AreEqual(OwnPieceProblem.MissingPhoto, OwnPieceRules.Validate(false, "40", "50", out _, out _));
        }

        [TestCase("", "50", OwnPieceProblem.MissingSize)]
        [TestCase("abc", "50", OwnPieceProblem.MissingSize)]
        [TestCase("3", "50", OwnPieceProblem.TooSmall)]
        [TestCase("40", "500", OwnPieceProblem.TooLarge)]
        [TestCase("40", "50", OwnPieceProblem.None)]
        public void SizesAreChecked(string width, string height, OwnPieceProblem expected)
        {
            Assert.AreEqual(expected, OwnPieceRules.Validate(true, width, height, out _, out _));
        }

        [Test]
        public void AMatchingPhotoHasNoShapeWarning()
        {
            Assert.IsFalse(OwnPieceRules.ShapeMismatch(800, 1000, 40f, 50f));
        }

        [Test]
        public void SwappedSidesAreDetected()
        {
            Assert.IsTrue(OwnPieceRules.ShapeMismatch(800, 1000, 50f, 40f));
            Assert.IsTrue(OwnPieceRules.LooksSwapped(800, 1000, 50f, 40f));
        }

        [Test]
        public void AnOwnPieceBecomesAnArtworkAtTheEnteredSize()
        {
            ArtworkData work = ArtworkData.CreateOwnPiece("My print", new Vector2(40f, 50f), null);
            StringAssert.StartsWith(ArtworkData.OwnPiecePrefix, work.Identifier);
            Assert.IsTrue(work.IsOwnPiece);
            Assert.AreEqual("My print", work.Title);
            Assert.AreEqual(new Vector2(0.4f, 0.5f), work.GetDimensionsInMeters(0));
        }

        [Test]
        public void AnUntitledOwnPieceGetsAName()
        {
            ArtworkData work = ArtworkData.CreateOwnPiece("  ", new Vector2(30f, 30f), null);
            Assert.AreEqual(ArtworkData.OwnPieceTitle, work.Title);
        }
    }
}
