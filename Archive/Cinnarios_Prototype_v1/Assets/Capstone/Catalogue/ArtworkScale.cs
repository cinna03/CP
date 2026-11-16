namespace Capstone.Catalogue
{
    /// <summary>
    /// Wall size is the stored centimetres divided by 100.
    /// </summary>
    public static class ArtworkScale
    {
        public const float CentimetresPerMetre = 100f;

        public static float ToMetres(float centimetres)
        {
            return centimetres / CentimetresPerMetre;
        }
    }
}
