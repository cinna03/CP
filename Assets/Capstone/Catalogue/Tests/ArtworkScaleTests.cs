using NUnit.Framework;

namespace Capstone.Catalogue.Tests
{
    public class ArtworkScaleTests
    {
        [Test]
        public void FortyByFiftyCentimetresBecomesPointFourByPointFiveMetres()
        {
            Artwork work = new Artwork(
                "sample-1",
                "Sample title",
                "Sample artist",
                2024,
                "Acrylic",
                heightCm: 50f,
                widthCm: 40f,
                style: "portrait",
                mood: "calm",
                colour: "blue",
                priceBand: "mid",
                imageResource: "sample-image",
                storyNotes: StoryNotes.Empty);

            WallSize size = work.WallSizeMetres;

            Assert.AreEqual(0.40f, size.WidthMetres, 0.0001f);
            Assert.AreEqual(0.50f, size.HeightMetres, 0.0001f);
        }

        [Test]
        public void RejectsANonPositiveMeasurementBeforeDividing()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => ArtworkScale.ToWallSize(widthCm: 0f, heightCm: 50f));
        }
    }
}
