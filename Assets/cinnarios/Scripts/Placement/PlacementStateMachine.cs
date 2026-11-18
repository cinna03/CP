using System;

namespace Cinnarios.Placement
{
    /// <summary>
    /// Keeps track of the placement stage and allows only these moves:
    /// Scanning -> Aiming and back, Aiming -> Positioning (tap), Positioning -> Locked (Lock),
    /// Locked -> Positioning (Move), and Aiming / Positioning / Locked -> Scanning (Scan again).
    /// Anything else, such as Aiming -> Locked or Scanning -> Positioning, is refused, so a
    /// painting can never be locked without first being hung and positioned.
    /// </summary>
    public sealed class PlacementStateMachine
    {
        public PlacementState Current { get; private set; } = PlacementState.Scanning;

        /// <summary>Raised after every successful change, with the previous and new stage.</summary>
        public event Action<PlacementState, PlacementState> Changed;

        public bool TryTransition(PlacementState next)
        {
            if (!IsAllowed(Current, next))
            {
                return false;
            }

            PlacementState previous = Current;
            Current = next;
            Changed?.Invoke(previous, next);
            return true;
        }

        private static bool IsAllowed(PlacementState from, PlacementState to)
        {
            switch (from)
            {
                case PlacementState.Scanning:
                    return to == PlacementState.Aiming;
                case PlacementState.Aiming:
                    return to == PlacementState.Scanning || to == PlacementState.Positioning;
                case PlacementState.Positioning:
                    return to == PlacementState.Locked || to == PlacementState.Scanning;
                case PlacementState.Locked:
                    return to == PlacementState.Positioning || to == PlacementState.Scanning;
                default:
                    return false;
            }
        }
    }
}
