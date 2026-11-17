using System;

namespace Cinnarios.Placement
{
    /// <summary>
    /// Keeps track of the placement stage and allows only sensible moves between stages:
    /// Scanning -> Aiming, Aiming -> Scanning, Aiming -> Placed, Placed -> Scanning.
    /// Anything else (for example Scanning -> Placed) is refused, so the app can never place
    /// a painting without a wall.
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
                    return to == PlacementState.Scanning || to == PlacementState.Placed;
                case PlacementState.Placed:
                    return to == PlacementState.Scanning;
                default:
                    return false;
            }
        }
    }
}
