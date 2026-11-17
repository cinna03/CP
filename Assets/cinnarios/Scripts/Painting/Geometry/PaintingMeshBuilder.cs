using System;
using Cinnarios.Painting.Data;
using UnityEngine;

namespace Cinnarios.Painting.Geometry
{
    /// <summary>
    /// Builds a painting as a box at its real size: a front carrying the image, four sides
    /// and a back. Subclasses only decide how the front edge is shaped (rounded or sharp).
    ///
    /// Coordinates: 1 unit = 1 metre. The origin is the centre of the BACK face, so the
    /// painting sits flush on a wall when placed. The front faces -Z (towards the viewer)
    /// and +Z points into the wall.
    /// </summary>
    public abstract class PaintingMeshBuilder : IPaintingMeshBuilder
    {
        public const int SubmeshCount = 3;

        private static readonly Vector3[] SideOutwardDirections =
        {
            Vector3.left,  // side from corner 0 (bottom-left) to corner 1 (top-left)
            Vector3.up,    // corner 1 (top-left) to corner 2 (top-right)
            Vector3.right, // corner 2 (top-right) to corner 3 (bottom-right)
            Vector3.down   // corner 3 (bottom-right) back to corner 0 (bottom-left)
        };

        /// <summary>How rounded the front edge is, in metres. 0 means a sharp edge.</summary>
        protected abstract float EdgeRadiusMetres { get; }

        /// <summary>How many strips make up the rounded edge. More strips give a smoother curve.</summary>
        protected abstract int EdgeSegments { get; }

        public Mesh Build(PaintingDimensions dimensions)
        {
            if (dimensions == null)
            {
                throw new ArgumentNullException(nameof(dimensions));
            }

            Vector3 size = dimensions.SizeInMetres;
            var box = new Box(size.x * 0.5f, size.y * 0.5f, size.z);
            float radius = ClampEdgeRadius(EdgeRadiusMetres, box);
            int segments = radius > 0f ? Mathf.Max(1, EdgeSegments) : 0;

            var draft = new MeshDraft(SubmeshCount);
            AddFront(draft, box, radius);
            AddRoundedEdge(draft, box, radius, segments);
            AddSides(draft, box, radius);
            AddBack(draft, box);

            return draft.ToMesh($"Painting_{dimensions.WidthCm}x{dimensions.HeightCm}x{dimensions.DepthCm}cm");
        }

        // The flat front, inset by the edge radius. Its UVs stretch 0..1 over the WHOLE
        // painting, so the image fills the front exactly once and carries on smoothly
        // over the rounded edge, the way paint runs over a real canvas edge.
        private static void AddFront(MeshDraft draft, Box box, float radius)
        {
            float z = -box.Depth;
            MeshVertex Corner(int index)
            {
                Vector3 position = box.Corner(index, radius, z);
                return new MeshVertex(position, Vector3.back, box.FrontUv(position));
            }

            draft.AddQuad((int)PaintingSubmesh.Front, Vector3.back, Corner(0), Corner(1), Corner(2), Corner(3));
        }

        // A quarter-circle curve from the front face round to each side. Each strip's
        // normal blends from "facing the viewer" to "facing outwards", so light rolls
        // over the edge instead of breaking at a hard line.
        private static void AddRoundedEdge(MeshDraft draft, Box box, float radius, int segments)
        {
            for (int side = 0; side < 4; side++)
            {
                Vector3 outward = SideOutwardDirections[side];
                for (int step = 0; step < segments; step++)
                {
                    float angleA = (step / (float)segments) * Mathf.PI * 0.5f;
                    float angleB = ((step + 1) / (float)segments) * Mathf.PI * 0.5f;

                    EdgePoint(radius, box.Depth, angleA, out float insetA, out float zA);
                    EdgePoint(radius, box.Depth, angleB, out float insetB, out float zB);
                    Vector3 normalA = EdgeNormal(outward, angleA);
                    Vector3 normalB = EdgeNormal(outward, angleB);

                    Vector3 p0 = box.Corner(side, insetA, zA);
                    Vector3 p1 = box.Corner(side + 1, insetA, zA);
                    Vector3 p2 = box.Corner(side + 1, insetB, zB);
                    Vector3 p3 = box.Corner(side, insetB, zB);

                    draft.AddQuad(
                        (int)PaintingSubmesh.Front,
                        (normalA + normalB).normalized,
                        new MeshVertex(p0, normalA, box.FrontUv(p0)),
                        new MeshVertex(p1, normalA, box.FrontUv(p1)),
                        new MeshVertex(p2, normalB, box.FrontUv(p2)),
                        new MeshVertex(p3, normalB, box.FrontUv(p3)));
                }
            }
        }

        // The four flat sides, from where the rounded edge ends back to the wall.
        // UVs are in metres, so a canvas texture keeps the same weave size on every
        // side and every painting instead of being stretched.
        private static void AddSides(MeshDraft draft, Box box, float radius)
        {
            float zFront = -box.Depth + radius;
            const float zBack = 0f;

            for (int side = 0; side < 4; side++)
            {
                Vector3 outward = SideOutwardDirections[side];
                Vector3 along = (box.Corner(side + 1, 0f, zBack) - box.Corner(side, 0f, zBack)).normalized;

                MeshVertex Vertex(Vector3 position) =>
                    new MeshVertex(position, outward, new Vector2(Vector3.Dot(position, along), position.z));

                draft.AddQuad(
                    (int)PaintingSubmesh.Sides,
                    outward,
                    Vertex(box.Corner(side, 0f, zFront)),
                    Vertex(box.Corner(side + 1, 0f, zFront)),
                    Vertex(box.Corner(side + 1, 0f, zBack)),
                    Vertex(box.Corner(side, 0f, zBack)));
            }
        }

        // The flat back that rests against the wall.
        private static void AddBack(MeshDraft draft, Box box)
        {
            MeshVertex Corner(int index)
            {
                Vector3 position = box.Corner(index, 0f, 0f);
                return new MeshVertex(position, Vector3.forward, new Vector2(position.x, position.y));
            }

            draft.AddQuad((int)PaintingSubmesh.Back, Vector3.forward, Corner(0), Corner(1), Corner(2), Corner(3));
        }

        // Where a point on the quarter-circle edge sits. At angle 0 it is on the front
        // face (inset by the radius); at 90 degrees it is on the side (no inset).
        private static void EdgePoint(float radius, float depth, float angle, out float inset, out float z)
        {
            inset = radius - radius * Mathf.Sin(angle);
            z = -depth + radius - radius * Mathf.Cos(angle);
        }

        private static Vector3 EdgeNormal(Vector3 outward, float angle) =>
            (outward * Mathf.Sin(angle) + Vector3.back * Mathf.Cos(angle)).normalized;

        // Keeps the rounding sensible on very thin or very small paintings.
        private static float ClampEdgeRadius(float requested, Box box) =>
            Mathf.Max(0f, Mathf.Min(requested, box.Depth * 0.5f, box.HalfWidth * 0.5f, box.HalfHeight * 0.5f));

        /// <summary>The painting's outline, with helpers to find its corners and front UVs.</summary>
        private readonly struct Box
        {
            public readonly float HalfWidth;
            public readonly float HalfHeight;
            public readonly float Depth;

            public Box(float halfWidth, float halfHeight, float depth)
            {
                HalfWidth = halfWidth;
                HalfHeight = halfHeight;
                Depth = depth;
            }

            /// <summary>Corner 0 bottom-left, 1 top-left, 2 top-right, 3 bottom-right; wraps round past 3.</summary>
            public Vector3 Corner(int index, float inset, float z)
            {
                float x = HalfWidth - inset;
                float y = HalfHeight - inset;
                switch (index % 4)
                {
                    case 0: return new Vector3(-x, -y, z);
                    case 1: return new Vector3(-x, y, z);
                    case 2: return new Vector3(x, y, z);
                    default: return new Vector3(x, -y, z);
                }
            }

            public Vector2 FrontUv(Vector3 position) =>
                new Vector2((position.x + HalfWidth) / (2f * HalfWidth), (position.y + HalfHeight) / (2f * HalfHeight));
        }
    }
}
