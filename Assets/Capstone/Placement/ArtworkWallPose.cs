using Capstone.Catalogue;
using UnityEngine;

namespace Capstone.Placement
{
    /// <summary>
    /// Where a quad sits on a wall: position, rotation that faces the camera, and metre scale.
    /// </summary>
    public readonly struct WallPose
    {
        public WallPose(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }

        public Vector3 Position { get; }

        public Quaternion Rotation { get; }

        public Vector3 Scale { get; }
    }

    /// <summary>
    /// Builds the quad pose from a stored artwork and a hit on a vertical plane.
    /// A Unity quad faces its forward axis and is 1 metre by 1 metre before scaling.
    /// </summary>
    public static class ArtworkWallPose
    {
        public const float SurfaceOffsetMetres = 0.005f;

        public static WallPose At(Artwork artwork, Vector3 hitPoint, Vector3 planeNormal, Vector3 cameraPosition)
        {
            Vector3 normal = planeNormal.sqrMagnitude > 0f ? planeNormal.normalized : Vector3.forward;
            Vector3 towardCamera = cameraPosition - hitPoint;
            if (Vector3.Dot(normal, towardCamera) < 0f)
                normal = -normal;

            WallSize size = artwork.WallSizeMetres;
            return new WallPose(
                hitPoint + normal * SurfaceOffsetMetres,
                Quaternion.LookRotation(normal, Vector3.up),
                new Vector3(size.WidthMetres, size.HeightMetres, 1f));
        }
    }
}
