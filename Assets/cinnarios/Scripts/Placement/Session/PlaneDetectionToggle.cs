using UnityEngine.XR.ARFoundation;

namespace Cinnarios.Placement.Session
{
    /// <summary>
    /// Switches wall searching and the yellow plane outlines on or off together. Lock turns
    /// them off (saves processing and tidies the view); only Scan again turns them back on.
    /// </summary>
    public sealed class PlaneDetectionToggle
    {
        private readonly ARPlaneManager _planeManager;

        public PlaneDetectionToggle(ARPlaneManager planeManager)
        {
            _planeManager = planeManager;
        }

        public void SetSearching(bool searching)
        {
            if (_planeManager == null)
            {
                return;
            }

            _planeManager.enabled = searching;
            foreach (ARPlane plane in _planeManager.trackables)
            {
                plane.gameObject.SetActive(searching);
            }
        }
    }
}
