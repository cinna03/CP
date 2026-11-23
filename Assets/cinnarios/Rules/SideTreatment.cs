namespace Cinnarios.Painting.Data
{
    /// <summary>
    /// How the four sides of the painting look. Gallery wrap is not offered because
    /// source photos show only the front, so there is no image to wrap round the edge.
    /// </summary>
    public enum SideTreatment
    {
        /// <summary>Bare canvas or support on the sides. The default.</summary>
        PlainEdge,

        /// <summary>Sides painted in a single colour taken from the artwork's border.</summary>
        PaintedEdge
    }
}
