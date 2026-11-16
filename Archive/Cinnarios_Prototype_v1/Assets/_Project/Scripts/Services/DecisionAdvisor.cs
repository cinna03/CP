using System;
using System.Collections.Generic;
using Capstone.App.Data;

namespace Capstone.App.Services
{
    /// <summary>
    /// Answers open questions such as "which one would you recommend?" or "should I buy it?" with a
    /// clear pick and the reasons behind it, instead of handing the decision back to the buyer.
    /// The pick is ranked only from what the buyer did (kept, saved, dismissed) and the stored record
    /// (size against the room they named, mood, colour), so every reason can be traced.
    /// </summary>
    public static class DecisionAdvisor
    {
        static readonly string[] Asks =
        {
            "recommend", "which one", "which should", "which would", "what would you", "should i",
            "best one", "the best", "choose", "pick", "decide", "can't decide", "cant decide", "worth it",
            "happy with all", "like all", "love all", "all of them", "your opinion", "what do you think"
        };

        public static bool IsAdviceRequest(string asked)
        {
            if (string.IsNullOrWhiteSpace(asked))
                return false;
            foreach (string phrase in Asks)
            {
                if (asked.IndexOf(phrase, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        /// <summary>Points for one tried work: the buyer's own decision first, then fit with the room they named.</summary>
        public static int Score(TriedPiece piece, UserTasteProfile taste)
        {
            if (piece == null || piece.Work == null)
                return int.MinValue;
            int score = piece.Status == "Kept" ? 6 : piece.Status == "Saved" ? 4 : piece.Status == "Dismissed" ? -10 : 1;
            SizeTier tier = piece.Work.SizeTierAt(piece.SizeIndex);
            Orientation facing = piece.Work.OrientationAt(piece.SizeIndex);
            if (taste != null)
            {
                if ((taste.TargetRoom == UserTasteProfile.Bedroom || taste.TargetRoom == UserTasteProfile.Hallway) && tier != SizeTier.Large)
                    score += 2;
                if (taste.TargetRoom == UserTasteProfile.LivingRoom && tier != SizeTier.Small)
                    score += 2;
                if ((taste.FurnitureContext == UserTasteProfile.AboveSofa || taste.FurnitureContext == UserTasteProfile.AboveBed) && facing == Orientation.Landscape)
                    score += 2;
                if (taste.FurnitureContext == UserTasteProfile.FocalWall && tier == SizeTier.Large)
                    score += 2;
            }

            return score;
        }

        /// <summary>A decisive pick among the tried works, or a clear verdict on the one on the wall.</summary>
        public static string Advise(ArtworkData current, int currentSize, IReadOnlyList<TriedPiece> tried, UserTasteProfile taste)
        {
            var ranked = new List<TriedPiece>();
            var seen = new HashSet<ArtworkData>();
            if (tried != null)
            {
                foreach (TriedPiece piece in tried)
                {
                    if (piece != null && piece.Work != null && seen.Add(piece.Work))
                        ranked.Add(piece);
                }
            }

            ranked.Sort((a, b) => Score(b, taste).CompareTo(Score(a, taste)));
            if (ranked.Count < 2 || Score(ranked[0], taste) < 0)
                return Verdict(current, currentSize, taste);

            TriedPiece best = ranked[0];
            TriedPiece next = ranked[1];
            string reasons = Reasons(best, taste);
            string close = Score(next, taste) >= Score(best, taste) - 1 && next.Status != "Dismissed"
                ? " If you want a different feel, " + next.Work.Title + " is the close second: it is more " + Lower(next.Work.Mood, "different") + "."
                : "";
            return "My pick is " + best.Work.Title + ". " + reasons + close + " Put it back on the wall and keep it if it still feels right.";
        }

        static string Reasons(TriedPiece piece, UserTasteProfile taste)
        {
            var parts = new List<string>();
            if (piece.Status == "Kept")
                parts.Add("it is the one you kept");
            else if (piece.Status == "Saved")
                parts.Add("it is the one you saved");
            string room = taste != null && taste.TargetRoom.Length > 0 ? "your " + taste.TargetRoom.ToLowerInvariant() : "a single wall";
            parts.Add("at " + piece.Work.FormatSizeLabel(piece.SizeIndex) + " it is the right scale for " + room);
            if (piece.Work.Mood.Length > 0)
                parts.Add("its " + Lower(piece.Work.Mood, "") + " mood holds the space without overpowering it");
            return Capitalise(string.Join(", ", parts)) + ".";
        }

        /// <summary>With only one work tried: an honest read and one concrete next step.</summary>
        public static string Verdict(ArtworkData work, int sizeIndex, UserTasteProfile taste)
        {
            string mood = work.Mood.Length > 0 ? Lower(work.Mood, "") + " " : "";
            string colour = work.Colour.Length > 0 ? " in " + Lower(work.Colour, "") : "";
            return "My honest read: " + work.Title + " is a " + mood + "piece" + colour + ", " + work.FormatSizeLabel(sizeIndex)
                + ". If it still feels right after you step back and look at it on the wall, it is a good choice. "
                + "To be sure, keep it and try one suggestion next to it. I will then tell you which of the two works better here.";
        }

        static string Lower(string text, string fallback) => string.IsNullOrWhiteSpace(text) ? fallback : text.Trim().ToLowerInvariant();
        static string Capitalise(string text) => text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);
    }
}
