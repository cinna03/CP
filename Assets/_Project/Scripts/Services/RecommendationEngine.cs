using System;
using System.Collections.Generic;
using Capstone.App.Data;

namespace Capstone.App.Services
{
    /// <summary>
    /// Keep adds 2, Save adds 1, and Dismiss subtracts 1 on the current style, mood, and colour.
    /// A blank tag adds nothing. The current identifier is left out. Ties fall back to the identifier.
    /// </summary>
    [Serializable]
    public sealed class RecommendationEngine : IRecommendationEngine
    {
        public const int Count = 3;

        Dictionary<string, float> style;
        Dictionary<string, float> mood;
        Dictionary<string, float> colour;

        public void Apply(ArtworkData current, VisitDecision decision)
        {
            if (current == null)
                throw new ArgumentNullException(nameof(current));

            EnsureWeights();
            float change = Change(decision);
            Add(style, current.Style, change);
            Add(mood, current.Mood, change);
            Add(colour, current.Colour, change);
        }

        public IReadOnlyList<ArtworkData> Three(IReadOnlyList<ArtworkData> works, ArtworkData current)
        {
            if (works == null)
                throw new ArgumentNullException(nameof(works));
            if (current == null)
                throw new ArgumentNullException(nameof(current));

            var others = new List<ArtworkData>();
            for (int i = 0; i < works.Count; i++)
            {
                ArtworkData work = works[i];
                if (work != null && !string.Equals(work.Identifier, current.Identifier, StringComparison.Ordinal))
                    others.Add(work);
            }

            others.Sort(delegate (ArtworkData a, ArtworkData b)
            {
                int byScore = Score(b).CompareTo(Score(a));
                if (byScore != 0)
                    return byScore;
                return string.Compare(a.Identifier, b.Identifier, StringComparison.Ordinal);
            });

            if (others.Count > Count)
                others.RemoveRange(Count, others.Count - Count);
            return others;
        }

        public void Clear()
        {
            EnsureWeights();
            style.Clear();
            mood.Clear();
            colour.Clear();
        }

        void EnsureWeights()
        {
            if (style == null)
                style = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            if (mood == null)
                mood = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            if (colour == null)
                colour = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        }

        public float Score(ArtworkData work)
        {
            if (work == null)
                return 0f;

            EnsureWeights();
            return Value(style, work.Style) + Value(mood, work.Mood) + Value(colour, work.Colour);
        }

        static float Change(VisitDecision decision)
        {
            switch (decision)
            {
                case VisitDecision.Keep:
                    return 2f;
                case VisitDecision.Save:
                    return 1f;
                case VisitDecision.Dismiss:
                    return -1f;
                default:
                    throw new ArgumentOutOfRangeException(nameof(decision));
            }
        }

        static void Add(Dictionary<string, float> weights, string tag, float change)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return;

            weights.TryGetValue(tag, out float current);
            weights[tag] = current + change;
        }

        static float Value(Dictionary<string, float> weights, string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return 0f;

            weights.TryGetValue(tag, out float current);
            return current;
        }
    }
}
