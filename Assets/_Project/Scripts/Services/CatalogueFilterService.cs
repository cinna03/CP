using System;
using System.Collections.Generic;
using Capstone.App.Data;

namespace Capstone.App.Services
{
    /// <summary>
    /// Matches style, mood, and colour as whole words, and orientation and size tier from the default size.
    /// An open query returns every record that was passed in.
    /// </summary>
    [Serializable]
    public sealed class CatalogueFilterService : ICatalogueFilterService
    {
        public IReadOnlyList<ArtworkData> Matching(IReadOnlyList<ArtworkData> works, CatalogueQuery query)
        {
            if (works == null)
                throw new ArgumentNullException(nameof(works));

            var found = new List<ArtworkData>();
            for (int i = 0; i < works.Count; i++)
            {
                ArtworkData work = works[i];
                if (work == null)
                    continue;
                if (!Fits(work.Style, query.Style) || !Fits(work.Mood, query.Mood) || !Fits(work.Colour, query.Colour))
                    continue;
                if (query.Orientation.HasValue && work.Orientation != query.Orientation.Value)
                    continue;
                if (query.SizeTier.HasValue && work.SizeTier != query.SizeTier.Value)
                    continue;

                found.Add(work);
            }

            string[] tokens = Tokens(query.Search);
            if (tokens.Length == 0)
                return found;

            var ranked = new List<ScoredWork>();
            for (int i = 0; i < found.Count; i++)
            {
                int score = Score(found[i], tokens);
                if (score > 0)
                    ranked.Add(new ScoredWork { Work = found[i], Score = score });
            }

            ranked.Sort(delegate (ScoredWork a, ScoredWork b)
            {
                int byScore = b.Score.CompareTo(a.Score);
                if (byScore != 0)
                    return byScore;
                return string.Compare(a.Work.Identifier, b.Work.Identifier, StringComparison.Ordinal);
            });

            var ordered = new List<ArtworkData>(ranked.Count);
            for (int i = 0; i < ranked.Count; i++)
                ordered.Add(ranked[i].Work);
            return ordered;
        }

        static string[] Tokens(string search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return new string[0];

            string[] parts = search.ToLowerInvariant().Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            return parts;
        }

        static int Score(ArtworkData work, string[] tokens)
        {
            int total = 0;
            for (int i = 0; i < tokens.Length; i++)
                total += ScoreToken(work, tokens[i]);
            return total;
        }

        static int ScoreToken(ArtworkData work, string token)
        {
            if (Same(work.Style, token) || Same(work.Mood, token) || Same(work.Colour, token) || Same(work.Medium, token) || Same(work.SizeTier.ToString(), token))
                return 5;
            if (Same(work.Title, token) || Same(work.Artist, token))
                return 4;
            if (HasWord(work.Title, token) || HasWord(work.Artist, token) || HasWord(work.StoryNote, token) || HasWord(work.Style, token) || HasWord(work.Mood, token) || HasWord(work.Colour, token) || HasWord(work.Medium, token))
                return 3;
            if (Holds(work.Title, token) || Holds(work.Artist, token) || Holds(work.StoryNote, token) || Holds(work.Style, token) || Holds(work.Mood, token) || Holds(work.Colour, token) || Holds(work.Medium, token) || Holds(work.SizeTier.ToString(), token))
                return 1;
            return 0;
        }

        static bool Same(string field, string token)
        {
            return string.Equals(field == null ? "" : field.Trim(), token, StringComparison.OrdinalIgnoreCase);
        }

        static bool Holds(string field, string token)
        {
            if (string.IsNullOrEmpty(field))
                return false;
            return field.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static bool HasWord(string field, string token)
        {
            if (string.IsNullOrEmpty(field))
                return false;

            int start = 0;
            while (start < field.Length)
            {
                int found = field.IndexOf(token, start, StringComparison.OrdinalIgnoreCase);
                if (found < 0)
                    return false;
                bool left = found == 0 || !char.IsLetterOrDigit(field[found - 1]);
                int end = found + token.Length;
                bool right = end >= field.Length || !char.IsLetterOrDigit(field[end]);
                if (left && right)
                    return true;
                start = found + token.Length;
            }

            return false;
        }

        struct ScoredWork
        {
            public ArtworkData Work;
            public int Score;
        }

        static bool Fits(string stored, string wanted)
        {
            if (string.IsNullOrWhiteSpace(wanted))
                return true;

            return string.Equals(stored, wanted.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
