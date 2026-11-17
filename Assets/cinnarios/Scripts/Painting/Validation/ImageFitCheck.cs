using System;
using Cinnarios.Painting.Data;
using UnityEngine;

namespace Cinnarios.Painting.Validation
{
    /// <summary>
    /// Compares the shape of a photo with the shape of the real painting before anything is
    /// built, so an image is never stretched or squashed onto the 3D front.
    ///
    /// Both shapes are reduced to a ratio (width / height) and compared as a percentage:
    /// up to 2% is a match, up to 8% is fixed with a centred crop, otherwise the check tries
    /// swapping width and height before giving up.
    /// </summary>
    public sealed class ImageFitCheck
    {
        public const float DefaultMatchTolerance = 0.02f;
        public const float DefaultCropTolerance = 0.08f;

        private readonly float _matchTolerance;
        private readonly float _cropTolerance;

        public ImageFitCheck(float matchTolerance = DefaultMatchTolerance, float cropTolerance = DefaultCropTolerance)
        {
            if (matchTolerance <= 0f || cropTolerance <= matchTolerance)
            {
                throw new ArgumentException("Tolerances must satisfy 0 < match tolerance < crop tolerance.");
            }

            _matchTolerance = matchTolerance;
            _cropTolerance = cropTolerance;
        }

        public ImageFitResult Evaluate(int imageWidthPx, int imageHeightPx, PaintingDimensions dimensions)
        {
            if (imageWidthPx <= 0 || imageHeightPx <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(imageWidthPx), "Image size must be greater than zero.");
            }

            if (dimensions == null)
            {
                throw new ArgumentNullException(nameof(dimensions));
            }

            float imageRatio = imageWidthPx / (float)imageHeightPx;

            ImageFitResult asRecorded = TryFit(imageRatio, dimensions, ImageFitStatus.Match);
            if (asRecorded != null)
            {
                return asRecorded;
            }

            // The recorded size may simply be the wrong way round (height given as width).
            var swapped = new PaintingDimensions(dimensions.HeightCm, dimensions.WidthCm, dimensions.DepthCm);
            ImageFitResult asSwapped = TryFit(imageRatio, swapped, ImageFitStatus.DimensionsSwapped);
            if (asSwapped != null)
            {
                return asSwapped;
            }

            float difference = RelativeDifference(imageRatio, dimensions.AspectRatio);
            return new ImageFitResult(ImageFitStatus.Mismatch, ImageFitResult.FullImage, dimensions, difference);
        }

        // Returns a usable result if the image fits these dimensions (exactly or with a crop),
        // or null if it does not fit at all.
        private ImageFitResult TryFit(float imageRatio, PaintingDimensions dimensions, ImageFitStatus statusWhenExact)
        {
            float targetRatio = dimensions.AspectRatio;
            float difference = RelativeDifference(imageRatio, targetRatio);

            if (difference <= _matchTolerance)
            {
                return new ImageFitResult(statusWhenExact, ImageFitResult.FullImage, dimensions, difference);
            }

            if (difference <= _cropTolerance)
            {
                ImageFitStatus status = statusWhenExact == ImageFitStatus.Match ? ImageFitStatus.CropNeeded : statusWhenExact;
                return new ImageFitResult(status, CentreCrop(imageRatio, targetRatio), dimensions, difference);
            }

            return null;
        }

        private static float RelativeDifference(float actual, float target) => Mathf.Abs(actual - target) / target;

        // The largest centred rectangle of the image with exactly the painting's proportions.
        // A too-wide image loses a strip from each side; a too-tall one loses a strip from top and bottom.
        private static Rect CentreCrop(float imageRatio, float targetRatio)
        {
            if (imageRatio > targetRatio)
            {
                float width = targetRatio / imageRatio;
                return new Rect((1f - width) * 0.5f, 0f, width, 1f);
            }

            float height = imageRatio / targetRatio;
            return new Rect(0f, (1f - height) * 0.5f, 1f, height);
        }
    }
}
