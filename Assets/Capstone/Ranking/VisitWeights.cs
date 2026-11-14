using System;
using System.Collections.Generic;
using Capstone.Catalogue;

namespace Capstone.Ranking
{
    /// <summary>
    /// Style, mood, and colour weights for one visit. They clear when the visit ends.
    /// </summary>
    public sealed class VisitWeights
    {
        readonly Dictionary<string, float> style = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, float> mood = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, float> colour = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        public void Apply(Artwork work, Decision decision)
        {
            if (work == null)
                throw new ArgumentNullException(nameof(work));

            float change = decision == Decision.Keep ? 2f : decision == Decision.Save ? 1f : -1f;
            Add(style, work.Style, change);
            Add(mood, work.Mood, change);
            Add(colour, work.Colour, change);
        }

        public float Score(Artwork work)
        {
            if (work == null)
                return 0f;

            return Value(style, work.Style) + Value(mood, work.Mood) + Value(colour, work.Colour);
        }

        public void Clear()
        {
            style.Clear();
            mood.Clear();
            colour.Clear();
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
