namespace Cinnarios.Painting.Data
{
    /// <summary>
    /// What the painting is physically made on. The support decides how thick the
    /// painting is and, later, which surface texture it gets.
    /// </summary>
    public enum PaintingMedium
    {
        /// <summary>Oil or acrylic on stretched canvas.</summary>
        Canvas,

        /// <summary>Tempera or oil on a wooden panel: thinner, with hard edges.</summary>
        WoodPanel,

        /// <summary>Watercolour, ink or drawing on paper.</summary>
        Paper,

        /// <summary>Fallback when the source medium cannot be matched to one of the above.</summary>
        Other
    }
}
