using UnityEngine;

namespace Cinnarios.Placement
{
    /// <summary>
    /// Answers one question: "is there a wall at this point on the screen, and where?"
    /// Placement logic depends only on this, so the AR implementation can be swapped for a
    /// simulated or test one without touching the placement code.
    /// </summary>
    public interface IWallHitProvider
    {
        /// <param name="wallPose">The point on the wall; its up direction is the wall's normal, pointing into the room.</param>
        bool TryGetWallHit(Vector2 screenPoint, out Pose wallPose);
    }
}
