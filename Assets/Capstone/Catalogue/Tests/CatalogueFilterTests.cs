using System.Collections.Generic;
using NUnit.Framework;

namespace Capstone.Catalogue.Tests
{
    public class CatalogueFilterTests
    {
        [Test]
        public void StyleNarrowsTheList()
        {
            CatalogueFilter catalogue = Sample();

            IReadOnlyList<Artwork> quiet = catalogue.Matching("Quiet", "", "");

            Assert.AreEqual(1, quiet.Count);
            Assert.AreEqual("blue", quiet[0].Id);
        }

        [Test]
        public void MoodAndColourNarrowTheList()
        {
            CatalogueFilter catalogue = Sample();

            IReadOnlyList<Artwork> found = catalogue.Matching("", "Calm", "Blue");

            Assert.AreEqual(1, found.Count);
            Assert.AreEqual("blue", found[0].Id);
        }

        [Test]
        public void EmptyFilterReturnsEveryWork()
        {
            CatalogueFilter catalogue = Sample();

            Assert.AreEqual(2, catalogue.Matching("", "", "").Count);
            Assert.AreEqual(2, catalogue.All.Count);
        }

        [Test]
        public void UnknownStyleReturnsNothing()
        {
            CatalogueFilter catalogue = Sample();

            Assert.AreEqual(0, catalogue.Matching("Loud", "", "").Count);
        }

        [Test]
        public void FindReturnsTheStoredNote()
        {
            CatalogueFilter catalogue = Sample();

            Assert.AreEqual("A blue note.", catalogue.Find("blue").StoryNote);
            Assert.AreEqual("", catalogue.Find("plain").StoryNote);
            Assert.IsNull(catalogue.Find("missing"));
        }

        static CatalogueFilter Sample()
        {
            return new CatalogueFilter(new[]
            {
                Work("blue", "Quiet", "Calm", "Blue", "A blue note."),
                Work("plain", "Bold", "Bright", "Red", "")
            });
        }

        static Artwork Work(string id, string style, string mood, string colour, string note)
        {
            return new Artwork(id, id, "Artist", 2020, "Paint", 50f, 40f, style, mood, colour, "Mid", id + ".png", note);
        }
    }
}
