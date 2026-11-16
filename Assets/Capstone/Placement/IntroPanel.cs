using System;
using UnityEngine;
using UnityEngine.UI;

namespace Capstone.Placement
{
    /// <summary>
    /// The opening panel. Edit the picture, words, type, and resting layout on
    /// Resources/Home/Intro. This script only plays the cover and reveals Begin.
    /// </summary>
    public sealed class IntroPanel : MonoBehaviour
    {
        [SerializeField] Image cover;
        [SerializeField] RawImage art;
        [SerializeField] Text headline;
        [SerializeField] Text lead;
        [SerializeField] Text beginLabel;
        [SerializeField] Button beginButton;
        [SerializeField] Color dayField = Color.white;
        [SerializeField] Color nightField = Color.black;
        [SerializeField] float pictureAspect = 736f / 981f;
        [SerializeField] float morphSeconds = 1.6f;
        [SerializeField] float wordsDelay = 0.7f;
        [SerializeField] float wordsSeconds = 0.9f;
        [SerializeField] float wordsRise = 12f;

        Action onBegin;
        bool morphing;
        bool released;
        float clock;
        Vector2 fullPosition;
        Vector2 fullSize;
        Vector2 restPosition;
        Vector2 restSize;
        Text[] lines = new Text[0];
        Vector2[] lineRest = new Vector2[0];

        public static IntroPanel BuildFallback(Transform parent, Font displayFont, Font bodyFont)
        {
            GameObject root = new GameObject("Intro", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            Stretch(root.GetComponent<RectTransform>());
            IntroPanel panel = root.AddComponent<IntroPanel>();
            panel.cover = FullImage(root.transform, "Cover", Color.white);
            panel.art = Picture(root.transform, "Work");
            panel.headline = Line(root.transform, "CINNARIOS", displayFont, 84, new Vector2(0f, 571f), new Vector2(980f, 110f));
            panel.lead = Line(root.transform, "AR visualization of a work on your wall, at its real size.", bodyFont, 28, new Vector2(0f, -398f), new Vector2(860f, 100f));
            panel.beginLabel = Line(root.transform, "Begin", displayFont, 36, new Vector2(0f, -516f), new Vector2(360f, 72f));
            panel.beginButton = panel.beginLabel.gameObject.AddComponent<Button>();
            panel.beginButton.targetGraphic = panel.beginLabel;
            panel.beginButton.transition = Selectable.Transition.None;
            return panel;
        }

        public void Open(Action begin, bool night, Font displayFont, Font bodyFont)
        {
            onBegin = begin;
            if (art != null && art.texture == null)
                art.texture = Resources.Load<Texture2D>("Home/flowers");
            if (headline != null && headline.font == null)
                headline.font = displayFont;
            if (lead != null && lead.font == null)
                lead.font = bodyFont;
            if (beginLabel != null && beginLabel.font == null)
                beginLabel.font = displayFont;

            ApplyField(night);
            lines = new[] { headline, lead, beginLabel };
            lineRest = new Vector2[lines.Length];
            if (art != null)
            {
                restPosition = art.rectTransform.anchoredPosition;
                restSize = art.rectTransform.sizeDelta;
            }

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == null)
                    continue;
                lineRest[i] = lines[i].rectTransform.anchoredPosition;
                SetAlpha(lines[i], 0f);
            }

            if (beginButton != null)
            {
                beginButton.onClick.RemoveListener(Begin);
                beginButton.onClick.AddListener(Begin);
                beginButton.interactable = false;
                beginButton.transition = Selectable.Transition.None;
            }

            if (beginLabel != null)
                beginLabel.raycastTarget = false;
            if (cover != null)
            {
                Button tap = cover.GetComponent<Button>();
                if (tap == null)
                    tap = cover.gameObject.AddComponent<Button>();
                tap.targetGraphic = cover;
                tap.transition = Selectable.Transition.None;
                tap.onClick.RemoveListener(Release);
                tap.onClick.AddListener(Release);
            }

            clock = 0f;
            released = false;
            morphing = true;
            PoseCover();
        }

        public void Tick()
        {
            if (!morphing || art == null || !gameObject.activeInHierarchy)
                return;

            if (!released)
            {
                PoseCover();
                return;
            }

            float delta = Time.unscaledDeltaTime;
            if (delta > 0.2f)
                return;

            clock += Mathf.Min(delta, 0.033f);
            float move = Smooth(clock / Mathf.Max(0.05f, morphSeconds));
            PoseCover();
            art.rectTransform.anchoredPosition = Vector2.LerpUnclamped(fullPosition, restPosition, move);
            art.rectTransform.sizeDelta = Vector2.LerpUnclamped(fullSize, restSize, move);
            float word = Smooth((clock - wordsDelay) / Mathf.Max(0.05f, wordsSeconds));
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == null)
                    continue;
                Vector2 rest = lineRest[i];
                lines[i].rectTransform.anchoredPosition = Vector2.LerpUnclamped(rest + new Vector2(0f, -wordsRise), rest, word);
                SetAlpha(lines[i], word);
            }

            if (clock < morphSeconds)
                return;

            art.rectTransform.anchoredPosition = restPosition;
            art.rectTransform.sizeDelta = restSize;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == null)
                    continue;
                lines[i].rectTransform.anchoredPosition = lineRest[i];
                SetAlpha(lines[i], 1f);
            }

            if (beginLabel != null)
                beginLabel.raycastTarget = true;
            if (beginButton != null)
                beginButton.interactable = true;
            morphing = false;
        }

        public void ApplyField(bool night)
        {
            if (cover != null)
                cover.color = night ? nightField : dayField;
        }

        public void Close()
        {
            morphing = false;
            gameObject.SetActive(false);
        }

        void Begin()
        {
            if (onBegin != null)
                onBegin();
        }

        void Release()
        {
            if (!morphing || released || art == null)
                return;

            PoseCover();
            clock = 0f;
            released = true;
        }

        void PoseCover()
        {
            if (art == null)
                return;

            Rect page = ((RectTransform)transform).rect;
            float pageW = page.width > 2f ? page.width : 1080f;
            float pageH = page.height > 2f ? page.height : 1920f;
            float aspect = pictureAspect;
            if (art.texture != null && art.texture.height > 0)
                aspect = art.texture.width / (float)art.texture.height;
            if (aspect < 0.01f)
                aspect = 736f / 981f;

            float coverW = pageH * aspect;
            float coverH = pageH;
            if (coverW < pageW)
            {
                coverW = pageW;
                coverH = pageW / aspect;
            }

            fullSize = new Vector2(coverW, coverH);
            fullPosition = Vector2.zero;
            if (!released)
            {
                art.rectTransform.anchoredPosition = fullPosition;
                art.rectTransform.sizeDelta = fullSize;
            }
        }

        static float Smooth(float amount)
        {
            float t = Mathf.Clamp01(amount);
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }

        static void SetAlpha(Text line, float alpha)
        {
            Color color = line.color;
            color.a = alpha;
            line.color = color;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        static Image FullImage(Transform parent, string name, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            Stretch(image.rectTransform);
            return image;
        }

        static RawImage Picture(Transform parent, string name)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform));
            imageObject.transform.SetParent(parent, false);
            RawImage image = imageObject.AddComponent<RawImage>();
            image.texture = Resources.Load<Texture2D>("Home/flowers");
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 80f);
            rect.sizeDelta = new Vector2(600f, 800f);
            return image;
        }

        static Text Line(Transform parent, string message, Font font, int size, Vector2 position, Vector2 box)
        {
            GameObject textObject = new GameObject(message, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.878f, 0.082f, 0.384f, 1f);
            text.text = message;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = box;
            return text;
        }
    }
}
