using System;

namespace Capstone.Catalogue
{
    /// <summary>
    /// Width and height of a placed work, in metres.
    /// </summary>
    public readonly struct WallSize
    {
        public WallSize(float widthMetres, float heightMetres)
        {
            WidthMetres = widthMetres;
            HeightMetres = heightMetres;
        }

        public float WidthMetres { get; }

        public float HeightMetres { get; }
    }

    /// <summary>
    /// Converts stored centimetres into the metres Unity uses for a wall quad.
    /// One hundred centimetres is one metre.
    /// </summary>
    public static class ArtworkScale
    {
        public const float CentimetresPerMetre = 100f;

        public static WallSize ToWallSize(float widthCm, float heightCm)
        {
            if (widthCm <= 0f)
                throw new ArgumentOutOfRangeException(nameof(widthCm), widthCm, "Width must be a positive number of centimetres.");
            if (heightCm <= 0f)
                throw new ArgumentOutOfRangeException(nameof(heightCm), heightCm, "Height must be a positive number of centimetres.");

            return new WallSize(
                widthCm / CentimetresPerMetre,
                heightCm / CentimetresPerMetre);
        }
    }
}
