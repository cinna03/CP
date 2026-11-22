using UnityEngine;

namespace Cinnarios.Placement
{
    /// <summary>
    /// Removes AR tracking jitter by moving smoothly towards each new target pose instead of
    /// jumping to it (exponential smoothing). Each frame it closes a fixed share of the gap,
    /// worked out from the frame time, so it behaves the same at 30 or 60 frames per second.
    /// </summary>
    public sealed class PoseSmoother
    {
        private readonly float _sharpness;
        private Pose _current;
        private bool _hasPose;

        /// <param name="sharpness">Higher follows faster; lower is smoother. Around 8-15 suits hand-held AR.</param>
        public PoseSmoother(float sharpness)
        {
            _sharpness = Mathf.Max(0.01f, sharpness);
        }

        public Pose Step(Pose target, float deltaTime)
        {
            if (!_hasPose)
            {
                _current = target;
                _hasPose = true;
                return _current;
            }

            // Plane updates shiver by a few millimetres. Ignore that, follow a real move.
            if (Vector3.Distance(_current.position, target.position) < 0.004f
                && Quaternion.Angle(_current.rotation, target.rotation) < 1.5f)
            {
                return _current;
            }

            float t = 1f - Mathf.Exp(-_sharpness * deltaTime);
            _current = new Pose(
                Vector3.Lerp(_current.position, target.position, t),
                Quaternion.Slerp(_current.rotation, target.rotation, t));
            return _current;
        }

        /// <summary>Forget the last pose, so the next target is jumped to directly.</summary>
        public void Reset() => _hasPose = false;
    }
}
