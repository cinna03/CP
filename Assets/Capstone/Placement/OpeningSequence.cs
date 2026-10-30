using System;
using UnityEngine;
using UnityEngine.UI;

namespace Capstone.Placement
{
    /// <summary>
    /// The first screen is a blue field and one short line. A circle opens it to white,
    /// then a glass body floats until a tap starts the session.
    /// </summary>
    public sealed class OpeningSequence : MonoBehaviour
    {
        static readonly Color Paper = Color.white;
        static readonly Color Blue = new Color(0.09f, 0.38f, 0.96f, 1f);
        static readonly Color BlueDeep = new Color(0.04f, 0.22f, 0.86f, 1f);
        const float CircleSize = 150f;

        Action finished;
        Image field;
        Image circle;
        Image pillImage;
        RectTransform pill;
        Text caption;
        RectTransform orb;
        RectTransform ribbonA;
        RectTransform ribbonB;
        Image shadow;
        int phase;
        float phaseTime;
        bool done;

        public static void Play(Font typeface, Action onFinished)
        {
            GameObject host = new GameObject("Opening");
            OpeningSequence sequence = host.AddComponent<OpeningSequence>();
            sequence.finished = onFinished;
            sequence.Build(typeface);
        }

        void Build(Font typeface)
        {
            GameObject canvasObject = new GameObject("Opening canvas");
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 800;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0f;
            canvasObject.AddComponent<GraphicRaycaster>();

            Image ground = CreateImage(canvasObject.transform, "Paper", Paper, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Button screenTap = ground.gameObject.AddComponent<Button>();
            screenTap.targetGraphic = ground;
            screenTap.onClick.AddListener(Finish);

            field = CreateImage(canvasObject.transform, "Blue field", Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            field.sprite = VerticalGradient(new Color(0.22f, 0.52f, 1f, 1f), BlueDeep);
            field.raycastTarget = false;

            circle = CreateImage(
                canvasObject.transform,
                "Blue circle",
                Blue,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(CircleSize, CircleSize));
            circle.sprite = Disc(Blue, false);
            circle.raycastTarget = false;
            circle.gameObject.SetActive(false);

            shadow = CreateImage(
                canvasObject.transform,
                "Shadow",
                new Color(0.55f, 0.62f, 0.78f, 0f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -280f),
                new Vector2(420f, 90f));
            shadow.sprite = SoftEllipse();
            shadow.raycastTarget = false;

            GameObject orbObject = new GameObject("Glass body", typeof(RectTransform));
            orbObject.transform.SetParent(canvasObject.transform, false);
            orb = orbObject.GetComponent<RectTransform>();
            orb.anchorMin = new Vector2(0.5f, 0.5f);
            orb.anchorMax = new Vector2(0.5f, 0.5f);
            orb.pivot = new Vector2(0.5f, 0.5f);
            orb.sizeDelta = new Vector2(640f, 640f);
            orb.anchoredPosition = new Vector2(0f, 80f);
            orb.localScale = Vector3.one * 0.15f;

            ribbonA = Ribbon(orb, "Ribbon", 640f, 0);
            ribbonB = Ribbon(orb, "Inner ribbon", 640f, 1);
            Image glass = CreateImage(orb, "Glass", Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            glass.sprite = GlassShell();
            glass.raycastTarget = false;
            orb.gameObject.SetActive(false);

            pillImage = CreateImage(
                canvasObject.transform,
                "Pill",
                Color.white,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 36f),
                new Vector2(78f, 34f));
            pillImage.sprite = Capsule();
            pillImage.raycastTarget = false;
            pill = pillImage.rectTransform;

            caption = CreateCaption(canvasObject.transform, typeface, "See it on your wall.");
            caption.color = new Color(1f, 1f, 1f, 0f);
        }

        void Update()
        {
            if (done)
                return;

            phaseTime += Mathf.Min(Time.deltaTime, 0.033f);
            if (phase == 0)
                HoldCard();
            else if (phase == 1)
                OpenCircle();
            else if (phase == 2)
                HoldLine();
            else
                FloatBody();
        }

        void HoldCard()
        {
            float bob = Mathf.Sin(phaseTime * 2.4f) * 12f;
            pill.anchoredPosition = new Vector2(0f, 48f + bob);
            caption.color = new Color(1f, 1f, 1f, Mathf.Clamp01((phaseTime - 0.15f) / 0.3f));
            if (phaseTime < 1.45f)
                return;

            phase = 1;
            phaseTime = 0f;
            field.gameObject.SetActive(false);
            pill.gameObject.SetActive(false);
            caption.color = new Color(1f, 1f, 1f, 0f);
            circle.gameObject.SetActive(true);
            circle.rectTransform.localScale = Vector3.one * 18f;
        }

        void OpenCircle()
        {
            float t = Mathf.Clamp01(phaseTime / 0.58f);
            float scale = Mathf.Lerp(18f, 0.12f, t * t);
            circle.rectTransform.localScale = new Vector3(scale, scale, 1f);
            if (t < 1f)
                return;

            phase = 2;
            phaseTime = 0f;
            circle.gameObject.SetActive(false);
            pill.gameObject.SetActive(true);
            pill.sizeDelta = new Vector2(64f, 28f);
            pillImage.color = Blue;
            caption.text = "It keeps its real size.";
            caption.color = new Color(Blue.r, Blue.g, Blue.b, 0f);
        }

        void HoldLine()
        {
            float bob = Mathf.Sin(phaseTime * 2.4f) * 8f;
            pill.anchoredPosition = new Vector2(0f, 36f + bob);
            float fade = Mathf.Clamp01(phaseTime / 0.28f);
            caption.color = new Color(Blue.r, Blue.g, Blue.b, fade);
            if (phaseTime < 1.25f)
                return;

            phase = 3;
            phaseTime = 0f;
            pill.gameObject.SetActive(false);
            orb.gameObject.SetActive(true);
        }

        void FloatBody()
        {
            float arrive = Mathf.Clamp01(phaseTime / 0.72f);
            float eased = 1f - ((1f - arrive) * (1f - arrive));
            float breathe = 1f + (Mathf.Sin(phaseTime * 1.4f) * 0.018f);
            orb.localScale = Vector3.one * Mathf.Lerp(0.18f, 1f, eased) * breathe;
            float lift = Mathf.Sin(phaseTime * 1.15f) * 22f;
            orb.anchoredPosition = new Vector2(Mathf.Sin(phaseTime * 0.7f) * 10f, 90f + lift);
            ribbonA.localRotation = Quaternion.Euler(0f, 0f, phaseTime * 26f);
            ribbonB.localRotation = Quaternion.Euler(0f, 0f, -phaseTime * 17f);
            caption.color = new Color(Blue.r, Blue.g, Blue.b, 1f - eased);

            float height = Mathf.InverseLerp(-22f, 22f, lift);
            shadow.rectTransform.anchoredPosition = new Vector2(orb.anchoredPosition.x, -250f);
            shadow.rectTransform.localScale = new Vector3(Mathf.Lerp(1.05f, 0.72f, height), 1f, 1f);
            Color shade = shadow.color;
            shade.a = Mathf.Lerp(0.05f, 0.2f, eased) * Mathf.Lerp(1f, 0.55f, height);
            shadow.color = shade;
        }

        void Finish()
        {
            if (done)
                return;

            done = true;
            Action complete = finished;
            finished = null;
            if (complete != null)
                complete();
            Destroy(gameObject);
        }

        static RectTransform Ribbon(Transform parent, string name, float size, int band)
        {
            Image image = CreateImage(
                parent,
                name,
                Color.white,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(size, size));
            image.sprite = GlassRibbon(band);
            image.raycastTarget = false;
            return image.rectTransform;
        }

        static Text CreateCaption(Transform parent, Font typeface, string message)
        {
            GameObject host = new GameObject("Line");
            host.transform.SetParent(parent, false);
            Text text = host.AddComponent<Text>();
            text.font = typeface;
            text.fontSize = 34;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = message;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(760f, 80f);
            rect.anchoredPosition = new Vector2(0f, -36f);
            return text;
        }

        static Image CreateImage(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            GameObject host = new GameObject(name, typeof(RectTransform));
            host.transform.SetParent(parent, false);
            Image image = host.AddComponent<Image>();
            image.color = color;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return image;
        }

        static Sprite VerticalGradient(Color top, Color bottom)
        {
            Texture2D texture = new Texture2D(4, 64, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < 64; y++)
            {
                Color color = Color.Lerp(bottom, top, y / 63f);
                for (int x = 0; x < 4; x++)
                    texture.SetPixel(x, y, color);
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 4f, 64f), new Vector2(0.5f, 0.5f));
        }

        static Sprite Disc(Color color, bool soft)
        {
            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            float centre = (size - 1) * 0.5f;
            float radius = centre - 1f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Mathf.Sqrt(((x - centre) * (x - centre)) + ((y - centre) * (y - centre)));
                    float edge = Mathf.Clamp01((radius + 1.2f - distance) / (soft ? 8f : 2.2f));
                    Color pixel = color;
                    pixel.a = edge;
                    texture.SetPixel(x, y, pixel);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }

        static Sprite Capsule()
        {
            const int width = 96;
            const int height = 42;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            float radius = (height - 1) * 0.5f;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = 0f;
                    if (x < radius)
                        dx = radius - x;
                    else if (x > width - 1 - radius)
                        dx = x - (width - 1 - radius);
                    float dy = y - radius;
                    float distance = Mathf.Sqrt((dx * dx) + (dy * dy));
                    float edge = Mathf.Clamp01((radius + 0.8f - distance) / 1.8f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, edge));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f));
        }

        static Sprite GlassShell()
        {
            const int size = 256;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            float centre = (size - 1) * 0.5f;
            float radius = centre - 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - centre) / radius;
                    float dy = (y - centre) / radius;
                    float distance = Mathf.Sqrt((dx * dx) + (dy * dy));
                    if (distance > 1.02f)
                    {
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, 0f));
                        continue;
                    }

                    float edge = Mathf.Clamp01((1.01f - distance) / 0.035f);
                    float rim = Mathf.Clamp01((distance - 0.78f) / 0.18f) * Mathf.Clamp01((1f - distance) / 0.08f);
                    float body = Mathf.Clamp01(1f - distance) * 0.16f;
                    float highlight = Mathf.Clamp01(1f - (Mathf.Sqrt(((dx + 0.32f) * (dx + 0.32f)) + ((dy - 0.38f) * (dy - 0.38f))) / 0.42f));
                    highlight = highlight * highlight;
                    Color color = Color.Lerp(new Color(0.93f, 0.96f, 1f, 1f), Color.white, highlight);
                    color.a = Mathf.Clamp01((body + (rim * 0.72f) + (highlight * 0.55f)) * edge);
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }

        static Sprite GlassRibbon(int band)
        {
            const int size = 256;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            float centre = (size - 1) * 0.5f;
            float radius = centre - 2f;
            Color inner = band == 0
                ? new Color(0.55f, 0.82f, 0.28f, 1f)
                : new Color(0.95f, 0.28f, 0.48f, 1f);
            Color outer = band == 0
                ? new Color(0.98f, 0.62f, 0.18f, 1f)
                : new Color(0.98f, 0.78f, 0.28f, 1f);
            float near = band == 0 ? 0.34f : 0.5f;
            float far = band == 0 ? 0.62f : 0.78f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - centre) / radius;
                    float dy = (y - centre) / radius;
                    float distance = Mathf.Sqrt((dx * dx) + (dy * dy));
                    float angle = Mathf.Atan2(dy, dx);
                    float across = Mathf.Clamp01((distance - near) / 0.08f) * Mathf.Clamp01((far - distance) / 0.08f);
                    float sweep = Mathf.Clamp01(0.5f + (0.5f * Mathf.Sin((angle * (band == 0 ? 1.4f : 2f)) + (band * 1.2f))));
                    float alpha = across * sweep * (band == 0 ? 0.78f : 0.62f);
                    if (distance > 0.96f)
                        alpha = 0f;
                    float mix = Mathf.Clamp01(0.5f + (0.5f * Mathf.Sin(angle + distance)));
                    Color color = Color.Lerp(inner, outer, mix);
                    color.a = alpha;
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }

        static Sprite SoftEllipse()
        {
            const int width = 128;
            const int height = 48;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nx = (x - (width * 0.5f)) / (width * 0.5f);
                    float ny = (y - (height * 0.5f)) / (height * 0.5f);
                    float falloff = Mathf.Clamp01(1f - ((nx * nx) + (ny * ny)));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, falloff * falloff));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f));
        }
    }
}
