using System.Collections.Generic;
using Capstone.Catalogue;

namespace Capstone.Ranking
{
    /// <summary>
    /// The four pictures supplied for a wall trial. The centimetres follow each picture
    /// so the preview does not stretch. A measured size can replace these numbers.
    /// </summary>
    public static class TrialPieces
    {
        public static IReadOnlyList<Artwork> All { get; } = new Artwork[]
        {
            Piece("trial-reclining", "Reclining figure", 50f, 80f, "TrialPieces/reclining", "A figure lying back, in green and white."),
            Piece("trial-folded", "Folded figure", 89f, 60f, "TrialPieces/folded", "A figure folded forward, drawn on a warm ground."),
            Piece("trial-green-dress", "Green dress", 59f, 90f, "TrialPieces/green-dress", "A figure stretched out on a green ground."),
            Piece("trial-hands", "Hands", 93f, 70f, "TrialPieces/hands", "A close study of two hands."),
        };

        static Artwork Piece(string id, string title, float heightCm, float widthCm, string image, string meaning)
        {
            return new Artwork(
                id,
                title,
                "Trial piece",
                2022,
                "Painting",
                heightCm,
                widthCm,
                "trial",
                "calm",
                "earth",
                "none",
                image,
                new StoryNotes(
                    "Supplied for this wall trial.",
                    "The centimetres follow the picture so the preview does not stretch.",
                    meaning));
        }
    }
}
