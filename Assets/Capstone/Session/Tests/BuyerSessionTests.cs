using System;
using NUnit.Framework;

namespace Capstone.Session.Tests
{
    public class BuyerSessionTests
    {
        static readonly DateTime FirstTime = new DateTime(2026, 10, 24, 18, 0, 0, DateTimeKind.Utc);
        static readonly DateTime SecondTime = new DateTime(2026, 10, 24, 18, 1, 0, DateTimeKind.Utc);

        [Test]
        public void ANewSessionHasNoEventsAndNoConsent()
        {
            BuyerSession session = new BuyerSession("local-preview");

            Assert.AreEqual("local-preview", session.ParticipantCode);
            Assert.IsFalse(session.ConsentLogged);
            Assert.AreEqual(0, session.Count);
        }

        [Test]
        public void KeepThenDismissStayInOrder()
        {
            BuyerSession session = new BuyerSession("local-preview");
            session.MarkConsent();

            session.LogAction("sample-wall", "Keep", "size", FirstTime);
            session.LogAction("sample-wall", "Dismiss", "size", SecondTime);

            Assert.AreEqual(2, session.Count);
            Assert.AreEqual("action", session.EventAt(0).Kind);
            Assert.AreEqual("Keep", session.EventAt(0).Name);
            Assert.AreEqual("sample-wall", session.EventAt(0).ArtworkId);
            Assert.AreEqual("size", session.EventAt(0).Concern);
            Assert.AreEqual(FirstTime, session.EventAt(0).Utc);
            Assert.AreEqual("Dismiss", session.EventAt(1).Name);
            Assert.AreEqual(SecondTime, session.EventAt(1).Utc);
        }

        [Test]
        public void AMatchedRuleIsStoredWithItsConcern()
        {
            BuyerSession session = new BuyerSession("local-preview");
            session.MarkConsent();

            session.LogRule("sample-wall", "size", "size", FirstTime);

            Assert.AreEqual(1, session.Count);
            Assert.AreEqual("rule", session.EventAt(0).Kind);
            Assert.AreEqual("size", session.EventAt(0).Name);
            Assert.AreEqual("size", session.EventAt(0).Concern);
        }

        [Test]
        public void LoggingDoesNotMarkConsent()
        {
            BuyerSession session = new BuyerSession("local-preview");

            Assert.IsFalse(session.LogAction("sample-wall", "Save", string.Empty, FirstTime));
            Assert.IsFalse(session.LogRule("sample-wide", "colour", "colour", SecondTime));

            Assert.IsFalse(session.ConsentLogged);
            Assert.AreEqual(0, session.Count);
        }

        [Test]
        public void ConsentAllowsLaterEventsAndDoesNotFillEarlierOnes()
        {
            BuyerSession session = new BuyerSession("local-preview");

            Assert.IsFalse(session.LogAction("sample-wall", "Keep", "size", FirstTime));
            session.MarkConsent();
            session.MarkConsent();

            Assert.IsTrue(session.ConsentLogged);
            Assert.AreEqual(0, session.Count);
            Assert.IsTrue(session.LogAction("sample-wall", "Dismiss", "size", SecondTime));
            Assert.AreEqual(1, session.Count);
            Assert.AreEqual("Dismiss", session.EventAt(0).Name);
        }

        [Test]
        public void AChosenRankIsStoredAfterConsent()
        {
            BuyerSession session = new BuyerSession("local-preview");

            Assert.IsFalse(session.LogChoice("sample-calm-blue", 1, "colour", FirstTime));
            session.MarkConsent();

            Assert.AreEqual(0, session.Count);
            Assert.IsTrue(session.LogChoice("sample-calm-blue", 1, "colour", SecondTime));
            Assert.AreEqual(1, session.Count);
            Assert.AreEqual("choice", session.EventAt(0).Kind);
            Assert.AreEqual("1", session.EventAt(0).Name);
            Assert.AreEqual("sample-calm-blue", session.EventAt(0).ArtworkId);
            Assert.AreEqual("colour", session.EventAt(0).Concern);
            Assert.AreEqual(SecondTime, session.EventAt(0).Utc);
        }

        [Test]
        public void AChoiceMustBeRankOneTwoOrThree()
        {
            BuyerSession session = new BuyerSession("local-preview");
            session.MarkConsent();

            ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(
                () => session.LogChoice("sample-wall", 0, string.Empty, FirstTime));

            Assert.AreEqual("oneBasedRank", error.ParamName);
            Assert.Throws<ArgumentOutOfRangeException>(
                () => session.LogChoice("sample-wall", 4, string.Empty, FirstTime));
            Assert.AreEqual(0, session.Count);
        }

        [Test]
        public void AnEventNeedsAnArtworkId()
        {
            BuyerSession session = new BuyerSession("local-preview");
            session.MarkConsent();

            ArgumentException error = Assert.Throws<ArgumentException>(
                () => session.LogAction("  ", "Keep", string.Empty, FirstTime));

            Assert.AreEqual("artworkId", error.ParamName);
            Assert.Throws<ArgumentException>(
                () => session.LogChoice(" ", 1, string.Empty, FirstTime));
            Assert.AreEqual(0, session.Count);
        }

        [Test]
        public void ASessionNeedsAParticipantCode()
        {
            ArgumentException error = Assert.Throws<ArgumentException>(() => new BuyerSession("  "));

            Assert.AreEqual("participantCode", error.ParamName);
        }
    }
}
