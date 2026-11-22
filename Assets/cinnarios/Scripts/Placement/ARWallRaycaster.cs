using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Cinnarios.Placement
{
    /// <summary>
    /// Finds walls using AR Foundation: casts a ray from a screen point into the detected
    /// planes and accepts only vertical ones.
    ///
    /// Two choices make it reliable:
    /// - The wall's direction comes from the detected plane itself, flipped to face the
    ///   camera, not from the raycast hit's rotation, so the painting is always upright
    ///   and facing the viewer.
    /// - Once part of a wall is detected, the rest of that wall counts too ("within
    ///   infinity"). Hits inside the detected patch are preferred when both exist.
    /// </summary>
    public sealed class ARWallRaycaster : MonoBehaviour, IWallHitProvider
    {
        // Ignore planes whose normal is almost straight up or down after flattening.
        private const float MinimumHorizontalNormalLength = 0.5f;

        [SerializeField] private ARRaycastManager raycastManager;
        [Tooltip("The AR camera. Left empty, the main camera is used.")]
        [SerializeField] private Camera arCamera;

        // Reused every frame instead of allocating a new list.
        private readonly List<ARRaycastHit> _hits = new List<ARRaycastHit>();
        private TrackableId _heldPlane;

        private const TrackableType WallHitTypes = TrackableType.PlaneWithinPolygon | TrackableType.PlaneWithinInfinity;

        private void Awake()
        {
            if (arCamera == null)
            {
                arCamera = Camera.main;
            }
        }

        public bool TryGetWallHit(Vector2 screenPoint, out Pose wallPose)
        {
            wallPose = default;
            if (raycastManager == null || arCamera == null || !raycastManager.Raycast(screenPoint, _hits, WallHitTypes))
            {
                return false;
            }

            // Stay on the wall already chosen. A nearer patch must not yank the painting sideways.
            if (TryFindHeldPlane(out wallPose))
            {
                return true;
            }

            // First look for a hit inside a detected wall patch, then on a wall's extension.
            return TryFindWall(TrackableType.PlaneWithinPolygon, out wallPose)
                || TryFindWall(TrackableType.PlaneWithinInfinity, out wallPose);
        }

        private bool TryFindHeldPlane(out Pose wallPose)
        {
            wallPose = default;
            if (_heldPlane == TrackableId.invalidId)
            {
                return false;
            }

            foreach (ARRaycastHit hit in _hits)
            {
                if (hit.trackableId != _heldPlane || hit.trackable is not ARPlane plane || plane.alignment != PlaneAlignment.Vertical)
                {
                    continue;
                }

                if (TryWallNormalFacingCamera(plane.normal, hit.pose.position, out Vector3 normal))
                {
                    wallPose = new Pose(hit.pose.position, Quaternion.FromToRotation(Vector3.up, normal));
                    return true;
                }
            }

            _heldPlane = TrackableId.invalidId;
            return false;
        }

        // Hits are sorted nearest first, so the first matching wall is the one in front of the camera.
        private bool TryFindWall(TrackableType hitType, out Pose wallPose)
        {
            foreach (ARRaycastHit hit in _hits)
            {
                if ((hit.hitType & hitType) == 0)
                {
                    continue;
                }

                if (!(hit.trackable is ARPlane plane) || plane.alignment != PlaneAlignment.Vertical)
                {
                    continue;
                }

                if (TryWallNormalFacingCamera(plane.normal, hit.pose.position, out Vector3 normal))
                {
                    // Contract of IWallHitProvider: the pose's up direction is the wall's normal.
                    _heldPlane = hit.trackableId;
                    wallPose = new Pose(hit.pose.position, Quaternion.FromToRotation(Vector3.up, normal));
                    return true;
                }
            }

            wallPose = default;
            return false;
        }

        // A wall's normal could point into the wall or out of it; we need the side the viewer is on.
        // It is also flattened to horizontal so the painting always hangs straight.
        private bool TryWallNormalFacingCamera(Vector3 planeNormal, Vector3 hitPoint, out Vector3 normal)
        {
            normal = planeNormal;
            normal.y = 0f;
            if (normal.magnitude < MinimumHorizontalNormalLength)
            {
                return false;
            }

            normal.Normalize();
            Vector3 towardsCamera = arCamera.transform.position - hitPoint;
            if (Vector3.Dot(normal, towardsCamera) < 0f)
            {
                normal = -normal;
            }

            return true;
        }
    }
}
