using System.Collections.Generic;
using Capstone.Catalogue;

namespace Capstone.Ranking
{
    /// <summary>
    /// Scores the other catalogue works by tag overlap and returns the top three.
    /// Score is the style weight plus the mood weight plus the colour weight.
    /// A higher score wins. An equal score uses the artwork id, earlier id first.
    /// The work just acted on is left out. A dismiss does not delete it from the catalogue.
    /// </summary>
    public static class Recommender
    {
        public static int Score(Artwork artwork, PreferenceProfile profile)
        {
            if (artwork == null || profile == null)
                return 0;

            return profile.StyleWeight(artwork.Style)
                + profile.MoodWeight(artwork.Mood)
                + profile.ColourWeight(artwork.Colour);
        }

        public static List<Recommendation> RankThree(
            IReadOnlyList<Artwork> catalogue,
            PreferenceProfile profile,
            string excludeId)
        {
            List<Recommendation> ranked = new List<Recommendation>();
            if (catalogue == null || profile == null)
                return ranked;

            for (int i = 0; i < catalogue.Count; i++)
            {
                Artwork artwork = catalogue[i];
                if (artwork == null)
                    continue;
                if (!string.IsNullOrEmpty(excludeId) && artwork.Id == excludeId)
                    continue;
                ranked.Add(new Recommendation(artwork, Score(artwork, profile), 0));
            }

            ranked.Sort(Compare);

            List<Recommendation> top = new List<Recommendation>();
            int count = ranked.Count < 3 ? ranked.Count : 3;
            for (int i = 0; i < count; i++)
                top.Add(new Recommendation(ranked[i].Artwork, ranked[i].Score, i + 1));
            return top;
        }

        public static Artwork Choose(IReadOnlyList<Recommendation> ranked, int oneBasedRank)
        {
            if (ranked == null || oneBasedRank < 1)
                return null;

            for (int i = 0; i < ranked.Count; i++)
            {
                Recommendation recommendation = ranked[i];
                if (recommendation != null && recommendation.Rank == oneBasedRank)
                    return recommendation.Artwork;
            }

            return null;
        }

        static int Compare(Recommendation left, Recommendation right)
        {
            if (left.Score != right.Score)
                return right.Score.CompareTo(left.Score);
            return string.CompareOrdinal(left.Artwork.Id, right.Artwork.Id);
        }
    }
}
