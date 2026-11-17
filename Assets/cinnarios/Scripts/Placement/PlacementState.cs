namespace Cinnarios.Placement
{
    /// <summary>The stages of hanging a painting on a wall.</summary>
    public enum PlacementState
    {
        /// <summary>No wall under the centre of the screen yet. The painting is hidden.</summary>
        Scanning,

        /// <summary>A wall is in view. The painting previews where it would hang and follows the aim.</summary>
        Aiming,

        /// <summary>The painting is anchored to the wall and no longer follows the camera.</summary>
        Placed
    }
}
