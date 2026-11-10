using NUnit.Framework;

namespace Capstone.Catalogue.Tests
{
    public class PieceInRoomTests
    {
        [Test]
        public void SizeUsesTheStoredCentimetresInEachLanguage()
        {
            Artwork work = Sample();

            StringAssert.Contains("40 cm wide and 50 cm tall.", PieceInRoom.Fact("size", "en", work, 0f, 0f));
            StringAssert.Contains("40 cm de large et 50 cm de haut.", PieceInRoom.Fact("size", "fr", work, 0f, 0f));
            StringAssert.Contains("40 cm", PieceInRoom.Fact("size", "rw", work, 0f, 0f));
            StringAssert.Contains("50 cm", PieceInRoom.Fact("size", "rw", work, 0f, 0f));
        }

        [Test]
        public void FitComparesTheWorkWithTheWallOnScreen()
        {
            string text = PieceInRoom.Fact("fit", "en", Sample(), 240f, 240f);

            StringAssert.Contains("40 cm wide and 50 cm tall.", text);
            StringAssert.Contains("240 cm wide and 240 cm tall.", text);
            StringAssert.Contains("fits inside that wall.", text);
            StringAssert.Contains("17%", text);
            StringAssert.Contains("21%", text);
        }

        [Test]
        public void AWorkLargerThanTheWallSaysSo()
        {
            Artwork work = new Artwork(
                "wide",
                "Wide",
                "A",
                2020,
                "Oil",
                100f,
                300f,
                "abstract",
                "calm",
                "blue",
                "mid",
                string.Empty,
                StoryNotes.Empty);

            string text = PieceInRoom.Fact("fit", "en", work, 240f, 240f);

            StringAssert.Contains("wider than that wall.", text);
            StringAssert.DoesNotContain("taller than that wall.", text);
            StringAssert.Contains("125%", text);
        }

        [Test]
        public void SizeOnAKnownWallAlsoSaysHowItFits()
        {
            string text = PieceInRoom.Fact("size", "en", Sample(), 240f, 240f);

            StringAssert.Contains("fits inside that wall.", text);
            StringAssert.Contains("240 cm", text);
        }

        [Test]
        public void AMissingWallIsNotGivenASize()
        {
            string text = PieceInRoom.Fact("fit", "en", Sample(), 0f, 0f);

            StringAssert.Contains("not stored.", text);
            StringAssert.DoesNotContain("%", text);
        }

        [Test]
        public void StoredFieldsAreRepeatedAndEmptyOnesStayEmpty()
        {
            Artwork work = Sample();

            StringAssert.Contains("blue", PieceInRoom.Fact("colour", "en", work, 0f, 0f));
            StringAssert.Contains("The blue is the evening.", PieceInRoom.Fact("meaning", "en", work, 0f, 0f));
            StringAssert.Contains("mid", PieceInRoom.Fact("price", "en", work, 0f, 0f));
            StringAssert.Contains("Sample artist", PieceInRoom.Fact("artist", "en", work, 0f, 0f));
            StringAssert.Contains("2024", PieceInRoom.Fact("artist", "en", work, 0f, 0f));
            StringAssert.Contains("Acrylic", PieceInRoom.Fact("medium", "en", work, 0f, 0f));
            StringAssert.Contains("portrait", PieceInRoom.Fact("style", "en", work, 0f, 0f));
            StringAssert.Contains("calm", PieceInRoom.Fact("mood", "fr", work, 0f, 0f));
            StringAssert.Contains("n'est pas enregistré.", PieceInRoom.Fact("meaning", "fr", Bare(), 0f, 0f));
        }

        [Test]
        public void LightHasNoStoredReading()
        {
            Assert.IsNull(PieceInRoom.Fact("light", "en", Sample(), 240f, 240f));
            Assert.IsNull(PieceInRoom.Fact("unknown", "en", Sample(), 240f, 240f));
            Assert.IsNull(PieceInRoom.Fact("size", "en", null, 240f, 240f));
        }

        static Artwork Sample()
        {
            return new Artwork(
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
        }

        static Artwork Bare()
        {
            return new Artwork(
                "bare",
                "Bare",
                string.Empty,
                2020,
                string.Empty,
                10f,
                10f,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                StoryNotes.Empty);
        }
    }
}
