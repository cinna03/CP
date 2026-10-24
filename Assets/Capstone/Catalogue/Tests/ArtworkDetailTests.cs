using NUnit.Framework;

namespace Capstone.Catalogue.Tests
{
    public class ArtworkDetailTests
    {
        [Test]
        public void DescribesTheStoredSizeAndStory()
        {
            Artwork work = new Artwork(
                "sample-1",
                "Sample title",
                "Sample artist",
                2024,
                "Acrylic",
                50f,
                40f,
                "portrait",
                "calm",
                "blue",
                "mid",
                "sample-image",
                new StoryNotes("Raised in Kigali.", "A quiet room.", "The blue is the evening."));

            string text = ArtworkDetail.Describe(work);

            StringAssert.Contains("Sample title", text);
            StringAssert.Contains("Sample artist", text);
            StringAssert.Contains("40 cm wide, 50 cm tall.", text);
            StringAssert.Contains("Raised in Kigali.", text);
            StringAssert.Contains("A quiet room.", text);
            StringAssert.Contains("The blue is the evening.", text);
        }

        [Test]
        public void AnEmptyNoteIsNotReplacedWithAStory()
        {
            Artwork work = new Artwork(
                "sample-1",
                "Sample title",
                "Sample artist",
                2024,
                "Acrylic",
                50f,
                40f,
                "portrait",
                "calm",
                "blue",
                "mid",
                "sample-image",
                StoryNotes.Empty);

            string text = ArtworkDetail.Describe(work);

            StringAssert.Contains("Artist background: " + ArtworkDetail.MissingNote, text);
            StringAssert.Contains("Inspiration: " + ArtworkDetail.MissingNote, text);
            StringAssert.Contains("Meaning: " + ArtworkDetail.MissingNote, text);
            Assert.IsFalse(text.Contains("born"));
        }

        [Test]
        public void NoWorkSaysThatNoneIsSelected()
        {
            Assert.AreEqual("No work is selected.", ArtworkDetail.Describe(null));
        }
    }
}
