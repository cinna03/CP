using System;

namespace Capstone.Session
{
    /// <summary>
    /// One stored moment in a buyer session. Kind is "action" or "rule".
    /// The time is the time the caller supplies, so a test can fix it.
    /// </summary>
    public sealed class SessionEvent
    {
        public SessionEvent(string artworkId, string kind, string name, string concern, DateTime utc)
        {
            if (string.IsNullOrWhiteSpace(artworkId))
                throw new ArgumentException("An event needs an artwork id.", nameof(artworkId));

            ArtworkId = artworkId;
            Kind = kind ?? string.Empty;
            Name = name ?? string.Empty;
            Concern = string.IsNullOrWhiteSpace(concern) ? string.Empty : concern;
            Utc = utc;
        }

        public string ArtworkId { get; }

        public string Kind { get; }

        public string Name { get; }

        public string Concern { get; }

        public DateTime Utc { get; }
    }
}
