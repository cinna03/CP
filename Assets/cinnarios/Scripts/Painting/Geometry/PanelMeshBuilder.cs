namespace Cinnarios.Painting.Geometry
{
    /// <summary>A wooden panel or mounted paper: flat and rigid, with sharp edges.</summary>
    public sealed class PanelMeshBuilder : PaintingMeshBuilder
    {
        protected override float EdgeRadiusMetres => 0f;
        protected override int EdgeSegments => 0;
    }
}
