using UnityEngine;

namespace Capstone.Placement
{
    /// <summary>
    /// The pixel rectangle a placed work covers on a saved camera picture.
    /// </summary>
    public readonly struct ScreenSpan
    {
        public ScreenSpan(float xMin, float yMin, float width, float height)
        {
            XMin = xMin;
            YMin = yMin;
            Width = width;
            Height = height;
        }

        public float XMin { get; }

        public float YMin { get; }

        public float Width { get; }

        public float Height { get; }
    }

    /// <summary>
    /// Turns a wall pose into the four corners of the artwork, then into one screen rectangle.
    /// </summary>
    public static class FrozenWall
    {
        public static void Corners(WallPose pose, out Vector3 bottomLeft, out Vector3 bottomRight, out Vector3 topLeft, out Vector3 topRight)
        {
            Vector3 right = pose.Rotation * Vector3.right * (pose.Scale.x * 0.5f);
            Vector3 up = pose.Rotation * Vector3.up * (pose.Scale.y * 0.5f);
            Vector3 center = pose.Position;
            bottomLeft = center - right - up;
            bottomRight = center + right - up;
            topLeft = center - right + up;
            topRight = center + right + up;
        }

        public static ScreenSpan Span(Vector2 first, Vector2 second, Vector2 third, Vector2 fourth)
        {
            float xMin = Mathf.Min(Mathf.Min(first.x, second.x), Mathf.Min(third.x, fourth.x));
            float xMax = Mathf.Max(Mathf.Max(first.x, second.x), Mathf.Max(third.x, fourth.x));
            float yMin = Mathf.Min(Mathf.Min(first.y, second.y), Mathf.Min(third.y, fourth.y));
            float yMax = Mathf.Max(Mathf.Max(first.y, second.y), Mathf.Max(third.y, fourth.y));
            return new ScreenSpan(xMin, yMin, Mathf.Max(1f, xMax - xMin), Mathf.Max(1f, yMax - yMin));
        }
    }
}
