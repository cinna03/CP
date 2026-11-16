using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Capstone.Placement
{
    /// <summary>
    /// Reads the room's light from the live camera (ARKit/ARCore light estimation) and tints every
    /// registered painting to match, so a work in a dim or warm room looks dim or warm too.
    /// The estimate is smoothed with a frame-rate independent exponential blend so it never flickers.
    /// </summary>
    public sealed class RoomLight : MonoBehaviour
    {
        public const float MinBrightness = 0.55f;
        public const float MaxBrightness = 1.1f;
        const float Sharpness = 3f;
        // How strongly the room's colour temperature tints the work (0 = none, 1 = full).
        const float WarmthStrength = 0.45f;

        static readonly HashSet<Renderer> lit = new HashSet<Renderer>();
        static readonly List<Renderer> gone = new List<Renderer>();

        /// <summary>The colour every painting is multiplied by this frame.</summary>
        public static Color Tint { get; private set; } = Color.white;

        ARCameraManager cameraManager;
        Color target = Color.white;

        public static RoomLight Attach(GameObject host)
        {
            RoomLight light = host.GetComponent<RoomLight>();
            if (light == null)
                light = host.AddComponent<RoomLight>();
            return light;
        }

        public static void Register(Renderer renderer)
        {
            if (renderer != null)
                lit.Add(renderer);
        }

        public static void Unregister(Renderer renderer)
        {
            lit.Remove(renderer);
        }

        /// <summary>Turns an estimate into a tint: clamped brightness times a softened colour temperature.</summary>
        public static Color TintFor(float? brightness, float? kelvin)
        {
            float level = Mathf.Clamp(brightness ?? 1f, MinBrightness, MaxBrightness);
            Color warmth = Color.white;
            if (kelvin.HasValue && kelvin.Value > 1000f)
            {
                Color raw = Mathf.CorrelatedColorTemperatureToRGB(kelvin.Value);
                float peak = Mathf.Max(raw.r, Mathf.Max(raw.g, raw.b));
                if (peak > 0f)
                    raw = new Color(raw.r / peak, raw.g / peak, raw.b / peak, 1f);
                warmth = Color.Lerp(Color.white, raw, WarmthStrength);
            }

            return new Color(warmth.r * level, warmth.g * level, warmth.b * level, 1f);
        }

        void OnEnable()
        {
            cameraManager = Object.FindAnyObjectByType<ARCameraManager>();
            if (cameraManager == null)
                return;
            cameraManager.requestedLightEstimation |= LightEstimation.AmbientIntensity
                | LightEstimation.AmbientColor;
            cameraManager.frameReceived += OnFrame;
        }

        void OnDisable()
        {
            if (cameraManager != null)
                cameraManager.frameReceived -= OnFrame;
            Tint = Color.white;
        }

        void OnFrame(ARCameraFrameEventArgs frame)
        {
            ARLightEstimationData estimate = frame.lightEstimation;
            target = TintFor(estimate.averageBrightness, estimate.averageColorTemperature);
        }

        void LateUpdate()
        {
            float blend = 1f - Mathf.Exp(-Sharpness * Time.unscaledDeltaTime);
            Tint = Color.Lerp(Tint, target, blend);

            gone.Clear();
            foreach (Renderer renderer in lit)
            {
                if (renderer == null)
                {
                    gone.Add(renderer);
                    continue;
                }

                Material material = renderer.sharedMaterial;
                if (material != null)
                    material.color = Tint;
            }

            for (int i = 0; i < gone.Count; i++)
                lit.Remove(gone[i]);
        }
    }
}
