using NUnit.Framework;

namespace Capstone.Catalogue.Tests
{
    public class ArtworkScaleTests
    {
        [Test]
        public void FortyByFiftyCentimetresBecomesPointFourByPointFiveMetres()
        {
            Artwork work = Sample(heightCentimetres: 50f, widthCentimetres: 40f);

            Assert.AreEqual(0.40f, work.WidthMetres, 0.0001f);
            Assert.AreEqual(0.50f, work.HeightMetres, 0.0001f);
            Assert.AreEqual(0.40f, ArtworkScale.ToMetres(40f), 0.0001f);
        }

        [Test]
        public void ZeroHeightIsRejected()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => Sample(heightCentimetres: 0f, widthCentimetres: 40f));
        }

        [Test]
        public void NegativeWidthIsRejected()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => Sample(heightCentimetres: 50f, widthCentimetres: -1f));
        }

        static Artwork Sample(float heightCentimetres, float widthCentimetres)
        {
            return new Artwork(
                "work-1",
                "Sample",
                "Artist",
                2020,
                "Paint",
                heightCentimetres,
                widthCentimetres,
                "Quiet",
                "Calm",
                "Blue",
                "Mid",
                "sample.png",
                "A short note.");
        }
    }
}
