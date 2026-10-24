using System;
using System.Collections.Generic;

namespace Capstone.Session
{
    /// <summary>
    /// The events of one buyer session, kept in memory on the device.
    /// Consent is not marked here. That is recorded later, before a study session.
    /// An unmatched question is not logged, because no rule was selected.
    /// </summary>
    public sealed class BuyerSession
    {
        readonly List<SessionEvent> events = new List<SessionEvent>();

        public BuyerSession(string participantCode)
        {
            if (string.IsNullOrWhiteSpace(participantCode))
                throw new ArgumentException("A session needs a participant code.", nameof(participantCode));

            ParticipantCode = participantCode;
        }

        public string ParticipantCode { get; }

        public bool ConsentLogged { get { return false; } }

        public int Count { get { return events.Count; } }

        public SessionEvent EventAt(int index)
        {
            return events[index];
        }

        public void LogAction(string artworkId, string action, string concern, DateTime utc)
        {
            events.Add(new SessionEvent(artworkId, "action", action, concern, utc));
        }

        public void LogRule(string artworkId, string ruleId, string concern, DateTime utc)
        {
            events.Add(new SessionEvent(artworkId, "rule", ruleId, concern, utc));
        }
    }
}
