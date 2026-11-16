using NUnit.Framework;
using UnityEngine;

namespace Capstone.Placement.Tests
{
    public class FrozenWallTests
    {
        [Test]
        public void CornersSitHalfTheStoredSizeFromTheCentre()
        {
            WallPose pose = new WallPose(Vector3.zero, Quaternion.identity, new Vector3(0.40f, 0.50f, 1f));

            FrozenWall.Corners(pose, out Vector3 bottomLeft, out Vector3 bottomRight, out Vector3 topLeft, out Vector3 topRight);

            Assert.AreEqual(-0.20f, bottomLeft.x, 0.0001f);
            Assert.AreEqual(-0.25f, bottomLeft.y, 0.0001f);
            Assert.AreEqual(0.20f, bottomRight.x, 0.0001f);
            Assert.AreEqual(-0.25f, bottomRight.y, 0.0001f);
            Assert.AreEqual(-0.20f, topLeft.x, 0.0001f);
            Assert.AreEqual(0.25f, topLeft.y, 0.0001f);
            Assert.AreEqual(0.20f, topRight.x, 0.0001f);
            Assert.AreEqual(0.25f, topRight.y, 0.0001f);
        }

        [Test]
        public void SpanUsesTheOuterPixelsOfTheFourCorners()
        {
            ScreenSpan span = FrozenWall.Span(
                new Vector2(100f, 80f),
                new Vector2(300f, 90f),
                new Vector2(110f, 400f),
                new Vector2(280f, 390f));

            Assert.AreEqual(100f, span.XMin, 0.001f);
            Assert.AreEqual(80f, span.YMin, 0.001f);
            Assert.AreEqual(200f, span.Width, 0.001f);
            Assert.AreEqual(320f, span.Height, 0.001f);
        }
    }
}
