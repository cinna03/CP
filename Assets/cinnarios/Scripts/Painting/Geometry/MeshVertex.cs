using UnityEngine;

namespace Cinnarios.Painting.Geometry
{
    /// <summary>One corner of a face: where it is, which way it faces, and which point of a texture it shows.</summary>
    internal readonly struct MeshVertex
    {
        public readonly Vector3 Position;
        public readonly Vector3 Normal;
        public readonly Vector2 Uv;

        public MeshVertex(Vector3 position, Vector3 normal, Vector2 uv)
        {
            Position = position;
            Normal = normal;
            Uv = uv;
        }
    }
}
