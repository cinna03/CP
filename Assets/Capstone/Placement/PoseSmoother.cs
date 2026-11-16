using UnityEngine;

namespace Capstone.Placement
{
    /// <summary>
    /// Frame-rate-independent exponential smoothing for a pose.
    /// Each step moves a fraction 1 - e^(-sharpness * dt) of the remaining distance, so the
    /// motion looks the same at 30 or 120 frames per second. Position uses Lerp and rotation
    /// uses Slerp. A jump larger than SnapDistance is applied at once, so a new wall is never
    /// reached by a long slide across the room.
    /// </summary>
    public sealed class PoseSmoother
    {
        public const float DefaultSharpness = 14f;
        public const float SnapDistance = 0.75f;

        readonly float sharpness;
        bool started;

        public PoseSmoother(float sharpness = DefaultSharpness)
        {
            this.sharpness = Mathf.Max(0.01f, sharpness);
        }

        public Vector3 Position { get; private set; }

        public Quaternion Rotation { get; private set; } = Quaternion.identity;

        /// <summary>The fraction of the gap closed in one step of <paramref name="deltaTime"/> seconds.</summary>
        public static float Blend(float sharpness, float deltaTime)
        {
            if (deltaTime <= 0f)
                return 0f;
            return 1f - Mathf.Exp(-sharpness * deltaTime);
        }

        /// <summary>Jumps straight to a pose, for the first frame or after a reset.</summary>
        public void SnapTo(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
            started = true;
        }

        /// <summary>Moves part of the way toward the target and returns the new position.</summary>
        public Vector3 Step(Vector3 targetPosition, Quaternion targetRotation, float deltaTime)
        {
            if (!started || Vector3.Distance(Position, targetPosition) > SnapDistance)
            {
                SnapTo(targetPosition, targetRotation);
                return Position;
            }

            float t = Blend(sharpness, deltaTime);
            Position = Vector3.Lerp(Position, targetPosition, t);
            Rotation = Quaternion.Slerp(Rotation, targetRotation, t);
            return Position;
        }

        /// <summary>True once the pose is within a millimetre and a tenth of a degree of the target.</summary>
        public bool Arrived(Vector3 targetPosition, Quaternion targetRotation)
        {
            return Vector3.Distance(Position, targetPosition) < 0.001f
                && Quaternion.Angle(Rotation, targetRotation) < 0.1f;
        }
    }
}
