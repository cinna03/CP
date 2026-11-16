using System.Collections;
using Capstone.App.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Capstone.App.UI
{
    /// <summary>
    /// Small, layout-safe motion for the procedural screens: fade and settle on arrival,
    /// a staggered entrance for lists, and a soft press on every button.
    /// Only alpha and scale are animated, so layout groups never fight the animation.
    /// All timing uses unscaled time, so pausing the game does not freeze the interface.
    /// </summary>
    public static class UIMotion
    {
        public const float Quick = 0.16f;
        public const float Standard = 0.28f;
        public const float StaggerStep = 0.045f;
        const int MaxStaggered = 10;

        static Runner runner;

        /// <summary>Fades the object in from 0 to 1 and settles it from 96% to full size.</summary>
        public static void Appear(GameObject target, float delay = 0f, float duration = Standard)
        {
            if (target == null || !Application.isPlaying)
                return;
            CanvasGroup group = target.GetComponent<CanvasGroup>();
            if (group == null)
                group = target.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            target.transform.localScale = Vector3.one * 0.96f;
            Host().StartCoroutine(AppearRoutine(target, group, delay, duration));
        }

        /// <summary>Appear, delayed by the object's place among its siblings, for lists and grids.</summary>
        public static void AppearInList(GameObject target)
        {
            if (target == null)
                return;
            int index = Mathf.Min(target.transform.GetSiblingIndex(), MaxStaggered);
            Appear(target, index * StaggerStep);
        }

        /// <summary>Fades the object out, then destroys it.</summary>
        public static void Dismiss(GameObject target, float duration = Quick)
        {
            if (target == null)
                return;
            if (!Application.isPlaying)
            {
                Object.Destroy(target);
                return;
            }
            CanvasGroup group = target.GetComponent<CanvasGroup>();
            if (group == null)
                group = target.AddComponent<CanvasGroup>();
            Host().StartCoroutine(DismissRoutine(target, group, duration));
        }

        static IEnumerator AppearRoutine(GameObject target, CanvasGroup group, float delay, float duration)
        {
            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);
            // Critically damped spring (no overshoot): Apple's default for UI that was not flicked.
            var scale = new Spring(0.96f, 1f, Mathf.Max(0.2f, duration)) { Target = 1f };
            float time = 0f;
            while (!scale.Settled() || time < duration)
            {
                if (target == null)
                    yield break;
                float step = Time.unscaledDeltaTime;
                time += step;
                group.alpha = EaseOutCubic(Mathf.Clamp01(time / duration));
                target.transform.localScale = Vector3.one * scale.Step(step);
                if (time > 2f)
                    break;
                yield return null;
            }
            if (target == null)
                yield break;
            group.alpha = 1f;
            target.transform.localScale = Vector3.one;
        }

        static IEnumerator DismissRoutine(GameObject target, CanvasGroup group, float duration)
        {
            float start = group.alpha;
            float time = 0f;
            while (time < duration)
            {
                if (target == null)
                    yield break;
                time += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, 0f, Mathf.Clamp01(time / duration));
                yield return null;
            }
            if (target != null)
                Object.Destroy(target);
        }

        public static float EaseOutCubic(float t)
        {
            float u = 1f - t;
            return 1f - u * u * u;
        }

        static Runner Host()
        {
            if (runner == null)
            {
                var host = new GameObject("UIMotion");
                Object.DontDestroyOnLoad(host);
                host.hideFlags = HideFlags.HideInHierarchy;
                runner = host.AddComponent<Runner>();
            }
            return runner;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install() => Host();

        /// <summary>Gives every interactable button a soft press, including buttons built after start.</summary>
        sealed class Runner : MonoBehaviour
        {
            float nextScan;

            void Update()
            {
                if (Time.unscaledTime < nextScan)
                    return;
                nextScan = Time.unscaledTime + 0.5f;
                Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude);
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i] != null && buttons[i].GetComponent<PressFeedback>() == null)
                        buttons[i].gameObject.AddComponent<PressFeedback>();
                }
            }
        }
    }

    /// <summary>
    /// Press feedback in the Apple style: the button shrinks to 97% the instant the finger
    /// touches down (not on release), and springs back from wherever it is when the finger lifts,
    /// so a quick tap that is released mid-shrink reverses smoothly. It only drives the scale while
    /// a press is starting or ending, so it never fights another script animating the same object.
    /// </summary>
    public sealed class PressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        const float Pressed = 0.97f;
        Vector3 rest;
        Spring factor = new Spring(1f, 1f, 0.25f);
        bool moving;
        Selectable selectable;

        void Awake()
        {
            selectable = GetComponent<Selectable>();
            rest = transform.localScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (selectable != null && !selectable.interactable)
                return;
            if (!moving)
                rest = transform.localScale;
            factor = new Spring(factor.Value, 1f, 0.1f) { Velocity = factor.Velocity, Target = Pressed };
            moving = true;
        }

        public void OnPointerUp(PointerEventData eventData) => Release();

        public void OnPointerExit(PointerEventData eventData) => Release();

        void Release()
        {
            if (!moving)
                return;
            factor = new Spring(factor.Value, 1f, 0.3f) { Velocity = factor.Velocity, Target = 1f };
        }

        void OnDisable()
        {
            if (moving)
                transform.localScale = rest;
            factor = new Spring(1f, 1f, 0.25f);
            moving = false;
        }

        void Update()
        {
            if (!moving)
                return;
            transform.localScale = rest * factor.Step(Time.unscaledDeltaTime);
            if (factor.Target >= 1f && factor.Settled())
            {
                transform.localScale = rest;
                moving = false;
            }
        }
    }
}
