namespace Cinnarios.Placement.Input
{
    /// <summary>What a finger (or mouse) did this frame.</summary>
    public enum PointerGesture
    {
        None,

        /// <summary>Pressed and released without moving far: a tap.</summary>
        Tap,

        /// <summary>Moved past the drag threshold this frame.</summary>
        DragStarted,

        /// <summary>Still dragging.</summary>
        Dragging,

        /// <summary>Released after a drag.</summary>
        DragEnded,

        /// <summary>Held still, once, before release. Used to open details on the painting.</summary>
        Hold
    }
}
