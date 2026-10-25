using System;
using System.Collections.Generic;

namespace Capstone.Session
{
    /// <summary>
    /// The events of one buyer session, kept in memory on the device.
    /// Consent starts unmarked. A keep, save, dismiss, matched question, or chosen rank is stored only after that.
    /// Marking consent does not invent earlier events. An unmatched question is not logged.
    /// A choice is rank 1, 2, or 3. A missing rank is not stored.
    /// A keep, save, or dismiss stores up to three recommended artwork ids, in rank order.
    /// </summary>
    public sealed class BuyerSession
    {
        readonly List<SessionEvent> events = new List<SessionEvent>();
        bool consentLogged;

        public BuyerSession(string participantCode)
        {
            if (string.IsNullOrWhiteSpace(participantCode))
                throw new ArgumentException("A session needs a participant code.", nameof(participantCode));

            ParticipantCode = participantCode;
            Condition = StudyCondition.Prototype;
        }

        public string ParticipantCode { get; }

        public string Condition { get; private set; }

        public bool ConsentLogged { get { return consentLogged; } }

        public int Count { get { return events.Count; } }

        public SessionEvent EventAt(int index)
        {
            return events[index];
        }

        public void UseBaseline()
        {
            Condition = StudyCondition.Baseline;
        }

        public void UsePrototype()
        {
            Condition = StudyCondition.Prototype;
        }

        public void MarkConsent()
        {
            consentLogged = true;
        }

        public bool LogAction(string artworkId, string action, string concern, DateTime utc)
        {
            return LogAction(artworkId, action, concern, utc, null);
        }

        public bool LogAction(string artworkId, string action, string concern, DateTime utc, string[] recommendedIds)
        {
            return Store(new SessionEvent(artworkId, "action", action, concern, utc, recommendedIds));
        }

        public bool LogRule(string artworkId, string ruleId, string concern, DateTime utc)
        {
            return Store(new SessionEvent(artworkId, "rule", ruleId, concern, utc));
        }

        public bool LogChoice(string artworkId, int oneBasedRank, string concern, DateTime utc)
        {
            if (oneBasedRank < 1 || oneBasedRank > 3)
                throw new ArgumentOutOfRangeException(nameof(oneBasedRank), "A choice is rank 1, 2, or 3.");

            return Store(new SessionEvent(artworkId, "choice", oneBasedRank.ToString(), concern, utc));
        }

        bool Store(SessionEvent stored)
        {
            if (!consentLogged)
                return false;
            events.Add(stored);
            return true;
        }
    }
}
