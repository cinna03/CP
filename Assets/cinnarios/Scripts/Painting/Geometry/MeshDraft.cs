using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Cinnarios.Painting.Geometry
{
    /// <summary>
    /// Collects faces while a mesh is being built, then turns them into a Unity Mesh.
    /// Its only job is bookkeeping: it does not decide what shape is being built.
    /// </summary>
    internal sealed class MeshDraft
    {
        private const int MaxVerticesFor16BitIndices = 65535;

        private readonly List<Vector3> _positions = new List<Vector3>();
        private readonly List<Vector3> _normals = new List<Vector3>();
        private readonly List<Vector2> _uvs = new List<Vector2>();
        private readonly List<int>[] _trianglesPerSubmesh;

        public MeshDraft(int submeshCount)
        {
            _trianglesPerSubmesh = new List<int>[submeshCount];
            for (int i = 0; i < submeshCount; i++)
            {
                _trianglesPerSubmesh[i] = new List<int>();
            }
        }

        /// <summary>
        /// Adds a four-cornered face made of two triangles. The corners must go round the
        /// face in order. <paramref name="outward"/> is the side the face should be seen from.
        /// </summary>
        public void AddQuad(int submesh, Vector3 outward, MeshVertex a, MeshVertex b, MeshVertex c, MeshVertex d)
        {
            int first = _positions.Count;
            AddVertex(a);
            AddVertex(b);
            AddVertex(c);
            AddVertex(d);

            // Unity only draws the side of a triangle whose corners appear clockwise to the
            // viewer. The cross product tells us which way the corners turn, so we flip the
            // order when needed instead of relying on every caller getting it right.
            Vector3 facing = Vector3.Cross(b.Position - a.Position, c.Position - a.Position);
            bool alreadyClockwise = Vector3.Dot(facing, outward) >= 0f;

            List<int> triangles = _trianglesPerSubmesh[submesh];
            if (alreadyClockwise)
            {
                triangles.Add(first); triangles.Add(first + 1); triangles.Add(first + 2);
                triangles.Add(first); triangles.Add(first + 2); triangles.Add(first + 3);
            }
            else
            {
                triangles.Add(first); triangles.Add(first + 2); triangles.Add(first + 1);
                triangles.Add(first); triangles.Add(first + 3); triangles.Add(first + 2);
            }
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            if (_positions.Count > MaxVerticesFor16BitIndices)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.SetVertices(_positions);
            mesh.SetNormals(_normals);
            mesh.SetUVs(0, _uvs);
            mesh.subMeshCount = _trianglesPerSubmesh.Length;
            for (int i = 0; i < _trianglesPerSubmesh.Length; i++)
            {
                mesh.SetTriangles(_trianglesPerSubmesh[i], i);
            }

            mesh.RecalculateBounds();
            mesh.RecalculateTangents(); // needed later for the normal map on the front
            return mesh;
        }

        private void AddVertex(MeshVertex vertex)
        {
            _positions.Add(vertex.Position);
            _normals.Add(vertex.Normal);
            _uvs.Add(vertex.Uv);
        }
    }
}
