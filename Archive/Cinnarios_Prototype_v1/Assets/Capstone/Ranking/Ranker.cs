using System;
using System.Collections.Generic;
using Capstone.Catalogue;

namespace Capstone.Ranking
{
    /// <summary>
    /// Three other works for this visit. The work just tried is left out.
    /// </summary>
    public static class Ranker
    {
        public const int Count = 3;

        public static IReadOnlyList<Artwork> Three(IReadOnlyList<Artwork> works, Artwork current, VisitWeights weights)
        {
            if (works == null)
                throw new ArgumentNullException(nameof(works));
            if (current == null)
                throw new ArgumentNullException(nameof(current));
            if (weights == null)
                throw new ArgumentNullException(nameof(weights));

            var others = new List<Artwork>();
            for (int i = 0; i < works.Count; i++)
            {
                Artwork work = works[i];
                if (work != null && !string.Equals(work.Id, current.Id, StringComparison.Ordinal))
                    others.Add(work);
            }

            others.Sort(delegate (Artwork a, Artwork b)
            {
                int byScore = weights.Score(b).CompareTo(weights.Score(a));
                if (byScore != 0)
                    return byScore;
                return string.Compare(a.Id, b.Id, StringComparison.Ordinal);
            });

            if (others.Count > Count)
                others.RemoveRange(Count, others.Count - Count);
            return others;
        }
    }
}
