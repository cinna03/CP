using Capstone.App.Core;
using NUnit.Framework;

namespace Capstone.App.Tests
{
    public class SpringTests
    {
        [Test]
        public void ACriticallyDampedSpringReachesTheTargetWithoutOvershoot()
        {
            var spring = new Spring(0f, 1f, 0.4f) { Target = 1f };
            float highest = 0f;
            for (int i = 0; i < 120; i++)
            {
                spring.Step(1f / 60f);
                if (spring.Value > highest)
                    highest = spring.Value;
            }

            Assert.LessOrEqual(highest, 1.0005f);
            Assert.IsTrue(spring.Settled());
        }

        [Test]
        public void ABouncySpringOvershoots()
        {
            var spring = new Spring(0f, 0.6f, 0.4f) { Target = 1f };
            float highest = 0f;
            for (int i = 0; i < 120; i++)
            {
                spring.Step(1f / 60f);
                if (spring.Value > highest)
                    highest = spring.Value;
            }

            Assert.Greater(highest, 1.01f);
        }

        [Test]
        public void RetargetingMidMotionKeepsTheCurrentValueAndVelocity()
        {
            var spring = new Spring(0f) { Target = 1f };
            spring.Step(0.1f);
            float value = spring.Value;
            float velocity = spring.Velocity;
            spring.Target = -1f;
            Assert.AreEqual(value, spring.Value);
            Assert.AreEqual(velocity, spring.Velocity);
        }

        [Test]
        public void TheResultDoesNotDependOnFrameRate()
        {
            var thirty = new Spring(0f) { Target = 1f };
            var oneTwenty = new Spring(0f) { Target = 1f };
            for (int i = 0; i < 6; i++)
                thirty.Step(1f / 30f);
            for (int i = 0; i < 24; i++)
                oneTwenty.Step(1f / 120f);
            Assert.AreEqual(thirty.Value, oneTwenty.Value, 0.01f);
        }

        [Test]
        public void AFasterFlickProjectsFurther()
        {
            Assert.Greater(Spring.Project(2000f), Spring.Project(500f));
            Assert.AreEqual(0f, Spring.Project(0f));
        }
    }
}
