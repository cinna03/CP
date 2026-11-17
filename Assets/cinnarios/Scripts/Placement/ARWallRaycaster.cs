using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Cinnarios.Placement
{
    /// <summary>
    /// Finds walls using AR Foundation: casts a ray from a screen point into the detected
    /// planes and accepts only vertical ones.
    /// </summary>
    public sealed class ARWallRaycaster : MonoBehaviour, IWallHitProvider
    {
        [SerializeField] private ARRaycastManager raycastManager;

        // Reused every frame instead of allocating a new list.
        private readonly List<ARRaycastHit> _hits = new List<ARRaycastHit>();

        public bool TryGetWallHit(Vector2 screenPoint, out Pose wallPose)
        {
            if (raycastManager != null && raycastManager.Raycast(screenPoint, _hits, TrackableType.PlaneWithinPolygon))
            {
                // Hits are sorted nearest first; take the nearest one that is a wall.
                foreach (ARRaycastHit hit in _hits)
                {
                    if (hit.trackable is ARPlane plane && plane.alignment == PlaneAlignment.Vertical)
                    {
                        wallPose = hit.pose;
                        return true;
                    }
                }
            }

            wallPose = default;
            return false;
        }
    }
}
