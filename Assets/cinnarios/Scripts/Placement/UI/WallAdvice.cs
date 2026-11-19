using System;
using System.Collections.Generic;
using System.Globalization;
using Cinnarios.Catalogue.Shop;

namespace Cinnarios.Placement.UI
{
    /// <summary>
    /// Picks the next paintings after a person says what they are unsure about.
    /// A liked aspect is kept. A disliked aspect moves the other way.
    /// Colour, mood, and subject are the same shop guides the website uses.
    /// </summary>
    public static class WallAdvice
    {
        public enum Aspect
        {
            Size,
            Colour,
            Fit,
            Piece,
            Price,
            Light
        }

        public enum Lean
        {
            Keep,
            Smaller,
            Larger,
            Different,
            Cheaper,
            Dearer,
            Lighter,
            Deeper
        }

        private static readonly Dictionary<string, string> Colour = new Dictionary<string, string>
        {
            { "436535", "gold" }, { "436532", "blue" }, { "436528", "green" }, { "436524", "earth" },
            { "437881", "red" }, { "435809", "dark" }, { "437394", "pale" }, { "436575", "gold" },
            { "12127", "earth" }, { "11417", "blue" }, { "10065", "red" }
        };

        private static readonly Dictionary<string, string> Subject = new Dictionary<string, string>
        {
            { "437394", "figure" }, { "436528", "flowers" }, { "12127", "figure" }, { "10065", "figure" },
            { "436532", "figure" }, { "436524", "flowers" }, { "435809", "landscape" }, { "436575", "landscape" },
            { "11417", "figure" }, { "436535", "landscape" }, { "437881", "figure" }
        };

        public sealed class Suggestion
        {
            public ShopArtworkData Artwork;
            public bool Direct;
            public string Because;
            public float Rank;
        }

        public static List<Suggestion> Suggest(IReadOnlyList<ShopArtworkData> catalogue, ShopArtworkData current, Aspect aspect, Lean lean)
        {
            var ranked = new List<Suggestion>();
            if (catalogue == null || current == null)
            {
                return ranked;
            }

            var scored = new List<Suggestion>();
            foreach (ShopArtworkData candidate in catalogue)
            {
                if (candidate == null || candidate.sourceId == current.sourceId)
                {
                    continue;
                }

                float match = Score(current, candidate, aspect, lean);
                float closeness = 1000f / (1f + Math.Abs(Area(candidate) - Area(current)));
                bool direct = match > 0f;
                scored.Add(new Suggestion
                {
                    Artwork = candidate,
                    Direct = direct,
                    Because = direct ? DirectReason(aspect, lean) : "Nearest in size. Nothing else matched that exactly.",
                    // Direct matches stay ahead. The rest are ordered by how close the size is.
                    Rank = direct ? 10000f + match : closeness
                });
            }

            scored.Sort((left, right) => right.Rank.CompareTo(left.Rank));
            int count = Math.Min(3, scored.Count);
            for (int index = 0; index < count; index++)
            {
                ranked.Add(scored[index]);
            }

            return ranked;
        }

        private static string DirectReason(Aspect aspect, Lean lean)
        {
            switch (aspect)
            {
                case Aspect.Size:
                case Aspect.Fit:
                    if (lean == Lean.Smaller) return "Smaller than the one on the wall.";
                    if (lean == Lean.Larger) return "Larger than the one on the wall.";
                    return "Close to this size.";
                case Aspect.Colour:
                    return lean == Lean.Different ? "A different colour guide." : "The same colour guide.";
                case Aspect.Piece:
                    return lean == Lean.Different ? "A different artist and subject." : "Same artist or the same kind of subject.";
                case Aspect.Price:
                    if (lean == Lean.Cheaper) return "A lower recorded price.";
                    if (lean == Lean.Dearer) return "A higher recorded price.";
                    return "Close to this price.";
                case Aspect.Light:
                    if (lean == Lean.Lighter) return "A lighter colour guide.";
                    if (lean == Lean.Deeper) return "A deeper colour guide.";
                    return "Close to this size, so the light can be compared.";
                default:
                    return "Closest available.";
            }
        }

        private static float Score(ShopArtworkData current, ShopArtworkData candidate, Aspect aspect, Lean lean)
        {
            float areaGap = Area(candidate) - Area(current);
            bool sameColour = ColourOf(candidate) == ColourOf(current);
            bool sameSubject = SubjectOf(candidate) == SubjectOf(current);
            bool sameArtist = !string.IsNullOrEmpty(current.artist) && current.artist == candidate.artist;

            switch (aspect)
            {
                case Aspect.Size:
                case Aspect.Fit:
                    if (lean == Lean.Keep)
                    {
                        return 1000f / (1f + Math.Abs(areaGap));
                    }

                    if (lean == Lean.Smaller && areaGap < 0f)
                    {
                        return 1000f / (1f + Math.Abs(areaGap));
                    }

                    if (lean == Lean.Larger && areaGap > 0f)
                    {
                        return 1000f / (1f + areaGap);
                    }

                    return 0f;

                case Aspect.Colour:
                    if (lean == Lean.Keep && sameColour)
                    {
                        return 10f + (1000f / (1f + Math.Abs(areaGap)));
                    }

                    if (lean == Lean.Different && !sameColour)
                    {
                        return 8f + (1000f / (1f + Math.Abs(areaGap)));
                    }

                    return 0f;

                case Aspect.Piece:
                    if (lean == Lean.Keep)
                    {
                        float likeness = (sameArtist ? 6f : 0f) + (sameSubject ? 4f : 0f);
                        return likeness > 0f ? likeness + (1000f / (1f + Math.Abs(areaGap))) : 0f;
                    }

                    if (lean == Lean.Different && !sameArtist && !sameSubject)
                    {
                        return 8f;
                    }

                    return 0f;

                case Aspect.Price:
                    float priceGap = Price(candidate) - Price(current);
                    if (lean == Lean.Keep)
                    {
                        return 1000f / (1f + Math.Abs(priceGap));
                    }

                    if (lean == Lean.Cheaper && priceGap < 0f)
                    {
                        return 1000f / (1f + Math.Abs(priceGap));
                    }

                    if (lean == Lean.Dearer && priceGap > 0f)
                    {
                        return 1000f / (1f + priceGap);
                    }

                    return 0f;

                case Aspect.Light:
                    bool light = IsLight(ColourOf(candidate));
                    if (lean == Lean.Lighter && light)
                    {
                        return 8f + (1000f / (1f + Math.Abs(areaGap)));
                    }

                    if (lean == Lean.Deeper && !light)
                    {
                        return 8f + (1000f / (1f + Math.Abs(areaGap)));
                    }

                    if (lean == Lean.Keep)
                    {
                        return 1000f / (1f + Math.Abs(areaGap));
                    }

                    return 0f;

                default:
                    return 0f;
            }
        }

        private static bool IsLight(string colour)
        {
            return colour == "pale" || colour == "gold" || colour == "blue";
        }

        private static string ColourOf(ShopArtworkData artwork)
        {
            if (!string.IsNullOrEmpty(artwork.colourGuide)) return artwork.colourGuide;
            return Guide(Colour, artwork.sourceId);
        }

        private static string SubjectOf(ShopArtworkData artwork)
        {
            if (!string.IsNullOrEmpty(artwork.subjectGuide)) return artwork.subjectGuide;
            return Guide(Subject, artwork.sourceId);
        }

        private static string Guide(Dictionary<string, string> table, string sourceId) =>
            table.TryGetValue(sourceId ?? string.Empty, out string value) ? value : "earth";

        private static float Area(ShopArtworkData artwork) => artwork.widthCm * artwork.heightCm;

        private static float Price(ShopArtworkData artwork) =>
            float.TryParse(artwork.priceAmount, NumberStyles.Number, CultureInfo.InvariantCulture, out float cents) ? cents : 0f;
    }
}
