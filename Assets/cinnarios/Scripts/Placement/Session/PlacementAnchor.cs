using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Cinnarios.Placement.Session
{
    /// <summary>
    /// Creates and removes the AR anchor that keeps a locked painting fixed to the real wall
    /// as tracking improves. Removing it first unparents the painting, so it stays exactly
    /// where it is and can be slid again without the anchor fighting the drag.
    /// </summary>
    public sealed class PlacementAnchor
    {
        private GameObject _anchorObject;

        public void Attach(Transform painting)
        {
            Detach(painting);
            _anchorObject = new GameObject("Painting Anchor");
            _anchorObject.transform.SetPositionAndRotation(painting.position, painting.rotation);
            _anchorObject.AddComponent<ARAnchor>();
            painting.SetParent(_anchorObject.transform, true);
        }

        public void Detach(Transform painting)
        {
            if (_anchorObject == null)
            {
                return;
            }

            if (painting != null && painting.parent == _anchorObject.transform)
            {
                painting.SetParent(null, true);
            }

            Object.Destroy(_anchorObject);
            _anchorObject = null;
        }
    }
}
