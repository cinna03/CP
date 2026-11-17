using System;
using UnityEngine;

namespace Cinnarios.Painting.Data
{
    /// <summary>
    /// Everything the system needs to know to build one 3D painting. It holds data
    /// only and builds nothing. Values are fixed when it is created and can be read
    /// but not changed afterwards.
    /// </summary>
    public sealed class PaintingSpec
    {
        private const string UnknownArtist = "Unknown artist";

        /// <summary>The ID of the artwork at its source, for example the Met object ID.</summary>
        public string SourceId { get; }
        public string Title { get; }
        public string Artist { get; }
        public string Date { get; }

        /// <summary>The attribution the source requires to be shown with the image.</summary>
        public string CreditLine { get; }

        /// <summary>The front of the artwork as a flat 2D image.</summary>
        public Texture2D Image { get; }

        public PaintingDimensions Dimensions { get; }
        public PaintingMedium Medium { get; }
        public SideTreatment SideTreatment { get; }

        public PaintingSpec(
            string sourceId,
            string title,
            string artist,
            string date,
            string creditLine,
            Texture2D image,
            PaintingDimensions dimensions,
            PaintingMedium medium,
            SideTreatment sideTreatment = SideTreatment.PlainEdge)
        {
            SourceId = RequireText(sourceId, nameof(sourceId));
            Title = RequireText(title, nameof(title));
            Artist = string.IsNullOrWhiteSpace(artist) ? UnknownArtist : artist.Trim();
            Date = date?.Trim() ?? string.Empty;
            CreditLine = creditLine?.Trim() ?? string.Empty;
            Image = image != null ? image : throw new ArgumentNullException(nameof(image));
            Dimensions = dimensions ?? throw new ArgumentNullException(nameof(dimensions));
            Medium = medium;
            SideTreatment = sideTreatment;
        }

        public override string ToString() => $"{Title} by {Artist} ({Dimensions})";

        private static string RequireText(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("This value is required.", parameterName);
            }

            return value.Trim();
        }
    }
}
