using System.Collections.Generic;

namespace Capstone.App.Data
{
    /// <summary>
    /// What the buyer ended the visit with: the work the visit's own evidence points to (kept or saved
    /// first, never a dismissed one) and how many works were tried. Captured when Finish visit is tapped,
    /// before the visit is cleared, so the closing screen can show "Your pick".
    /// </summary>
    public sealed class VisitOutcome
    {
        public ArtworkData Work;
        public int SizeIndex;
        public string Status = "";
        public int Tried;

        /// <summary>The outcome of the most recent visit, or null.</summary>
        public static VisitOutcome Last { get; set; }

        public static VisitOutcome From(IReadOnlyList<TriedPiece> tried, System.Func<TriedPiece, int> score)
        {
            var outcome = new VisitOutcome();
            if (tried == null)
                return outcome;
            int best = int.MinValue;
            var seen = new HashSet<ArtworkData>();
            foreach (TriedPiece piece in tried)
            {
                if (piece == null || piece.Work == null || !seen.Add(piece.Work))
                    continue;
                outcome.Tried++;
                if (piece.Status == "Dismissed")
                    continue;
                int points = score(piece);
                if (points > best)
                {
                    best = points;
                    outcome.Work = piece.Work;
                    outcome.SizeIndex = piece.SizeIndex;
                    outcome.Status = piece.Status;
                }
            }

            return outcome;
        }
    }
}
