using NUnit.Framework;
using UnityEngine;

namespace Capstone.Placement.Tests
{
    public class PoseSmootherTests
    {
        [Test]
        public void TheFirstStepSnapsToTheTarget()
        {
            var smoother = new PoseSmoother();
            smoother.Step(new Vector3(1f, 2f, 3f), Quaternion.identity, 0.016f);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), smoother.Position);
        }

        [Test]
        public void ASmallMoveIsEasedNotJumped()
        {
            var smoother = new PoseSmoother();
            smoother.SnapTo(Vector3.zero, Quaternion.identity);
            Vector3 after = smoother.Step(new Vector3(0.1f, 0f, 0f), Quaternion.identity, 0.016f);
            Assert.Greater(after.x, 0f);
            Assert.Less(after.x, 0.1f);
        }

        [Test]
        public void ALargeJumpSnapsSoTheWorkNeverSlidesAcrossTheRoom()
        {
            var smoother = new PoseSmoother();
            smoother.SnapTo(Vector3.zero, Quaternion.identity);
            smoother.Step(new Vector3(2f, 0f, 0f), Quaternion.identity, 0.016f);
            Assert.AreEqual(2f, smoother.Position.x, 1e-5f);
        }

        [Test]
        public void TheBlendIsTheSameAtThirtyAndSixtyFramesPerSecond()
        {
            float oneThirtieth = PoseSmoother.Blend(14f, 1f / 30f);
            float twoSixtieths = 1f - (1f - PoseSmoother.Blend(14f, 1f / 60f)) * (1f - PoseSmoother.Blend(14f, 1f / 60f));
            Assert.AreEqual(oneThirtieth, twoSixtieths, 1e-5f);
        }

        [Test]
        public void ItArrivesAfterEnoughSteps()
        {
            var smoother = new PoseSmoother();
            smoother.SnapTo(Vector3.zero, Quaternion.identity);
            Vector3 target = new Vector3(0f, 0f, 0.06f);
            Quaternion turn = Quaternion.Euler(0f, 10f, 0f);
            for (int i = 0; i < 120; i++)
                smoother.Step(target, turn, 1f / 60f);
            Assert.IsTrue(smoother.Arrived(target, turn));
        }

        [Test]
        public void NoTimePassedMeansNoMovement()
        {
            Assert.AreEqual(0f, PoseSmoother.Blend(14f, 0f));
        }
    }
}
