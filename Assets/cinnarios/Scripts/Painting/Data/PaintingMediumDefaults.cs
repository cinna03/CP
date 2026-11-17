namespace Cinnarios.Painting.Data
{
    /// <summary>
    /// Sensible physical defaults for each medium, used when a source such as the Met
    /// gives width and height but no depth.
    /// </summary>
    public static class PaintingMediumDefaults
    {
        private const float CanvasDepthCm = 2.0f;
        private const float WoodPanelDepthCm = 1.0f;
        private const float PaperDepthCm = 0.1f;

        /// <summary>A typical thickness, in centimetres, for a painting on this medium.</summary>
        public static float DefaultDepthCm(this PaintingMedium medium)
        {
            switch (medium)
            {
                case PaintingMedium.WoodPanel:
                    return WoodPanelDepthCm;
                case PaintingMedium.Paper:
                    return PaperDepthCm;
                case PaintingMedium.Canvas:
                case PaintingMedium.Other:
                default:
                    return CanvasDepthCm;
            }
        }
    }
}
