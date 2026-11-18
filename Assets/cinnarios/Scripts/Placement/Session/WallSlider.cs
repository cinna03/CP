using UnityEngine;

namespace Cinnarios.Placement.Session
{
    /// <summary>
    /// Slides a hung painting along the wall it was hung on. The wall is remembered at the
    /// tap as a mathematical plane through the painting (which already sits 1 mm off the
    /// wall, upright and at museum size). A drag casts a ray from the camera through the
    /// finger and finds where it meets that plane, so the painting can only move along the
    /// wall: never off it, never tilted, never resized. Height is free on the wall; the
    /// gallery-height rule is applied once at the tap, not on every drag.
    /// </summary>
    public sealed class WallSlider
    {
        private Plane _wallPlane;
        private Vector3 _grabOffset;
        private bool _hasWall;
        private bool _hasGrab;

        /// <summary>The painting's fixed, upright rotation on this wall.</summary>
        public Quaternion Rotation { get; private set; } = Quaternion.identity;

        public bool HasWall => _hasWall;

        /// <summary>Remembers the wall the painting was just hung on.</summary>
        public void Capture(Pose paintingPose)
        {
            // The painting's forward (+Z) points into the wall, so the wall faces the other way.
            Vector3 wallNormal = -(paintingPose.rotation * Vector3.forward);
            wallNormal.y = 0f;
            wallNormal.Normalize();

            _wallPlane = new Plane(wallNormal, paintingPose.position);
            Rotation = paintingPose.rotation;
            _hasWall = true;
            _hasGrab = false;
        }

        /// <summary>
        /// Called when a drag starts. Remembers where on the painting the finger grabbed it, so
        /// the painting moves with the finger instead of jumping its centre to the finger.
        /// </summary>
        public void BeginDrag(Vector2 screenPosition, Camera camera, Vector3 paintingPosition)
        {
            _hasGrab = TryHitWall(screenPosition, camera, out Vector3 hit);
            _grabOffset = _hasGrab ? paintingPosition - hit : Vector3.zero;
        }

        /// <returns>True with the painting's new position if the finger is over the wall.</returns>
        public bool TryDrag(Vector2 screenPosition, Camera camera, out Vector3 paintingPosition)
        {
            paintingPosition = default;
            if (!_hasGrab || !TryHitWall(screenPosition, camera, out Vector3 hit))
            {
                return false;
            }

            paintingPosition = hit + _grabOffset;
            return true;
        }

        public void Clear()
        {
            _hasWall = false;
            _hasGrab = false;
        }

        private bool TryHitWall(Vector2 screenPosition, Camera camera, out Vector3 hit)
        {
            hit = default;
            if (!_hasWall || camera == null)
            {
                return false;
            }

            Ray ray = camera.ScreenPointToRay(screenPosition);
            if (_wallPlane.Raycast(ray, out float distance) && distance > 0f)
            {
                hit = ray.GetPoint(distance);
                return true;
            }

            return false;
        }
    }
}
