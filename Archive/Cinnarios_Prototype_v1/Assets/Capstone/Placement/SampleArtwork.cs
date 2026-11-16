using System;
using Capstone.Catalogue;
using Capstone.Ranking;

namespace Capstone.Placement
{
    /// <summary>
    /// The two works the placement screen can swap. Both are the same records as the sample catalogue,
    /// so the size and the story note do not diverge.
    /// </summary>
    public static class SampleArtwork
    {
        public const string FirstId = "sample-wall";
        public const string SecondId = "sample-wide";
        public const float WidthCm = 40f;
        public const float HeightCm = 50f;
        public const float WideWidthCm = 80f;
        public const float WideHeightCm = 30f;

        public static Artwork Create()
        {
            return Require(FirstId);
        }

        public static Artwork CreateWide()
        {
            return Require(SecondId);
        }

        static Artwork Require(string id)
        {
            Artwork artwork = SampleCatalogue.Find(id);
            if (artwork == null)
                throw new InvalidOperationException("The sample catalogue has no work with id " + id + ".");
            return artwork;
        }

        public static Artwork OtherThan(Artwork current)
        {
            if (current != null && current.Id == FirstId)
                return CreateWide();

            return Create();
        }
    }
}
