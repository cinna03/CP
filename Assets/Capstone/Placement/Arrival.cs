using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Capstone.Placement
{
    /// <summary>
    /// The first-visit door. A white gallery: the work is the event, and the type and blue control stay quiet around it.
    /// Begin marks the visit, then the opening film starts.
    /// </summary>
    public sealed class Arrival : MonoBehaviour
    {
        const string SeenKey = "capstone-arrival-gallery";
        static readonly Color Gallery = Color.white;
        static readonly Color Ink = new Color(0.114f, 0.114f, 0.122f, 1f);
        static readonly Color Slate = new Color(0.439f, 0.439f, 0.439f, 1f);
        static readonly Color PricingBlue = new Color(0f, 0.443f, 0.890f, 1f);

        static Sprite pill;
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

            Image paper = CreateImage(canvasObject.transform, "Gallery", Gallery);
            Stretch(paper.rectTransform);

            Text kicker = CreateText(canvasObject.transform, typeface, "Reclining figure", 42, Ink, FontStyle.Bold);
            AnchorTop(kicker.rectTransform, 168f, 64f);

            Text claim = CreateText(canvasObject.transform, typeface, "On your wall.", 76, Ink, FontStyle.Bold);
            AnchorTop(claim.rectTransform, 248f, 96f);

            Text line = CreateText(canvasObject.transform, typeface, "At the size it was made.", 36, Slate, FontStyle.Normal);
            AnchorTop(line.rectTransform, 352f, 52f);

            Texture2D picture = Resources.Load<Texture2D>("TrialPieces/reclining");
            if (picture != null)
            {
                GameObject stage = new GameObject("Work", typeof(RectTransform));
                stage.transform.SetParent(canvasObject.transform, false);
                RawImage raw = stage.AddComponent<RawImage>();
                raw.texture = picture;
                raw.raycastTarget = false;
                float aspect = picture.width / (float)Mathf.Max(1, picture.height);
                float width = 920f;
                float height = width / aspect;
                if (height > 860f)
                {
                    height = 860f;
                    width = height * aspect;
                }

                RectTransform stageRect = stage.GetComponent<RectTransform>();
                stageRect.anchorMin = new Vector2(0.5f, 0.5f);
                stageRect.anchorMax = new Vector2(0.5f, 0.5f);
                stageRect.pivot = new Vector2(0.5f, 0.5f);
                stageRect.sizeDelta = new Vector2(width, height);
                stageRect.anchoredPosition = new Vector2(0f, -40f);
            }

            Image buttonImage = CreateImage(canvasObject.transform, "Begin", PricingBlue);
            buttonImage.sprite = PillSprite();
            buttonImage.type = Image.Type.Sliced;
            RectTransform buttonRect = buttonImage.rectTransform;
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.sizeDelta = new Vector2(280f, 88f);
            buttonRect.anchoredPosition = new Vector2(0f, 96f);
            Button button = buttonImage.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            ColorBlock colors = button.colors;
            colors.fadeDuration = 0.12f;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.95f, 1f, 1f);
            colors.pressedColor = new Color(0.82f, 0.88f, 0.96f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            button.onClick.AddListener(Begin);
            Text label = CreateText(buttonImage.transform, typeface, "Begin", 32, Color.white, FontStyle.Normal);
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
            const float duration = 0.4f;
            while (time < duration)
            {
                if (fade == null)
                    yield break;
                time += Time.unscaledDeltaTime;
                fade.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / duration));
                yield return null;
            }

            if (fade != null)
                fade.alpha = 1f;
        }

        static Sprite PillSprite()
        {
            if (pill != null)
                return pill;

            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            float radius = (size * 0.5f) - 0.5f;
            Vector2 center = new Vector2(radius, radius);
            Color clear = new Color(1f, 1f, 1f, 0f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool inside = Vector2.Distance(new Vector2(x, y), center) <= radius;
                    texture.SetPixel(x, y, inside ? Color.white : clear);
                }
            }

            texture.Apply();
            pill = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(30f, 30f, 30f, 30f));
            return pill;
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

        static void AnchorTop(RectTransform rect, float fromTop, float height)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(920f, height);
            rect.anchoredPosition = new Vector2(0f, -fromTop);
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
