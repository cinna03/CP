using NUnit.Framework;

namespace Capstone.Catalogue.Tests
{
    public class ArtworkRecordTests
    {
        [Test]
        public void RecordKeepsTheCatalogueFields()
        {
            Artwork work = new Artwork(
                "work-1",
                "Sample wall work",
                "A. Maker",
                2019,
                "Oil",
                50f,
                40f,
                "Quiet",
                "Calm",
                "Blue",
                "Mid",
                "sample.png",
                "Painted for a small room.");

            Assert.AreEqual("work-1", work.Id);
            Assert.AreEqual("Sample wall work", work.Title);
            Assert.AreEqual("A. Maker", work.Artist);
            Assert.AreEqual(2019, work.Year);
            Assert.AreEqual("Oil", work.Medium);
            Assert.AreEqual(50f, work.HeightCentimetres);
            Assert.AreEqual(40f, work.WidthCentimetres);
            Assert.AreEqual("Quiet", work.Style);
            Assert.AreEqual("Calm", work.Mood);
            Assert.AreEqual("Blue", work.Colour);
            Assert.AreEqual("Mid", work.PriceBand);
            Assert.AreEqual("sample.png", work.ImageName);
            Assert.AreEqual("Painted for a small room.", work.StoryNote);
        }

        [Test]
        public void MissingIdIsRejected()
        {
            Assert.Throws<System.ArgumentException>(() => new Artwork(
                " ",
                "Sample",
                "Artist",
                2020,
                "Paint",
                50f,
                40f,
                "Quiet",
                "Calm",
                "Blue",
                "Mid",
                "sample.png",
                "A note."));
        }

        [Test]
        public void EmptyStoryNoteStaysEmpty()
        {
            Artwork work = new Artwork(
                "work-2",
                "Untitled",
                "",
                0,
                "",
                30f,
                20f,
                "",
                "",
                "",
                "",
                "",
                null);

            Assert.AreEqual("", work.StoryNote);
        }
    }
}
