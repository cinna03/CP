using Capstone.Catalogue;
using NUnit.Framework;

namespace Capstone.Dialogue.Tests
{
    public class RuleTableTests
    {
        [Test]
        public void EnglishSizeIncludesTheStoredCentimetres()
        {
            string reply = RuleTable.Answer(Sample(), "what size is it?");

            StringAssert.Contains("40", reply);
            StringAssert.Contains("50", reply);
            StringAssert.Contains("cm", reply);
        }

        [Test]
        public void FrenchSizeStaysInFrench()
        {
            string reply = RuleTable.Answer(Sample(), "quelle est la taille?");

            StringAssert.Contains("40", reply);
            StringAssert.Contains("largeur", reply);
            StringAssert.DoesNotContain("wide", reply);
        }

        [Test]
        public void KinyarwandaSizeStaysInKinyarwanda()
        {
            string reply = RuleTable.Answer(Sample(), "ingano yayo ni iyihe?");

            StringAssert.Contains("40", reply);
            StringAssert.Contains("ubugari", reply);
        }

        [Test]
        public void UnknownQuestionReturnsTheFixedLineAndNoNewFact()
        {
            string reply = RuleTable.Answer(Sample(), "hello");

            Assert.AreEqual(RuleTable.FixedLine, reply);
            StringAssert.DoesNotContain("40", reply);
            StringAssert.DoesNotContain("Blue", reply);
        }

        static Artwork Sample()
        {
            return new Artwork(
                "blue",
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
                "blue.png",
                "A blue note.");
        }
    }
}
