using UnityEngine;

namespace Capstone.Placement
{
    /// <summary>
    /// Eases a just-placed artwork onto the wall: it starts a few centimetres in front of the
    /// surface and settles into the stored pose with <see cref="PoseSmoother"/>, so tracking
    /// corrections and the moment of placement read as one calm motion instead of a jump.
    /// </summary>
    public sealed class PlacementSettle : MonoBehaviour
    {
        const float LiftMetres = 0.06f;
        const float MaxSeconds = 0.8f;

        readonly PoseSmoother smoother = new PoseSmoother();
        Vector3 targetPosition;
        Quaternion targetRotation;
        float started;

        public static void Begin(Transform piece, Vector3 position, Quaternion rotation)
        {
            if (piece == null)
                return;
            PlacementSettle settle = piece.GetComponent<PlacementSettle>();
            if (settle == null)
                settle = piece.gameObject.AddComponent<PlacementSettle>();
            settle.targetPosition = position;
            settle.targetRotation = rotation;
            settle.smoother.SnapTo(position + rotation * Vector3.forward * LiftMetres, rotation);
            settle.started = Time.unscaledTime;
            piece.SetPositionAndRotation(settle.smoother.Position, settle.smoother.Rotation);
            settle.enabled = true;
        }

        void LateUpdate()
        {
            smoother.Step(targetPosition, targetRotation, Time.unscaledDeltaTime);
            transform.SetPositionAndRotation(smoother.Position, smoother.Rotation);
            if (smoother.Arrived(targetPosition, targetRotation) || Time.unscaledTime - started > MaxSeconds)
            {
                transform.SetPositionAndRotation(targetPosition, targetRotation);
                enabled = false;
            }
        }
    }
}
