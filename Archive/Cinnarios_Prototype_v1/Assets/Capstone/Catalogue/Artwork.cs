using System;

namespace Capstone.Catalogue
{
    /// <summary>
    /// One catalogue work. Height and width are stored in centimetres.
    /// An empty story note stays empty.
    /// </summary>
    public sealed class Artwork
    {
        public string Id { get; }
        public string Title { get; }
        public string Artist { get; }
        public int Year { get; }
        public string Medium { get; }
        public float HeightCentimetres { get; }
        public float WidthCentimetres { get; }
        public string Style { get; }
        public string Mood { get; }
        public string Colour { get; }
        public string PriceBand { get; }
        public string ImageName { get; }
        public string StoryNote { get; }

        public float HeightMetres => ArtworkScale.ToMetres(HeightCentimetres);
        public float WidthMetres => ArtworkScale.ToMetres(WidthCentimetres);

        public Artwork(
            string id,
            string title,
            string artist,
            int year,
            string medium,
            float heightCentimetres,
            float widthCentimetres,
            string style,
            string mood,
            string colour,
            string priceBand,
            string imageName,
            string storyNote)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A work needs an id.", nameof(id));
            if (heightCentimetres <= 0f)
                throw new ArgumentOutOfRangeException(nameof(heightCentimetres), "Height must be a positive number of centimetres.");
            if (widthCentimetres <= 0f)
                throw new ArgumentOutOfRangeException(nameof(widthCentimetres), "Width must be a positive number of centimetres.");

            Id = id.Trim();
            Title = title ?? "";
            Artist = artist ?? "";
            Year = year;
            Medium = medium ?? "";
            HeightCentimetres = heightCentimetres;
            WidthCentimetres = widthCentimetres;
            Style = style ?? "";
            Mood = mood ?? "";
            Colour = colour ?? "";
            PriceBand = priceBand ?? "";
            ImageName = imageName ?? "";
            StoryNote = storyNote ?? "";
        }
    }
}
