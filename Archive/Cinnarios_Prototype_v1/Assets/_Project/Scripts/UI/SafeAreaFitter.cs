using UnityEngine;

namespace Capstone.App.UI
{
    /// <summary>
    /// Pulls a full-screen rect inside the phone's safe area so a notch does not cover the top controls.
    /// </summary>
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        Rect last;

        void OnEnable()
        {
            Apply();
        }

        void Update()
        {
            if (Screen.safeArea != last)
                Apply();
        }

        public void Apply()
        {
            RectTransform rect = transform as RectTransform;
            if (rect == null || Screen.width <= 0 || Screen.height <= 0)
                return;

            Rect safe = Screen.safeArea;
            last = safe;
            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
