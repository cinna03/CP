using System.Collections.Generic;
using Capstone.Catalogue;

namespace Capstone.Ranking
{
    /// <summary>
    /// The current preference signal. Style, mood, and colour each carry an integer weight.
    /// A missing tag scores zero. Weights may become negative after a dismiss.
    /// </summary>
    public sealed class PreferenceProfile
    {
        readonly Dictionary<string, int> style = new Dictionary<string, int>();
        readonly Dictionary<string, int> mood = new Dictionary<string, int>();
        readonly Dictionary<string, int> colour = new Dictionary<string, int>();

        public string LastAction { get; private set; }

        public string LastConcern { get; private set; }

        public static PreferenceProfile FromInitialTags(string styleTag, string moodTag, string colourTag)
        {
            PreferenceProfile profile = new PreferenceProfile();
            profile.Add(profile.style, styleTag, 1);
            profile.Add(profile.mood, moodTag, 1);
            profile.Add(profile.colour, colourTag, 1);
            return profile;
        }

        public int StyleWeight(string tag)
        {
            return Weight(style, tag);
        }

        public int MoodWeight(string tag)
        {
            return Weight(mood, tag);
        }

        public int ColourWeight(string tag)
        {
            return Weight(colour, tag);
        }

        public void UpdateFromFeedback(Artwork artwork, FeedbackAction action)
        {
            if (artwork == null)
                return;

            int delta = action == FeedbackAction.Keep || action == FeedbackAction.Save ? 1 : -1;
            Add(style, artwork.Style, delta);
            Add(mood, artwork.Mood, delta);
            Add(colour, artwork.Colour, delta);
            LastAction = action.ToString();
        }

        public void NoteConcern(string concern, Artwork artwork)
        {
            LastConcern = string.IsNullOrWhiteSpace(concern) ? "none" : concern.Trim().ToLowerInvariant();
            if (LastConcern == "colour" || LastConcern == "color")
                Add(colour, artwork != null ? artwork.Colour : string.Empty, 1);
        }

        static int Weight(Dictionary<string, int> weights, string tag)
        {
            string key = Normalize(tag);
            int weight;
            if (key.Length == 0 || !weights.TryGetValue(key, out weight))
                return 0;
            return weight;
        }

        void Add(Dictionary<string, int> weights, string tag, int delta)
        {
            string key = Normalize(tag);
            if (key.Length == 0 || delta == 0)
                return;

            int weight;
            weights.TryGetValue(key, out weight);
            weights[key] = weight + delta;
        }

        static string Normalize(string tag)
        {
            return string.IsNullOrWhiteSpace(tag) ? string.Empty : tag.Trim().ToLowerInvariant();
        }
    }
}
