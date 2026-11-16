using NUnit.Framework;
using UnityEngine;

namespace Capstone.Placement.Tests
{
    public class RoomLightTests
    {
        [Test]
        public void NoEstimateLeavesTheWorkUntinted()
        {
            Color tint = RoomLight.TintFor(null, null);
            Assert.AreEqual(1f, tint.r, 0.001f);
            Assert.AreEqual(1f, tint.g, 0.001f);
            Assert.AreEqual(1f, tint.b, 0.001f);
        }

        [Test]
        public void ADarkRoomIsClampedSoTheWorkStaysReadable()
        {
            Color tint = RoomLight.TintFor(0.05f, null);
            Assert.AreEqual(RoomLight.MinBrightness, tint.g, 0.001f);
        }

        [Test]
        public void ABrightRoomIsClamped()
        {
            Color tint = RoomLight.TintFor(5f, null);
            Assert.AreEqual(RoomLight.MaxBrightness, tint.g, 0.001f);
        }

        [Test]
        public void WarmLightTintsTowardsRed()
        {
            Color tint = RoomLight.TintFor(1f, 2700f);
            Assert.Greater(tint.r, tint.b);
        }
    }
}
