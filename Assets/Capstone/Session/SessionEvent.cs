using System;

namespace Capstone.Session
{
    /// <summary>
    /// One stored moment in a buyer session. Kind is "action", "rule", or "choice".
    /// The time is the time the caller supplies, so a test can fix it.
    /// An action may store up to three recommended artwork ids, in rank order.
    /// </summary>
    public sealed class SessionEvent
    {
        readonly string[] recommendedIds;

        public SessionEvent(string artworkId, string kind, string name, string concern, DateTime utc)
            : this(artworkId, kind, name, concern, utc, null)
        {
        }

        public SessionEvent(string artworkId, string kind, string name, string concern, DateTime utc, string[] recommendedIds)
        {
            if (string.IsNullOrWhiteSpace(artworkId))
                throw new ArgumentException("An event needs an artwork id.", nameof(artworkId));

            ArtworkId = artworkId;
            Kind = kind ?? string.Empty;
            Name = name ?? string.Empty;
            Concern = string.IsNullOrWhiteSpace(concern) ? string.Empty : concern;
            Utc = utc;
            this.recommendedIds = CopyRecommended(recommendedIds);
        }

        public string ArtworkId { get; }

        public string Kind { get; }

        public string Name { get; }

        public string Concern { get; }

        public DateTime Utc { get; }

        public int RecommendedCount { get { return recommendedIds.Length; } }

        public string RecommendedIdAt(int index)
        {
            return recommendedIds[index];
        }

        static string[] CopyRecommended(string[] recommendedIds)
        {
            if (recommendedIds == null || recommendedIds.Length == 0)
                return new string[0];
            if (recommendedIds.Length > 3)
                throw new ArgumentException("An action stores at most three recommended ids.", nameof(recommendedIds));

            string[] copy = new string[recommendedIds.Length];
            for (int i = 0; i < recommendedIds.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(recommendedIds[i]))
                    throw new ArgumentException("A recommended id is required.", nameof(recommendedIds));
                copy[i] = recommendedIds[i];
            }

            return copy;
        }
    }
}
