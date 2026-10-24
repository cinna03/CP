using System;

namespace Capstone.Catalogue
{
    /// <summary>
    /// One catalogue work. Height and width are stored in centimetres.
    /// The wall preview reads <see cref="WallSizeMetres"/> and does not invent a size.
    /// </summary>
    public sealed class Artwork
    {
        public Artwork(
            string id,
            string title,
            string artist,
            int year,
            string medium,
            float heightCm,
            float widthCm,
            string style,
            string mood,
            string colour,
            string priceBand,
            string imageResource,
            StoryNotes storyNotes)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("An artwork needs an id.", nameof(id));
            if (heightCm <= 0f)
                throw new ArgumentOutOfRangeException(nameof(heightCm), heightCm, "Height must be a positive number of centimetres.");
            if (widthCm <= 0f)
                throw new ArgumentOutOfRangeException(nameof(widthCm), widthCm, "Width must be a positive number of centimetres.");

            Id = id;
            Title = title ?? string.Empty;
            Artist = artist ?? string.Empty;
            Year = year;
            Medium = medium ?? string.Empty;
            HeightCm = heightCm;
            WidthCm = widthCm;
            Style = style ?? string.Empty;
            Mood = mood ?? string.Empty;
            Colour = colour ?? string.Empty;
            PriceBand = priceBand ?? string.Empty;
            ImageResource = imageResource ?? string.Empty;
            StoryNotes = storyNotes ?? StoryNotes.Empty;
        }

        public string Id { get; }

        public string Title { get; }

        public string Artist { get; }

        public int Year { get; }

        public string Medium { get; }

        public float HeightCm { get; }

        public float WidthCm { get; }

        public string Style { get; }

        public string Mood { get; }

        public string Colour { get; }

        public string PriceBand { get; }

        public string ImageResource { get; }

        public StoryNotes StoryNotes { get; }

        public WallSize WallSizeMetres => ArtworkScale.ToWallSize(WidthCm, HeightCm);
    }
}
