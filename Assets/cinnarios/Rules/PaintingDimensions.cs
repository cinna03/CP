using System;
using UnityEngine;

namespace Cinnarios.Painting.Data
{
    /// <summary>
    /// The real physical size of a painting, stored in centimetres because that is
    /// how museums and artists measure. This is the only place in the project that
    /// converts centimetres to Unity units (1 unit = 1 metre), so the true-size
    /// promise cannot break anywhere else.
    /// </summary>
    public sealed class PaintingDimensions
    {
        private const float CentimetresPerMetre = 100f;

        public float WidthCm { get; }
        public float HeightCm { get; }
        public float DepthCm { get; }

        /// <summary>Width divided by height. Above 1 is landscape, below 1 is portrait.</summary>
        public float AspectRatio => WidthCm / HeightCm;

        /// <summary>The same size in metres, ready to use as a Unity scale: x = width, y = height, z = depth.</summary>
        public Vector3 SizeInMetres => new Vector3(WidthCm, HeightCm, DepthCm) / CentimetresPerMetre;

        public PaintingDimensions(float widthCm, float heightCm, float depthCm)
        {
            WidthCm = RequirePositive(widthCm, nameof(widthCm));
            HeightCm = RequirePositive(heightCm, nameof(heightCm));
            DepthCm = RequirePositive(depthCm, nameof(depthCm));
        }

        public override string ToString() => $"{WidthCm} x {HeightCm} x {DepthCm} cm";

        // Rejects bad input as soon as the object is created, so no other class
        // ever has to check for zero, negative or missing sizes.
        private static float RequirePositive(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "A painting dimension must be greater than zero.");
            }

            return value;
        }
    }
}
