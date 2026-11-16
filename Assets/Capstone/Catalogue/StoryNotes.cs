namespace Capstone.Catalogue
{
    /// <summary>
    /// Artist background, inspiration, and meaning for one artwork.
    /// Looked up later by artwork id. Empty text is allowed until a note is written.
    /// </summary>
    public sealed class StoryNotes
    {
        public static readonly StoryNotes Empty = new StoryNotes(string.Empty, string.Empty, string.Empty);

        public StoryNotes(string artistBackground, string inspiration, string meaning)
        {
            ArtistBackground = artistBackground ?? string.Empty;
            Inspiration = inspiration ?? string.Empty;
            Meaning = meaning ?? string.Empty;
        }

        public string ArtistBackground { get; }

        public string Inspiration { get; }

        public string Meaning { get; }
    }
}
