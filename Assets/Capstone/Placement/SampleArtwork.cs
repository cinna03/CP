using Capstone.Catalogue;

namespace Capstone.Placement
{
    /// <summary>
    /// The one work used to test wall placement. Width and height are the tape-measure targets.
    /// </summary>
    public static class SampleArtwork
    {
        public const float WidthCm = 40f;
        public const float HeightCm = 50f;

        public static Artwork Create()
        {
            return new Artwork(
                "sample-wall",
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
    }
}
