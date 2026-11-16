using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Capstone.Placement
{
    /// <summary>
    /// The still door of a first visit. It plays once, before the opening film and before any guide.
    /// Begin marks the visit, then the opening starts.
    /// </summary>
    public sealed class Arrival : MonoBehaviour
    {
        const string SeenKey = "capstone-first-visit";
        static readonly Color Paper = new Color(0.957f, 0.941f, 0.910f, 1f);
        static readonly Color Ink = new Color(0.102f, 0.090f, 0.078f, 1f);
        static readonly Color Muted = new Color(0.420f, 0.384f, 0.337f, 1f);

        Action begin;

        public static bool AlreadySeen()
        {
            return PlayerPrefs.GetInt(SeenKey, 0) == 1;
        }

        public static void Play(Font typeface, Action onBegin)
        {
            GameObject host = new GameObject("Arrival");
            Arrival arrival = host.AddComponent<Arrival>();
            arrival.begin = onBegin;
            arrival.Build(typeface);
        }

        void Build(Font typeface)
        {
            GameObject canvasObject = new GameObject("Arrival canvas");
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            CanvasGroup fade = canvasObject.AddComponent<CanvasGroup>();
            fade.alpha = 0f;

            Image paper = CreateImage(canvasObject.transform, "Paper", Paper);
            paper.rectTransform.anchorMin = Vector2.zero;
            paper.rectTransform.anchorMax = Vector2.one;
            paper.rectTransform.offsetMin = Vector2.zero;
            paper.rectTransform.offsetMax = Vector2.zero;

            Text eyebrow = CreateText(canvasObject.transform, typeface, "First visit", 28, Muted, FontStyle.Normal);
            Place(eyebrow.rectTransform, new Vector2(0f, 280f), new Vector2(880f, 48f));

            Text title = CreateText(canvasObject.transform, typeface, "One work,\non your wall.", 84, Ink, FontStyle.Normal);
            title.lineSpacing = 0.85f;
            Place(title.rectTransform, new Vector2(0f, 40f), new Vector2(880f, 280f));

            Text line = CreateText(
                canvasObject.transform,
                typeface,
                "Its size is already stored. You place it, ask one thing, then decide.",
                34,
                Muted,
                FontStyle.Normal);
            Place(line.rectTransform, new Vector2(0f, -280f), new Vector2(820f, 160f));

            Image buttonImage = CreateImage(canvasObject.transform, "Begin", Ink);
            RectTransform buttonRect = buttonImage.rectTransform;
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.sizeDelta = new Vector2(880f, 120f);
            buttonRect.anchoredPosition = new Vector2(0f, 96f);
            Button button = buttonImage.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(Begin);
            Text label = CreateText(buttonImage.transform, typeface, "Begin", 36, Color.white, FontStyle.Normal);
            Stretch(label.rectTransform);

            StartCoroutine(Rise(fade));
        }

        void Begin()
        {
            PlayerPrefs.SetInt(SeenKey, 1);
            PlayerPrefs.Save();
            Action done = begin;
            Destroy(gameObject);
            if (done != null)
                done();
        }

        static IEnumerator Rise(CanvasGroup fade)
        {
            float time = 0f;
            const float duration = 0.45f;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                fade.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / duration));
                yield return null;
            }

            fade.alpha = 1f;
        }

        static Image CreateImage(Transform parent, string name, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        static Text CreateText(Transform parent, Font typeface, string message, int size, Color color, FontStyle style)
        {
            GameObject textObject = new GameObject(message, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.font = typeface;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.text = message;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
