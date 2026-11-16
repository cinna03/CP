using Capstone.Catalogue;

namespace Capstone.Ranking
{
    /// <summary>
    /// One ranked work. Rank 1 is the closest tag overlap. Score is the sum of the matching weights.
    /// </summary>
    public sealed class Recommendation
    {
        public Recommendation(Artwork artwork, int score, int rank)
        {
            Artwork = artwork;
            Score = score;
            Rank = rank;
        }

        public Artwork Artwork { get; }

        public int Score { get; }

        public int Rank { get; }
    }
}
