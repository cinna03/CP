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
                + artwork.WidthCm.ToString("0") + " cm wide, "
                + artwork.HeightCm.ToString("0") + " cm tall."
                + "\nArtist background: " + Note(artwork.StoryNotes.ArtistBackground)
                + "\nInspiration: " + Note(artwork.StoryNotes.Inspiration)
                + "\nMeaning: " + Note(artwork.StoryNotes.Meaning);
        }

        static string Note(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? MissingNote : text;
        }
    }
}
