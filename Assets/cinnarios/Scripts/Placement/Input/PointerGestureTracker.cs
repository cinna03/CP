using System;
using UnityEngine;
using InputPointer = UnityEngine.InputSystem.Pointer;

namespace Cinnarios.Placement.Input
{
    /// <summary>
    /// Tells taps and drags apart. A press that moves less than the threshold before release
    /// is a tap; one that moves further is a drag. A press that starts on a button is ignored
    /// until it is released, so on-screen buttons never count as a wall tap or drag.
    /// Works with a finger on the phone and a mouse in the Editor.
    /// </summary>
    public sealed class PointerGestureTracker
    {
        private readonly float _dragThresholdPixels;
        private bool _pressed;
        private bool _dragging;
        private bool _startedOnUi;
        private Vector2 _pressStart;

        /// <summary>Current pointer position in screen pixels.</summary>
        public Vector2 Position { get; private set; }

        public PointerGestureTracker(float dragThresholdPixels)
        {
            _dragThresholdPixels = Mathf.Max(1f, dragThresholdPixels);
        }

        /// <param name="isOverUi">Answers whether a screen position is on a button or panel.</param>
        public PointerGesture Update(Func<Vector2, bool> isOverUi)
        {
            InputPointer pointer = InputPointer.current;
            if (pointer == null)
            {
                return PointerGesture.None;
            }

            Position = pointer.position.ReadValue();

            if (pointer.press.wasPressedThisFrame)
            {
                _pressed = true;
                _dragging = false;
                _pressStart = Position;
                _startedOnUi = isOverUi != null && isOverUi(Position);
                return PointerGesture.None;
            }

            if (!_pressed)
            {
                return PointerGesture.None;
            }

            if (pointer.press.wasReleasedThisFrame)
            {
                _pressed = false;
                bool wasDragging = _dragging;
                _dragging = false;

                if (_startedOnUi)
                {
                    return PointerGesture.None;
                }

                return wasDragging ? PointerGesture.DragEnded : PointerGesture.Tap;
            }

            if (_startedOnUi)
            {
                return PointerGesture.None;
            }

            if (!_dragging && (Position - _pressStart).magnitude >= _dragThresholdPixels)
            {
                _dragging = true;
                return PointerGesture.DragStarted;
            }

            return _dragging ? PointerGesture.Dragging : PointerGesture.None;
        }

        /// <summary>Forgets any press in progress, for example after Scan again.</summary>
        public void Reset()
        {
            _pressed = false;
            _dragging = false;
            _startedOnUi = false;
        }
    }
}
