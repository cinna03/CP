using System;
using NUnit.Framework;

namespace Capstone.Catalogue.Tests
{
    public class ArtworkRecordTests
    {
        [Test]
        public void StoresTheCatalogueFields()
        {
            Artwork work = Sample(heightCm: 50f, widthCm: 40f);

            Assert.AreEqual("sample-1", work.Id);
            Assert.AreEqual("Sample title", work.Title);
            Assert.AreEqual("Sample artist", work.Artist);
            Assert.AreEqual(2024, work.Year);
            Assert.AreEqual("Acrylic", work.Medium);
            Assert.AreEqual(50f, work.HeightCm);
            Assert.AreEqual(40f, work.WidthCm);
            Assert.AreEqual("portrait", work.Style);
            Assert.AreEqual("calm", work.Mood);
            Assert.AreEqual("blue", work.Colour);
            Assert.AreEqual("mid", work.PriceBand);
            Assert.AreEqual("sample-image", work.ImageResource);
            Assert.AreEqual("Background", work.StoryNotes.ArtistBackground);
            Assert.AreEqual("Inspiration", work.StoryNotes.Inspiration);
            Assert.AreEqual("Meaning", work.StoryNotes.Meaning);
        }

        [Test]
        public void RejectsAHeightThatIsNotPositive()
        {
            ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(
                () => Sample(heightCm: 0f, widthCm: 40f));

            Assert.AreEqual("heightCm", error.ParamName);
        }

        [Test]
        public void RejectsAWidthThatIsNotPositive()
        {
            ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(
                () => Sample(heightCm: 50f, widthCm: -1f));

            Assert.AreEqual("widthCm", error.ParamName);
        }

        [Test]
        public void RejectsAMissingId()
        {
            Assert.Throws<ArgumentException>(() => Sample(heightCm: 50f, widthCm: 40f, id: " "));
        }

        static Artwork Sample(float heightCm, float widthCm, string id = "sample-1")
        {
            return new Artwork(
                id,
                "Sample title",
                "Sample artist",
                2024,
                "Acrylic",
                heightCm,
                widthCm,
                "portrait",
                "calm",
                "blue",
                "mid",
                "sample-image",
                new StoryNotes("Background", "Inspiration", "Meaning"));
        }
    }
}
