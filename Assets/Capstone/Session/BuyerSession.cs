using System;
using System.Collections.Generic;

namespace Capstone.Session
{
    /// <summary>
    /// The events of one buyer session, kept in memory on the device.
    /// Consent starts unmarked. A keep, save, dismiss, or matched question is stored only after that.
    /// Marking consent does not invent earlier events. An unmatched question is not logged.
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
            return Store(new SessionEvent(artworkId, "action", action, concern, utc));
        }

        public bool LogRule(string artworkId, string ruleId, string concern, DateTime utc)
        {
            return Store(new SessionEvent(artworkId, "rule", ruleId, concern, utc));
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
