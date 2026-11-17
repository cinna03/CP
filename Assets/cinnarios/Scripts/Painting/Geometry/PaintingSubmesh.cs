namespace Cinnarios.Painting.Geometry
{
    /// <summary>
    /// The three parts a painting mesh is split into, so each can carry its own
    /// material: the image on the front, canvas or paint on the sides, a plain back.
    /// The numbers match the order of materials on the MeshRenderer.
    /// </summary>
    public enum PaintingSubmesh
    {
        Front = 0,
        Sides = 1,
        Back = 2
    }
}
