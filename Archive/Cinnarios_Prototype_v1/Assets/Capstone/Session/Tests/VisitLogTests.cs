using System.IO;
using Capstone.Ranking;
using NUnit.Framework;

namespace Capstone.Session.Tests
{
    public class VisitLogTests
    {
        [Test]
        public void ThereAreTwentyFiveCodes()
        {
            Assert.AreEqual(25, EntranceCodes.All.Count);
            Assert.IsTrue(EntranceCodes.Accepts("azure"));
            Assert.IsFalse(EntranceCodes.Accepts("nope"));
        }

        [Test]
        public void AValidCodeRecordsTheQuestionAndTheDecision()
        {
            string path = TempPath();
            VisitLog log = new VisitLog();

            Assert.IsTrue(log.Open("azure"));
            log.Question("what size is it?", "40 cm wide");
            log.Decide("blue", Decision.Keep);
            Assert.IsTrue(log.Write(path));

            string text = File.ReadAllText(path);
            StringAssert.Contains("code AZURE", text);
            StringAssert.Contains("question what size is it? | 40 cm wide", text);
            StringAssert.Contains("decision keep blue", text);
            File.Delete(path);
        }

        [Test]
        public void AnInvalidCodeWritesNothing()
        {
            string path = TempPath();
            VisitLog log = new VisitLog();

            Assert.IsFalse(log.Open("nope"));
            log.Question("what size is it?", "40 cm wide");
            log.Decide("blue", Decision.Keep);
            Assert.IsFalse(log.Write(path));
            Assert.AreEqual("", log.Text);
            Assert.IsFalse(File.Exists(path));
        }

        static string TempPath()
        {
            return Path.Combine(Path.GetTempPath(), "capstone-visit-" + Path.GetRandomFileName() + ".txt");
        }
    }
}
