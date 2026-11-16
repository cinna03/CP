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

        /// <summary>
        /// Keep and Save rank the three works most like the one on the wall.
        /// Dismiss ranks the three that differ in style, mood, and colour.
        /// A dismissed identifier is left out for the rest of the visit.
        /// </summary>
        public IReadOnlyList<ArtworkData> Three(IReadOnlyList<ArtworkData> works, ArtworkData current, VisitDecision decision, IReadOnlyList<string> dismissed, UserTasteProfile taste)
        {
            if (works == null)
                throw new ArgumentNullException(nameof(works));
            if (current == null)
                throw new ArgumentNullException(nameof(current));

            bool contrast = decision == VisitDecision.Dismiss;
            var others = new List<ArtworkData>();
            for (int i = 0; i < works.Count; i++)
            {
                ArtworkData work = works[i];
                if (work == null || string.Equals(work.Identifier, current.Identifier, StringComparison.Ordinal))
                    continue;
                if (Dismissed(dismissed, work.Identifier))
                    continue;
                others.Add(work);
            }

            others.Sort(delegate (ArtworkData a, ArtworkData b)
            {
                int byScore = (Rank(current, b, contrast) + Fit(b, taste)).CompareTo(Rank(current, a, contrast) + Fit(a, taste));
                if (byScore != 0)
                    return byScore;
                return string.Compare(a.Identifier, b.Identifier, StringComparison.Ordinal);
            });

            if (others.Count > Count)
                others.RemoveRange(Count, others.Count - Count);
            return others;
        }

        public string Heading(VisitDecision decision)
        {
            return decision == VisitDecision.Dismiss ? "Try a Different Direction" : "Similar Works You Might Like";
        }

        static int Rank(ArtworkData current, ArtworkData other, bool contrast)
        {
            int score = 0;
            score += Tag(current.Style, other.Style, contrast);
            score += Tag(current.Mood, other.Mood, contrast);
            score += Tag(current.Colour, other.Colour, contrast);
            return score;
        }

        static int Tag(string current, string other, bool contrast)
        {
            if (string.IsNullOrWhiteSpace(current) || string.IsNullOrWhiteSpace(other))
                return 0;

            bool same = string.Equals(current.Trim(), other.Trim(), StringComparison.OrdinalIgnoreCase);
            if (contrast)
                return same ? 0 : 3;
            return same ? 3 : 0;
        }

        static bool Dismissed(IReadOnlyList<string> dismissed, string identifier)
        {
            if (dismissed == null || string.IsNullOrEmpty(identifier))
                return false;

            for (int i = 0; i < dismissed.Count; i++)
            {
                if (string.Equals(dismissed[i], identifier, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        static int Fit(ArtworkData work, UserTasteProfile taste)
        {
            if (work == null || taste == null)
                return 0;

            int score = 0;
            score += (int)taste.Weight(taste.LikedStyles, work.Style);
            score += (int)taste.Weight(taste.LikedMoods, work.Mood);
            score += (int)taste.Weight(taste.LikedColours, work.Colour);

            SizeTier tier = work.SizeTier;
            Orientation facing = work.Orientation;
            if (taste.TargetRoom == UserTasteProfile.Bedroom)
            {
                if (Quiet(work.Mood) || Quiet(work.Style))
                    score += 2;
                if (tier == SizeTier.Large)
                    score -= 1;
                if (tier == SizeTier.Small || tier == SizeTier.Medium)
                    score += 1;
            }
            else if (taste.TargetRoom == UserTasteProfile.Hallway && tier == SizeTier.Small && facing == Orientation.Portrait)
                score += 3;
            else if (taste.TargetRoom == UserTasteProfile.Workspace && tier == SizeTier.Small)
                score += 2;
            else if (taste.TargetRoom == UserTasteProfile.LivingRoom && (tier == SizeTier.Medium || tier == SizeTier.Large))
                score += 1;

            if (taste.FurnitureContext == UserTasteProfile.AboveSofa && facing == Orientation.Landscape && tier != SizeTier.Small)
                score += 3;
            else if (taste.FurnitureContext == UserTasteProfile.AboveBed && facing == Orientation.Landscape)
                score += 2;
            else if (taste.FurnitureContext == UserTasteProfile.FocalWall && tier == SizeTier.Large)
                score += 2;
            else if (taste.FurnitureContext == UserTasteProfile.DeskShelf && tier == SizeTier.Small)
                score += 3;

            return score;
        }

        static bool Quiet(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return false;
            string value = tag.ToLowerInvariant();
            return value.Contains("calm") || value.Contains("quiet") || value.Contains("still") || value.Contains("soft");
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
