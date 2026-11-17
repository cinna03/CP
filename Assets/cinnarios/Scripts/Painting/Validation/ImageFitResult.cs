using Cinnarios.Painting.Data;
using UnityEngine;

namespace Cinnarios.Painting.Validation
{
    /// <summary>
    /// The outcome of checking a photo against a painting's real size. Read-only once created.
    /// </summary>
    public sealed class ImageFitResult
    {
        /// <summary>The whole image, used when no crop is needed.</summary>
        public static readonly Rect FullImage = new Rect(0f, 0f, 1f, 1f);

        public ImageFitStatus Status { get; }

        /// <summary>
        /// The part of the image to show, in 0..1 image coordinates. The image itself is never
        /// edited: the material is told to display only this rectangle.
        /// </summary>
        public Rect CropRect { get; }

        /// <summary>The dimensions to build with. Differs from the input only when they were swapped.</summary>
        public PaintingDimensions Dimensions { get; }

        /// <summary>How far apart the two proportions were, as a fraction (0.05 = 5%).</summary>
        public float RatioDifference { get; }

        public bool IsUsable => Status != ImageFitStatus.Mismatch;

        public ImageFitResult(ImageFitStatus status, Rect cropRect, PaintingDimensions dimensions, float ratioDifference)
        {
            Status = status;
            CropRect = cropRect;
            Dimensions = dimensions;
            RatioDifference = ratioDifference;
        }

        public override string ToString() =>
            $"{Status} (proportions {RatioDifference:P1} apart, showing {CropRect.width:P0} x {CropRect.height:P0} of the image)";
    }
}
