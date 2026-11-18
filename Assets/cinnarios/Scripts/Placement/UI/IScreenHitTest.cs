using UnityEngine;

namespace Cinnarios.Placement.UI
{
    /// <summary>
    /// Answers "is this screen position on a button or panel?", so presses on the interface
    /// are never treated as taps or drags on the wall.
    /// </summary>
    public interface IScreenHitTest
    {
        bool IsOverUi(Vector2 screenPosition);
    }
}
