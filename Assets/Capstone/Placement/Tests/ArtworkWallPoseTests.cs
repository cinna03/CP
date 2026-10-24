using Capstone.Catalogue;
using NUnit.Framework;
using UnityEngine;

namespace Capstone.Placement.Tests
{
    public class ArtworkWallPoseTests
    {
        [Test]
        public void FortyByFiftyCentimetresSetsTheQuadScale()
        {
            Artwork work = SampleArtwork.Create();
            WallPose pose = ArtworkWallPose.At(work, Vector3.zero, Vector3.forward, new Vector3(0f, 0f, -2f));

            Assert.AreEqual(0.40f, pose.Scale.x, 0.0001f);
            Assert.AreEqual(0.50f, pose.Scale.y, 0.0001f);
            Assert.AreEqual(1f, pose.Scale.z, 0.0001f);
        }

        [Test]
        public void FacesTheCameraWhenThePlaneNormalPointsAway()
        {
            Artwork work = SampleArtwork.Create();
            Vector3 hit = new Vector3(0f, 1.5f, 2f);
            Vector3 camera = new Vector3(0f, 1.5f, 0f);

            WallPose pose = ArtworkWallPose.At(work, hit, Vector3.forward, camera);

            Vector3 forward = pose.Rotation * Vector3.forward;
            Assert.Greater(Vector3.Dot(forward, camera - pose.Position), 0f);
        }

        [Test]
        public void SitsJustInFrontOfTheWallTowardTheCamera()
        {
            Artwork work = SampleArtwork.Create();
            Vector3 hit = new Vector3(1f, 1f, 1f);
            Vector3 camera = new Vector3(1f, 1f, 4f);

            WallPose pose = ArtworkWallPose.At(work, hit, Vector3.forward, camera);

            Vector3 expected = hit + Vector3.forward * ArtworkWallPose.SurfaceOffsetMetres;
            Assert.AreEqual(expected.x, pose.Position.x, 0.0001f);
            Assert.AreEqual(expected.y, pose.Position.y, 0.0001f);
            Assert.AreEqual(expected.z, pose.Position.z, 0.0001f);
        }

        [Test]
        public void SwapSelectsTheOtherWorkAndItsSize()
        {
            Artwork first = SampleArtwork.Create();
            Artwork second = SampleArtwork.OtherThan(first);
            Artwork back = SampleArtwork.OtherThan(second);

            Assert.AreEqual(SampleArtwork.SecondId, second.Id);
            Assert.AreEqual(first.Id, back.Id);
            Assert.AreEqual(0.80f, second.WallSizeMetres.WidthMetres, 0.0001f);
            Assert.AreEqual(0.30f, second.WallSizeMetres.HeightMetres, 0.0001f);
            Assert.AreNotEqual(first.WallSizeMetres.WidthMetres, second.WallSizeMetres.WidthMetres);
        }
    }
}
