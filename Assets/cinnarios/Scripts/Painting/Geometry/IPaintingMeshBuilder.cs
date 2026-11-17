using Cinnarios.Painting.Data;
using UnityEngine;

namespace Cinnarios.Painting.Geometry
{
    /// <summary>
    /// The one promise every painting builder makes: give it real dimensions and it
    /// returns a 3D mesh of exactly that size. Code outside Geometry depends only on
    /// this interface, never on how a particular builder shapes the painting.
    /// </summary>
    public interface IPaintingMeshBuilder
    {
        Mesh Build(PaintingDimensions dimensions);
    }
}
