using System;
using NUnit.Framework;

namespace Capstone.Session.Tests
{
    public class SessionTranscriptTests
    {
        static readonly DateTime When = new DateTime(2026, 10, 31, 18, 0, 0, DateTimeKind.Utc);

        [Test]
        public void TheTranscriptListsConsentAndTheStoredEventsInOrder()
        {
            BuyerSession session;
            Assert.IsTrue(BuyerSession.TryStart("azure", out session));
            session.LogAction("sample-wall", "Keep", "colour", When, new[] { "sample-calm-blue", "sample-quiet", "sample-wide" });
            session.LogRule("sample-wall", "colour", "colour", When);
            session.LogChoice("sample-calm-blue", 1, "colour", When);

            string text = SessionTranscript.Format(session);

            StringAssert.Contains("participant AZURE", text);
            StringAssert.Contains("consent marked", text);
            StringAssert.Contains("action sample-wall Keep colour", text);
            StringAssert.Contains("rule sample-wall colour colour", text);
            StringAssert.Contains("choice sample-calm-blue 1 colour", text);
            int actionAt = text.IndexOf("action ", StringComparison.Ordinal);
            int ruleAt = text.IndexOf("rule ", StringComparison.Ordinal);
            int choiceAt = text.IndexOf("choice ", StringComparison.Ordinal);
            Assert.Less(actionAt, ruleAt);
            Assert.Less(ruleAt, choiceAt);
        }

        [Test]
        public void ASessionWithoutConsentPrintsUnmarkedAndNoEvents()
        {
            BuyerSession session = new BuyerSession("local-preview");
            session.LogAction("sample-wall", "Keep", "size", When);

            string text = SessionTranscript.Format(session);

            StringAssert.Contains("consent unmarked", text);
            StringAssert.DoesNotContain("Keep", text);
        }

        [Test]
        public void AnEmptySessionPrintsNothing()
        {
            Assert.AreEqual(string.Empty, SessionTranscript.Format(null));
        }
    }
}
