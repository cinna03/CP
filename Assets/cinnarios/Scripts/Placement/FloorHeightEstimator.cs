using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Cinnarios.Placement
{
    /// <summary>
    /// Estimates the floor height from the detected planes: the lowest upward-facing
    /// horizontal plane is taken to be the floor (tables and sofas are always higher).
    /// </summary>
    public static class FloorHeightEstimator
    {
        public static bool TryEstimate(ARPlaneManager planeManager, out float floorHeight)
        {
            floorHeight = float.MaxValue;
            bool found = false;

            if (planeManager == null)
            {
                return false;
            }

            foreach (ARPlane plane in planeManager.trackables)
            {
                if (plane.alignment != PlaneAlignment.HorizontalUp)
                {
                    continue;
                }

                if (plane.center.y < floorHeight)
                {
                    floorHeight = plane.center.y;
                    found = true;
                }
            }

            return found;
        }
    }
}
