using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Cinnarios.Placement
{
    /// <summary>
    /// Asks ARKit to show its own wall-finding guide until a vertical plane exists.
    /// The guide is Apple's, so the motion it asks for is the one the detector needs.
    /// </summary>
    public static class WallCoaching
    {
        public static bool AskForAWall()
        {
#if UNITY_IOS && !UNITY_EDITOR
            ARSession session = Object.FindAnyObjectByType<ARSession>();
            if (session == null || session.subsystem is not UnityEngine.XR.ARKit.ARKitSessionSubsystem subsystem)
            {
                return false;
            }

            if (!UnityEngine.XR.ARKit.ARKitSessionSubsystem.coachingOverlaySupported)
            {
                return true;
            }

            subsystem.requestedCoachingGoal = UnityEngine.XR.ARKit.ARCoachingGoal.VerticalPlane;
            subsystem.coachingActivatesAutomatically = true;
            return true;
#else
            return true;
#endif
        }
    }
}
