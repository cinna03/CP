using System.Collections.Generic;
using NUnit.Framework;

namespace Capstone.Catalogue.Tests
{
    public class CatalogueFilterTests
    {
        [Test]
        public void ABlankFilterKeepsEveryWork()
        {
            List<Artwork> matches = CatalogueFilter.Matching(Works(), null, null, null, 0f, 0f);

            Assert.AreEqual(3, matches.Count);
            Assert.AreEqual("wide", matches[0].Id);
            Assert.AreEqual("blue", matches[1].Id);
            Assert.AreEqual("tall", matches[2].Id);
        }

        [Test]
        public void ColourMatchesWithoutCaringAboutCase()
        {
            List<Artwork> matches = CatalogueFilter.Matching(Works(), null, null, "BLUE", 0f, 0f);

            Assert.AreEqual(1, matches.Count);
            Assert.AreEqual("blue", matches[0].Id);
        }

        [Test]
        public void StyleAndMoodMustBothMatch()
        {
            List<Artwork> matches = CatalogueFilter.Matching(Works(), "sample", "calm", null, 0f, 0f);

            Assert.AreEqual(1, matches.Count);
            Assert.AreEqual("wide", matches[0].Id);
        }

        [Test]
        public void AWorkLargerThanTheMaximumIsLeftOut()
        {
            List<Artwork> matches = CatalogueFilter.Matching(Works(), null, null, null, 50f, 50f);

            Assert.AreEqual(1, matches.Count);
            Assert.AreEqual("blue", matches[0].Id);
            Assert.AreEqual(40f, matches[0].WidthCm);
            Assert.AreEqual(40f, matches[0].HeightCm);
        }

        [Test]
        public void AZeroMaximumDoesNotLimitSize()
        {
            List<Artwork> matches = CatalogueFilter.Matching(Works(), null, null, null, 0f, 0f);

            Assert.AreEqual(3, matches.Count);
        }

        [Test]
        public void AMissingCatalogueMatchesNothing()
        {
            Assert.AreEqual(0, CatalogueFilter.Matching(null, "sample", "calm", "blue", 50f, 50f).Count);
        }

        static List<Artwork> Works()
        {
            return new List<Artwork>
            {
                Work("wide", 30f, 80f, "sample", "calm", "orange"),
                Work("blue", 40f, 40f, "sample", "quiet", "blue"),
                Work("tall", 70f, 50f, "bold", "bright", "red"),
            };
        }

        static Artwork Work(string id, float heightCm, float widthCm, string style, string mood, string colour)
        {
            return new Artwork(
                id,
                id,
                "Sample artist",
                2024,
                "Test print",
                heightCm,
                widthCm,
                style,
                mood,
                colour,
                "none",
                id,
                StoryNotes.Empty);
        }
    }
}
