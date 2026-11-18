namespace Cinnarios.Placement
{
    /// <summary>The stages of hanging a painting on a wall in one session.</summary>
    public enum PlacementState
    {
        /// <summary>No wall under the centre of the screen yet. The painting is hidden.</summary>
        Scanning,

        /// <summary>A wall is in view. The painting previews where it would hang and follows the aim.</summary>
        Aiming,

        /// <summary>
        /// Hung on the wall by a tap. It stays fixed in the room; a drag slides it along that
        /// same wall. Not anchored yet: the person is still choosing the spot.
        /// </summary>
        Positioning,

        /// <summary>Locked in place with an AR anchor. Wall search is off. Only Move or Scan again change it.</summary>
        Locked
    }
}
