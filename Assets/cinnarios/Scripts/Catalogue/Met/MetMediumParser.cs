using Cinnarios.Painting.Data;

namespace Cinnarios.Catalogue.Met
{
    /// <summary>
    /// Turns the Met's free-text medium ("Oil on canvas", "Tempera on wood, gold ground")
    /// into one of our fixed media by looking for keywords ("Oil on oak" counts as a wood panel). Canvas is checked first, so
    /// "oil on canvas, mounted on wood" still counts as canvas.
    /// </summary>
    public static class MetMediumParser
    {
        // "Wood" and "panel", plus the woods old panel paintings were usually made of.
        private static readonly string[] WoodPanelWords = { "wood", "panel", "oak", "poplar", "walnut", "linden", "lime" };

        public static PaintingMedium Parse(string medium)
        {
            if (string.IsNullOrWhiteSpace(medium))
            {
                return PaintingMedium.Other;
            }

            string text = medium.ToLowerInvariant();

            if (text.Contains("canvas"))
            {
                return PaintingMedium.Canvas;
            }

            foreach (string woodWord in WoodPanelWords)
            {
                if (text.Contains(woodWord))
                {
                    return PaintingMedium.WoodPanel;
                }
            }

            if (text.Contains("paper") || text.Contains("watercolor") || text.Contains("vellum") || text.Contains("parchment"))
            {
                return PaintingMedium.Paper;
            }

            return PaintingMedium.Other;
        }
    }
}
