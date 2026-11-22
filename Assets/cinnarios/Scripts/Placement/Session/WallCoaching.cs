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
        private static bool _guideVisible;

        /// <summary>
        /// Shows Apple's wall guide until a vertical plane exists. Automatic coaching stays
        /// hidden when the room is already tracking but the wall itself has not been found,
        /// which is what happened on a plain painted wall.
        /// </summary>
        public static void GuideUntilAWall(bool wallFound)
        {
#if UNITY_IOS && !UNITY_EDITOR
            ARSession session = Object.FindAnyObjectByType<ARSession>();
            if (session == null || session.subsystem is not UnityEngine.XR.ARKit.ARKitSessionSubsystem subsystem)
            {
                return;
            }

            if (!UnityEngine.XR.ARKit.ARKitSessionSubsystem.coachingOverlaySupported)
            {
                return;
            }

            if (wallFound)
            {
                if (_guideVisible)
                {
                    subsystem.SetCoachingActive(false, UnityEngine.XR.ARKit.ARCoachingOverlayTransition.Animated);
                    _guideVisible = false;
                }

                return;
            }

            if (_guideVisible)
            {
                return;
            }

            subsystem.requestedCoachingGoal = UnityEngine.XR.ARKit.ARCoachingGoal.VerticalPlane;
            subsystem.SetCoachingActive(true, UnityEngine.XR.ARKit.ARCoachingOverlayTransition.Animated);
            _guideVisible = true;
#else
            _ = wallFound;
#endif
        }
    }
}
