using System.Globalization;
using Cinnarios.Catalogue.Data;

namespace Cinnarios.Placement.UI
{
    /// <summary>
    /// The wall conversation. It reads what the person said, answers from the
    /// painting's own record, and chooses the next three works. It does not
    /// invent a story the museum record does not contain.
    /// </summary>
    public static class DecisionTalk
    {
        public sealed class Turn
        {
            public string Reply;
            public WallAdvice.Aspect Aspect;
            public WallAdvice.Lean Lean;
            public bool WantsSuggestions;
            public bool WantsNewSpot;
            public bool Satisfied;
            public bool Leave;
        }

        public static string Opening(ArtworkRecord artwork)
        {
            long francs = Francs(artwork.Price);
            return $"{artwork.Title} by {artwork.Artist} is {artwork.Dimensions.WidthCm:0.#} × {artwork.Dimensions.HeightCm:0.#} cm, at RWF {francs.ToString("N0", CultureInfo.InvariantCulture)}. Does this one feel right, or tell me what is in the way.";
        }

        public static Turn Hear(string said, ArtworkRecord artwork)
        {
            string text = (said ?? string.Empty).Trim().ToLowerInvariant();
            var turn = new Turn();
            if (text.Length == 0)
            {
                turn.Reply = Clarify();
                return turn;
            }

            Turn fact = AnswerFromTheRecord(text, artwork);
            if (fact != null)
            {
                return fact;
            }

            if (Contains(text, "too big", "too large", "smaller", "huge", "overwhelming", "too wide", "too tall"))
            {
                return Suggest(artwork, WallAdvice.Aspect.Size, WallAdvice.Lean.Smaller,
                    artwork == null ? "These are smaller." : $"{artwork.Title} is {artwork.Dimensions.WidthCm:0.#} × {artwork.Dimensions.HeightCm:0.#} cm. These take less wall.");
            }

            if (Contains(text, "too small", "larger", "tiny", "more presence", "too little"))
            {
                return Suggest(artwork, WallAdvice.Aspect.Size, WallAdvice.Lean.Larger,
                    artwork == null ? "These are larger." : $"{artwork.Title} is {artwork.Dimensions.WidthCm:0.#} × {artwork.Dimensions.HeightCm:0.#} cm. These take more wall.");
            }

            if (Contains(text, "price", "expensive", "cheaper", "afford", "budget", "cost", "dear"))
            {
                bool dearer = Contains(text, "more expensive", "higher", "dearer", "worth more");
                long francs = artwork == null ? 0 : Francs(artwork.Price);
                string money = artwork == null ? "the recorded price" : $"RWF {francs.ToString("N0", CultureInfo.InvariantCulture)}";
                return Suggest(artwork, WallAdvice.Aspect.Price, dearer ? WallAdvice.Lean.Dearer : WallAdvice.Lean.Cheaper,
                    dearer
                        ? $"This one is {money}. These are recorded at a higher price."
                        : $"This one is {money}. These are recorded at a lower price.");
            }

            if (Contains(text, "light", "lighting", "lamp", "dark room", "bright room", "too dark", "too bright"))
            {
                bool deeper = Contains(text, "dark", "deeper", "dim");
                return Suggest(artwork, WallAdvice.Aspect.Light, deeper ? WallAdvice.Lean.Deeper : WallAdvice.Lean.Lighter,
                    "The museum record does not describe the lamp light. These use a lighter or deeper colour guide, and that guide is only a shop stand-in.");
            }

            if (Contains(text, "colour", "color", "clash", "bright", "pale", "dark"))
            {
                bool keep = Contains(text, "same", "like this", "more like", "keep");
                return Suggest(artwork, WallAdvice.Aspect.Colour, keep ? WallAdvice.Lean.Keep : WallAdvice.Lean.Different,
                    keep
                        ? "These share this colour guide. The guide sorts the shop. It is not a measurement of the paint."
                        : "These have a different colour guide. The guide sorts the shop. It is not a measurement of the paint.");
            }

            if (Contains(text, "move it", "another spot", "different spot", "place it again", "place again", "readjust", "reposition", "new spot"))
            {
                turn.WantsNewSpot = true;
                turn.Reply = "The locked spot stays until you place this same painting again. Start from the wall, and lock the new spot when it is right.";
                return turn;
            }

            if (Contains(text, "different", "another", "something else", "other artist", "boring", "not the style"))
            {
                return Suggest(artwork, WallAdvice.Aspect.Piece, WallAdvice.Lean.Different,
                    artwork == null ? "These are a different artist or subject." : $"These are not {artwork.Artist}, or they are a different kind of subject.");
            }

            if (Contains(text, "like this", "same artist", "similar", "more like"))
            {
                return Suggest(artwork, WallAdvice.Aspect.Piece, WallAdvice.Lean.Keep,
                    artwork == null ? "These stay close to this one." : $"These stay with {artwork.Artist}, or with the same kind of subject.");
            }

            if (Contains(text, "fit", "space"))
            {
                return Suggest(artwork, WallAdvice.Aspect.Fit, WallAdvice.Lean.Keep,
                    artwork == null ? "These are close to this size, so you can compare the fit." : $"{artwork.Title} is {artwork.Dimensions.WidthCm:0.#} cm wide. These are close to that, so you can compare the fit on this wall.");
            }

            if (Contains(text, "yes", "this one is right", "i like it", "love it", "perfect", "keep it", "satisfied"))
            {
                turn.Satisfied = true;
                turn.Reply = "Then this one can stay. You can save the view, or leave.";
                return turn;
            }

            if (Contains(text, "don't want", "do not want", "not this", "hate", "i don't want it"))
            {
                turn.Leave = true;
                turn.Reply = "You can look for a better fit, or leave because you are sure.";
                return turn;
            }

            if (Contains(text, "not sure", "unsure", "maybe", "don't know", "do not know"))
            {
                turn.Reply = Clarify();
                return turn;
            }

            turn.Reply = Clarify();
            return turn;
        }

        private static Turn AnswerFromTheRecord(string text, ArtworkRecord artwork)
        {
            if (artwork == null)
            {
                return null;
            }

            if (Contains(text, "who painted", "who made", "what artist", "who is the artist", "who's the artist"))
            {
                return Say($"{artwork.Title} is by {Name(artwork)}. The date in the record is {When(artwork)}.");
            }

            if (Contains(text, "when was", "what year", "what date", "how old", "when did"))
            {
                return Say($"{artwork.Title} is dated {When(artwork)}, by {Name(artwork)}.");
            }

            if (Contains(text, "what is it made", "medium", "oil paint", "on canvas", "what material"))
            {
                string medium = string.IsNullOrWhiteSpace(artwork.MediumDescription) ? "not stated" : artwork.MediumDescription;
                return Say($"The museum record gives the medium as {medium}.");
            }

            if (Contains(text, "how much", "what price", "what's the price", "how much is"))
            {
                return Say($"The recorded price is RWF {Francs(artwork.Price).ToString("N0", CultureInfo.InvariantCulture)}. Do you want a lower price, a higher price, or is the price fine?");
            }

            if (Contains(text, "how big", "how wide", "how tall", "what size", "dimensions", "how large is"))
            {
                return Say($"{artwork.Title} is {artwork.Dimensions.WidthCm:0.#} cm wide and {artwork.Dimensions.HeightCm:0.#} cm tall. Is it too big for this wall, too small, or about right?");
            }

            return null;
        }

        private static string Clarify() =>
            "I don't have an answer for that. I can help with the size, the price, the colour, or a different picture. Which of those is in the way?";

        private static Turn Say(string reply) => new Turn { Reply = reply };

        private static string Name(ArtworkRecord artwork) =>
            string.IsNullOrWhiteSpace(artwork.Artist) ? "an artist the record does not name" : artwork.Artist;

        private static string When(ArtworkRecord artwork) =>
            string.IsNullOrWhiteSpace(artwork.Date) ? "a date the record does not give" : artwork.Date;

        private static Turn Suggest(ArtworkRecord artwork, WallAdvice.Aspect aspect, WallAdvice.Lean lean, string reply)
        {
            return new Turn
            {
                Reply = reply,
                Aspect = aspect,
                Lean = lean,
                WantsSuggestions = true
            };
        }

        private static bool Contains(string text, params string[] phrases)
        {
            foreach (string phrase in phrases)
            {
                if (text.Contains(phrase))
                {
                    return true;
                }
            }

            return false;
        }

        private static long Francs(decimal dollars) =>
            (long)decimal.Round(dollars * 1477.385m, 0, System.MidpointRounding.AwayFromZero);
    }
}
