using Capstone.Catalogue;

namespace Capstone.Placement
{
    /// <summary>
    /// Two sample works used to test placement and swapping. Each has its own id and centimetre size.
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
            return new Artwork(
                FirstId,
                "Sample wall work",
                "Sample artist",
                2024,
                "Test print",
                HeightCm,
                WidthCm,
                "sample",
                "calm",
                "blue",
                "none",
                "generated-sample",
                StoryNotes.Empty);
        }

        public static Artwork CreateWide()
        {
            return new Artwork(
                SecondId,
                "Wide sample",
                "Sample artist",
                2024,
                "Test print",
                WideHeightCm,
                WideWidthCm,
                "sample",
                "calm",
                "orange",
                "none",
                "generated-wide",
                StoryNotes.Empty);
        }

        public static Artwork OtherThan(Artwork current)
        {
            if (current != null && current.Id == FirstId)
                return CreateWide();

            return Create();
        }
    }
}
