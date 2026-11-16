using System;
using System.Collections.Generic;

namespace Capstone.Placement
{
    /// <summary>The stages of one wall session, from opening the camera to a work on the wall.</summary>
    public enum WallSessionState
    {
        /// <summary>The wall session is not looking for a surface.</summary>
        Idle,
        /// <summary>The camera is open and the session is looking for a vertical wall.</summary>
        Scanning,
        /// <summary>A wall candidate was seen but tracking is not yet steady enough to place on.</summary>
        Settling,
        /// <summary>A work is on the wall at its stored size.</summary>
        Placed
    }

    /// <summary>
    /// A small finite state machine for the AR wall session.
    /// Only the transitions in the table below are allowed, so the session can never jump,
    /// for example, from Idle straight to Settling. A refused move is reported, not thrown,
    /// so a stray call cannot crash a participant's session.
    /// Pure C#, no Unity types, so every transition is covered by EditMode tests.
    /// </summary>
    public sealed class ARSessionStateMachine
    {
        static readonly Dictionary<WallSessionState, WallSessionState[]> Allowed = new Dictionary<WallSessionState, WallSessionState[]>
        {
            { WallSessionState.Idle, new[] { WallSessionState.Scanning, WallSessionState.Placed } },
            { WallSessionState.Scanning, new[] { WallSessionState.Settling, WallSessionState.Placed, WallSessionState.Idle } },
            { WallSessionState.Settling, new[] { WallSessionState.Scanning, WallSessionState.Placed, WallSessionState.Idle } },
            { WallSessionState.Placed, new[] { WallSessionState.Scanning, WallSessionState.Idle } },
        };

        int refused;

        public ARSessionStateMachine(WallSessionState start = WallSessionState.Idle)
        {
            Current = start;
        }

        public WallSessionState Current { get; private set; }

        /// <summary>How many moves were refused because the table does not allow them.</summary>
        public int RefusedMoves => refused;

        /// <summary>Raised with (from, to) after every change of state.</summary>
        public event Action<WallSessionState, WallSessionState> Changed;

        public static bool CanMove(WallSessionState from, WallSessionState to)
        {
            if (from == to)
                return true;
            return Allowed.TryGetValue(from, out WallSessionState[] targets) && Array.IndexOf(targets, to) >= 0;
        }

        /// <summary>Moves to <paramref name="next"/> if the table allows it. Staying in the same state is always allowed and raises nothing.</summary>
        public bool MoveTo(WallSessionState next)
        {
            if (next == Current)
                return true;
            if (!CanMove(Current, next))
            {
                refused++;
                return false;
            }

            WallSessionState from = Current;
            Current = next;
            Changed?.Invoke(from, next);
            return true;
        }

        /// <summary>Back to Idle from any state, for leaving the wall session.</summary>
        public void Reset()
        {
            if (Current == WallSessionState.Idle)
                return;
            WallSessionState from = Current;
            Current = WallSessionState.Idle;
            Changed?.Invoke(from, WallSessionState.Idle);
        }
    }
}
