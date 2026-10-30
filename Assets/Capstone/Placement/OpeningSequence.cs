using System;
using UnityEngine;
using UnityEngine.UI;

namespace Capstone.Placement
{
    /// <summary>
    /// The first screens after the app opens.
    /// It begins as one glowing circle with no words. A tap brings the next line.
    /// The edge swells and settles. The light inside drifts. The circle is drawn here.
    /// </summary>
    public sealed class OpeningSequence : MonoBehaviour
    {
        const int BeatCount = 3;

        static readonly string[] Titles =
        {
            "See it on your wall.",
            "Ask one thing.",
            "Three other works."
        };

        static readonly string[] Lines =
        {
            "The work keeps the centimetres stored on it.",
            "Size, light, colour, meaning, or price. The reply is already written.",
            "Each one brings its story. You can try the next one on the same wall."
        };

        static readonly Color Paper = new Color(0.965f, 0.965f, 0.969f, 1f);
        static readonly Color Ink = new Color(0.16f, 0.14f, 0.16f, 1f);

        Font font;
        Action finished;
        int beat;
        float clock;
        bool introduced;
        Text title;
        Text line;
        Text nextLabel;
        Image[] dots;
        GlassBodyGraphic body;
        GameObject words;
        Button screenTap;

        public static void Play(Font typeface, Action onFinished)
        {
            GameObject host = new GameObject("Opening");
            OpeningSequence sequence = host.AddComponent<OpeningSequence>();
            sequence.font = typeface;
            sequence.finished = onFinished;
            sequence.Build();
        }

        void Build()
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

            Image ground = CreateImage(canvasObject.transform, "Ground", Paper, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            screenTap = ground.gameObject.AddComponent<Button>();
            screenTap.targetGraphic = ground;
            screenTap.onClick.AddListener(Reveal);

            GameObject bodyObject = new GameObject("Glowing body", typeof(RectTransform));
            bodyObject.transform.SetParent(canvasObject.transform, false);
            body = bodyObject.AddComponent<GlassBodyGraphic>();
            body.sprite = GlassBody();
            body.raycastTarget = false;
            body.color = Color.white;
            RectTransform bodyRect = body.rectTransform;
            bodyRect.anchorMin = new Vector2(0.5f, 0.5f);
            bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
            bodyRect.pivot = new Vector2(0.5f, 0.5f);
            bodyRect.sizeDelta = new Vector2(620f, 620f);
            bodyRect.anchoredPosition = Vector2.zero;

            words = new GameObject("Words", typeof(RectTransform));
            words.transform.SetParent(canvasObject.transform, false);
            RectTransform wordRect = words.GetComponent<RectTransform>();
            wordRect.anchorMin = Vector2.zero;
            wordRect.anchorMax = Vector2.one;
            wordRect.offsetMin = Vector2.zero;
            wordRect.offsetMax = Vector2.zero;

            title = Label(words.transform, 64, FontStyle.Bold, new Vector2(0f, 280f), new Vector2(920f, 160f));
            line = Label(words.transform, 32, FontStyle.Normal, new Vector2(0f, 140f), new Vector2(860f, 120f));
            line.color = new Color(Ink.r, Ink.g, Ink.b, 0.78f);

            Sprite dot = HardDot();
            dots = new Image[BeatCount];
            for (int i = 0; i < BeatCount; i++)
            {
                dots[i] = CreateImage(
                    words.transform,
                    "Dot",
                    Ink,
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    new Vector2(-36f + (i * 36f), 250f),
                    new Vector2(12f, 12f));
                dots[i].sprite = dot;
                dots[i].raycastTarget = false;
            }

            Button next = Button(words.transform, out nextLabel);
            next.onClick.AddListener(Advance);
            Button skip = SmallButton(words.transform, "Skip");
            skip.onClick.AddListener(Finish);
            words.SetActive(false);
        }

        void Update()
        {
            clock += Time.deltaTime;
            Vector2 home = introduced ? new Vector2(0f, 360f) : Vector2.zero;
            Vector2 sway = new Vector2(
                (Mathf.Sin(clock * 0.37f) * 16f) + (Mathf.Sin(clock * 0.71f) * 7f),
                (Mathf.Sin(clock * 0.53f) * 24f) + (Mathf.Cos(clock * 0.29f) * 8f));
            body.rectTransform.anchoredPosition = Vector2.Lerp(
                body.rectTransform.anchoredPosition,
                home + sway,
                1f - Mathf.Exp(-2.2f * Time.deltaTime));
            body.clock = clock;
            body.SetVerticesDirty();
        }

        void Reveal()
        {
            if (introduced)
                return;

            introduced = true;
            screenTap.interactable = false;
            words.SetActive(true);
            ShowBeat();
        }

        void ShowBeat()
        {
            title.text = Titles[beat];
            line.text = Lines[beat];
            nextLabel.text = beat == BeatCount - 1 ? "Begin" : "Next";
            for (int i = 0; i < dots.Length; i++)
                dots[i].color = i == beat ? Ink : new Color(Ink.r, Ink.g, Ink.b, 0.28f);
        }

        void Advance()
        {
            if (beat >= BeatCount - 1)
            {
                Finish();
                return;
            }

            beat += 1;
            ShowBeat();
        }

        void Finish()
        {
            Action done = finished;
            finished = null;
            if (done != null)
                done();
            Destroy(gameObject);
        }

        Text Label(Transform parent, int size, FontStyle style, Vector2 position, Vector2 box)
        {
            GameObject host = new GameObject("Line");
            host.transform.SetParent(parent, false);
            Text text = host.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Ink;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = box;
            rect.anchoredPosition = position;
            return text;
        }

        Button Button(Transform parent, out Text caption)
        {
            Image image = CreateImage(
                parent,
                "Next",
                Ink,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 100f),
                new Vector2(420f, 96f));
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            caption = Label(image.transform, 36, FontStyle.Bold, Vector2.zero, new Vector2(420f, 96f));
            caption.color = Paper;
            caption.rectTransform.anchorMin = Vector2.zero;
            caption.rectTransform.anchorMax = Vector2.one;
            caption.rectTransform.offsetMin = Vector2.zero;
            caption.rectTransform.offsetMax = Vector2.zero;
            caption.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            return button;
        }

        Button SmallButton(Transform parent, string wordsOnButton)
        {
            Image image = CreateImage(
                parent,
                wordsOnButton,
                new Color(0f, 0f, 0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-36f, -48f),
                new Vector2(160f, 72f));
            image.rectTransform.pivot = new Vector2(1f, 1f);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Text caption = Label(image.transform, 28, FontStyle.Normal, Vector2.zero, new Vector2(160f, 72f));
            caption.text = wordsOnButton;
            caption.color = new Color(Ink.r, Ink.g, Ink.b, 0.55f);
            caption.rectTransform.anchorMin = Vector2.zero;
            caption.rectTransform.anchorMax = Vector2.one;
            caption.rectTransform.offsetMin = Vector2.zero;
            caption.rectTransform.offsetMax = Vector2.zero;
            return button;
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

        static Sprite HardDot()
        {
            const int size = 32;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            float centre = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centre, centre)) / centre;
                    float alpha = Mathf.Clamp01((1f - distance) * size);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }

        static Sprite GlassBody()
        {
            const int size = 512;
            const float span = 1.12f;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            Vector3 light = new Vector3(-0.35f, 0.55f, 0.76f).normalized;
            Color rose = new Color(0.925f, 0.306f, 0.40f, 1f);
            Color blush = new Color(1f, 0.690f, 0.722f, 1f);
            Color glass = new Color(0.980f, 0.976f, 0.976f, 1f);
            Color clear = new Color(1f, 1f, 1f, 0f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = (((x / (size - 1f)) * 2f) - 1f) * span;
                    float py = (((y / (size - 1f)) * 2f) - 1f) * span;
                    float radius = Mathf.Sqrt((px * px) + (py * py));
                    if (radius >= 1.05f)
                    {
                        texture.SetPixel(x, y, clear);
                        continue;
                    }

                    if (radius > 1f)
                    {
                        float glow = (1.05f - radius) / 0.05f;
                        texture.SetPixel(x, y, new Color(1f, 0.894f, 0.902f, glow * glow * 0.55f));
                        continue;
                    }

                    float facing = Mathf.Sqrt(Mathf.Max(0f, 1f - (radius * radius)));
                    float shade = Mathf.Max(0f, (px * light.x) + (py * light.y) + (facing * light.z));
                    float shine = Mathf.Pow(shade, 28f);
                    float angle = Mathf.Atan2(py + 0.05f, px - 0.08f);
                    float band = Mathf.Sin((angle * 2f) + (radius * 2.4f));
                    float veil = Smooth(0.15f, 0.55f, band) * Smooth(0.15f, 0.95f, 1f - radius);
                    float bandTwo = Mathf.Sin((angle * 3f) - 1.1f + py);
                    float veilTwo = Smooth(0.45f, 0.85f, bandTwo) * Smooth(0.2f, 0.9f, 1f - radius) * 0.65f;
                    Color color = Color.Lerp(glass, blush, veil * 0.85f);
                    color = Color.Lerp(color, rose, veilTwo * veil);
                    float rim = Mathf.Pow(1f - facing, 3f);
                    color = Color.Lerp(color, Color.white, Mathf.Min(1f, rim * 1.15f));
                    color = Color.Lerp(color, Color.white, shine);
                    color = Color.Lerp(color, new Color(0.808f, 0.839f, 0.878f, 1f), (1f - shade) * 0.18f);
                    color.a = Mathf.Min(1f, (1f - radius) * (size * 0.45f));
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }

        static float Smooth(float start, float end, float value)
        {
            if (value <= start)
                return 0f;
            if (value >= end)
                return 1f;

            float span = (value - start) / (end - start);
            return span * span * (3f - (2f * span));
        }
    }

    /// <summary>
    /// A round glass body whose edge changes shape while the picture inside drifts.
    /// </summary>
    public sealed class GlassBodyGraphic : MaskableGraphic
    {
        public Sprite sprite;
        public float clock;

        public override Texture mainTexture
        {
            get { return sprite != null ? sprite.texture : null; }
        }

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            Rect rect = GetPixelAdjustedRect();
            float radiusX = rect.width * 0.5f;
            float radiusY = rect.height * 0.5f;
            Vector2 centre = rect.center;
            const int steps = 72;
            float drift = clock * 0.22f;

            UIVertex middle = UIVertex.simpleVert;
            middle.color = color;
            middle.position = centre;
            middle.uv0 = new Vector4(0.5f, 0.5f, 0f, 0f);
            helper.AddVert(middle);

            for (int i = 0; i < steps; i++)
            {
                float angle = (i / (float)steps) * Mathf.PI * 2f;
                float swell = 1f
                    + (0.07f * Mathf.Sin((clock * 1.15f) + (angle * 2f)))
                    + (0.05f * Mathf.Sin((clock * 0.68f) + (angle * 3f)))
                    + (0.035f * Mathf.Sin((clock * 1.7f) - angle));
                float wide = 1f + (0.06f * Mathf.Sin(clock * 0.8f));
                float tall = 1f - (0.05f * Mathf.Sin(clock * 0.8f));
                Vector2 point = new Vector2(
                    Mathf.Cos(angle) * radiusX * swell * wide,
                    Mathf.Sin(angle) * radiusY * swell * tall);

                UIVertex vertex = UIVertex.simpleVert;
                vertex.color = color;
                vertex.position = centre + point;
                vertex.uv0 = new Vector4(
                    0.5f + (Mathf.Cos(angle + drift) * 0.43f),
                    0.5f + (Mathf.Sin(angle + drift) * 0.43f),
                    0f,
                    0f);
                helper.AddVert(vertex);
            }

            for (int i = 0; i < steps; i++)
                helper.AddTriangle(0, 1 + i, 1 + ((i + 1) % steps));
        }
    }
}
