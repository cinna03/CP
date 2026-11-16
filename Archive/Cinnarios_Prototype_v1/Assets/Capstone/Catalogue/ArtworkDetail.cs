namespace Capstone.Catalogue
{
    /// <summary>
    /// Reads the stored catalogue fields and the three story notes.
    /// An empty note stays empty. This text does not invent a story.
    /// </summary>
    public static class ArtworkDetail
    {
        public const string MissingNote = "Not stored.";

        public static string Describe(Artwork artwork)
        {
            if (artwork == null)
                return "No work is selected.";

            return artwork.Title
                + " by " + artwork.Artist
                + ", " + artwork.Year
                + ". "
                + artwork.WidthCentimetres.ToString("0") + " cm wide, "
                + artwork.HeightCentimetres.ToString("0") + " cm tall."
                + "\nStory note: " + Note(artwork.StoryNote);
        }

        static string Note(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? MissingNote : text;
        }
    }
}
