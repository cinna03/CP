using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Cinnarios.Placement.Session
{
    /// <summary>
    /// Keeps the wall mark as a faint wash and a thin edge. Floors stay invisible.
    /// The plane finder turns the bright debug fill back on, so this runs after it.
    /// </summary>
    public sealed class QuietPlaneMark : MonoBehaviour
    {
        private ARPlane _plane;
        private MeshRenderer _fill;
        private LineRenderer _edge;
        private ARPlaneMeshVisualizer _visualizer;

        private void Awake()
        {
            _plane = GetComponent<ARPlane>();
            _fill = GetComponent<MeshRenderer>();
            _edge = GetComponent<LineRenderer>();
            _visualizer = GetComponent<ARPlaneMeshVisualizer>();
        }

        private void LateUpdate()
        {
            if (_plane == null)
            {
                return;
            }

            bool wall = _plane.alignment == PlaneAlignment.Vertical;
            if (!wall)
            {
                if (_visualizer != null)
                {
                    _visualizer.enabled = false;
                }

                if (_fill != null)
                {
                    _fill.enabled = false;
                }

                if (_edge != null)
                {
                    _edge.enabled = false;
                }

                return;
            }

            if (_fill != null)
            {
                _fill.enabled = false;
            }

            if (_edge != null)
            {
                _edge.enabled = true;
                _edge.widthMultiplier = 0.006f;
                Color ink = new Color(1f, 1f, 1f, 0.85f);
                _edge.startColor = ink;
                _edge.endColor = ink;
            }
        }
    }
}
