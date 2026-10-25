using System.Collections.Generic;
using NUnit.Framework;

namespace Capstone.Catalogue.Tests
{
    public class CatalogueSequenceTests
    {
        [Test]
        public void TheNextWorkFollowsTheStoredOrder()
        {
            List<Artwork> works = ThreeWorks();

            Artwork next = CatalogueSequence.Next(works, "a");

            Assert.AreEqual("b", next.Id);
            Assert.AreEqual(80f, next.WidthCm);
            Assert.AreEqual(30f, next.HeightCm);
        }

        [Test]
        public void TheLastWorkWrapsToTheFirst()
        {
            List<Artwork> works = ThreeWorks();

            Assert.AreEqual("a", CatalogueSequence.Next(works, "c").Id);
        }

        [Test]
        public void AnUnknownCurrentWorkStartsAtTheFirst()
        {
            List<Artwork> works = ThreeWorks();

            Assert.AreEqual("a", CatalogueSequence.Next(works, "missing").Id);
            Assert.AreEqual("a", CatalogueSequence.Next(works, null).Id);
        }

        [Test]
        public void AnEmptyCatalogueHasNoNextWork()
        {
            Assert.IsNull(CatalogueSequence.Next(new List<Artwork>(), "a"));
            Assert.IsNull(CatalogueSequence.Next(null, "a"));
        }

        static List<Artwork> ThreeWorks()
        {
            return new List<Artwork>
            {
                Work("a", 50f, 40f),
                Work("b", 30f, 80f),
                Work("c", 70f, 50f),
            };
        }

        static Artwork Work(string id, float heightCm, float widthCm)
        {
            return new Artwork(
                id,
                id,
                "Sample artist",
                2024,
                "Test print",
                heightCm,
                widthCm,
                "sample",
                "calm",
                "blue",
                "none",
                id,
                StoryNotes.Empty);
        }
    }
}
