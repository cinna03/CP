using System;
using Cinnarios.Painting.Data;
using UnityEngine;

namespace Cinnarios.Catalogue.Data
{
    /// <summary>
    /// One artwork as the shop knows it: its details, real size, price and where its images
    /// live. It holds no image itself, so the whole catalogue can be listed before any image
    /// is downloaded. When an artwork is shown in 3D, ToSpec turns it into a PaintingSpec.
    /// </summary>
    public sealed class ArtworkRecord
    {
        public string SourceId { get; }
        public string Title { get; }
        public string Artist { get; }
        public string Date { get; }
        public string CreditLine { get; }

        /// <summary>The medium as the source wrote it, for example "Oil on canvas".</summary>
        public string MediumDescription { get; }

        public PaintingMedium Medium { get; }
        public PaintingDimensions Dimensions { get; }

        /// <summary>Full-resolution image, for the 3D painting.</summary>
        public string ImageUrl { get; }

        /// <summary>Smaller image, for lists and previews.</summary>
        public string ThumbnailUrl { get; }

        public decimal Price { get; }
        public string CurrencyCode { get; }

        public ArtworkRecord(
            string sourceId, string title, string artist, string date, string creditLine,
            string mediumDescription, PaintingMedium medium, PaintingDimensions dimensions,
            string imageUrl, string thumbnailUrl, decimal price, string currencyCode)
        {
            SourceId = Require(sourceId, nameof(sourceId));
            Title = Require(title, nameof(title));
            Artist = artist ?? string.Empty;
            Date = date ?? string.Empty;
            CreditLine = creditLine ?? string.Empty;
            MediumDescription = mediumDescription ?? string.Empty;
            Medium = medium;
            Dimensions = dimensions ?? throw new ArgumentNullException(nameof(dimensions));
            ImageUrl = Require(imageUrl, nameof(imageUrl));
            ThumbnailUrl = string.IsNullOrWhiteSpace(thumbnailUrl) ? imageUrl : thumbnailUrl;
            Price = price;
            CurrencyCode = currencyCode ?? string.Empty;
        }

        /// <summary>Combines this record with its downloaded image into a buildable painting.</summary>
        public PaintingSpec ToSpec(Texture2D image) =>
            new PaintingSpec(SourceId, Title, Artist, Date, CreditLine, image, Dimensions, Medium);

        public override string ToString() => $"{Title} by {Artist} ({Dimensions}, {Price:0} {CurrencyCode})";

        private static string Require(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("This value is required.", parameterName);
            }

            return value.Trim();
        }
    }
}
