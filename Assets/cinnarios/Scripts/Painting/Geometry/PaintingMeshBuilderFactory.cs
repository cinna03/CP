using Cinnarios.Painting.Data;

namespace Cinnarios.Painting.Geometry
{
    /// <summary>
    /// Picks the right builder for a painting's medium. Builders hold no state, so one
    /// of each is shared rather than creating a new one for every painting.
    /// </summary>
    public static class PaintingMeshBuilderFactory
    {
        private static readonly IPaintingMeshBuilder Canvas = new CanvasMeshBuilder();
        private static readonly IPaintingMeshBuilder Panel = new PanelMeshBuilder();

        public static IPaintingMeshBuilder For(PaintingMedium medium)
        {
            switch (medium)
            {
                case PaintingMedium.WoodPanel:
                case PaintingMedium.Paper:
                    return Panel;
                case PaintingMedium.Canvas:
                case PaintingMedium.Other:
                default:
                    return Canvas;
            }
        }
    }
}
