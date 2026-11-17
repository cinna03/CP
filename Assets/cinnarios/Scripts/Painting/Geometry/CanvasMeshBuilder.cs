namespace Cinnarios.Painting.Geometry
{
    /// <summary>
    /// A stretched canvas: the fabric wraps over a wooden frame, so the front edge is
    /// slightly rounded and catches the light.
    /// </summary>
    public sealed class CanvasMeshBuilder : PaintingMeshBuilder
    {
        protected override float EdgeRadiusMetres => 0.004f; // 4 mm
        protected override int EdgeSegments => 4;
    }
}
