using UnityEngine;

namespace Capstone.App.Core
{
    /// <summary>
    /// A damped spring described the way Apple's design guidance does: a damping ratio
    /// (1 = critically damped, no overshoot; below 1 = bounce) and a response in seconds
    /// (how quickly it reaches the target; not a fixed duration).
    /// Mass is 1, stiffness = (2π / response)², damping = 4π·ratio / response.
    /// The spring keeps its velocity, so a new target mid-motion continues smoothly
    /// from the current value instead of restarting: the motion is interruptible.
    /// Integrated with semi-implicit Euler in fixed sub-steps for stability at any frame rate.
    /// </summary>
    public struct Spring
    {
        public const float DefaultResponse = 0.4f;
        const float MaxStep = 1f / 240f;

        public float Value;
        public float Velocity;
        public float Target;
        public readonly float DampingRatio;
        public readonly float Response;

        public Spring(float value, float dampingRatio = 1f, float response = DefaultResponse)
        {
            Value = value;
            Target = value;
            Velocity = 0f;
            DampingRatio = Mathf.Max(0.01f, dampingRatio);
            Response = Mathf.Max(0.01f, response);
        }

        public float Stiffness => Mathf.Pow(2f * Mathf.PI / Response, 2f);

        public float Damping => 4f * Mathf.PI * DampingRatio / Response;

        /// <summary>True once the value is within <paramref name="tolerance"/> of the target and nearly still.</summary>
        public bool Settled(float tolerance = 0.0005f)
        {
            return Mathf.Abs(Target - Value) < tolerance && Mathf.Abs(Velocity) < tolerance * 10f;
        }

        /// <summary>Advances the spring by <paramref name="deltaTime"/> seconds and returns the new value.</summary>
        public float Step(float deltaTime)
        {
            float remaining = Mathf.Max(0f, deltaTime);
            while (remaining > 0f)
            {
                float dt = Mathf.Min(MaxStep, remaining);
                float force = -Stiffness * (Value - Target) - Damping * Velocity;
                Velocity += force * dt;
                Value += Velocity * dt;
                remaining -= dt;
            }

            return Value;
        }

        /// <summary>
        /// Where a flick will come to rest, using Apple's projection with a deceleration rate
        /// (0.998 feels like scrolling). Velocity is in units per second.
        /// </summary>
        public static float Project(float velocity, float decelerationRate = 0.998f)
        {
            return velocity / 1000f * decelerationRate / (1f - decelerationRate);
        }
    }
}
