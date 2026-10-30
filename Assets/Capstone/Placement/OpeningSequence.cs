using System;
using UnityEngine;
using UnityEngine.UI;

namespace Capstone.Placement
{
    /// <summary>
    /// The first screens after the app opens. Each tap shows the next line, then the session starts.
    /// The soft forms are drawn here. They are an original motion for this intro.
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

        static readonly Color Paper = new Color(0.95f, 0.94f, 0.92f, 1f);
        static readonly Color Ink = new Color(0.16f, 0.14f, 0.16f, 1f);
        static readonly Color[] Lead =
        {
            new Color(0.62f, 0.52f, 0.82f, 0.78f),
            new Color(0.55f, 0.28f, 0.48f, 0.72f),
            new Color(0.45f, 0.58f, 0.72f, 0.75f)
        };
        static readonly Color[] Warm =
        {
            new Color(0.90f, 0.62f, 0.42f, 0.70f),
            new Color(0.86f, 0.45f, 0.55f, 0.62f),
            new Color(0.82f, 0.70f, 0.42f, 0.68f)
        };

        Font font;
        Action finished;
        int beat;
        float clock;
        Text title;
        Text line;
        Text nextLabel;
        Image[] forms;
        Image[] dots;
        readonly Vector2[] homes = new Vector2[3];
        readonly Vector2[] drifts = new Vector2[3];

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
            ground.raycastTarget = true;

            Sprite disc = SoftDisc();
            forms = new Image[3];
            homes[0] = new Vector2(-120f, 280f);
            homes[1] = new Vector2(160f, 80f);
            homes[2] = new Vector2(20f, 460f);
            drifts[0] = new Vector2(36f, 28f);
            drifts[1] = new Vector2(48f, 22f);
            drifts[2] = new Vector2(18f, 40f);
            forms[0] = Form(canvasObject.transform, disc, homes[0], new Vector2(520f, 520f));
            forms[1] = Form(canvasObject.transform, disc, homes[1], new Vector2(340f, 340f));
            forms[2] = Form(canvasObject.transform, disc, homes[2], new Vector2(220f, 220f));

            title = Label(canvasObject.transform, 64, FontStyle.Bold, new Vector2(0f, 220f), new Vector2(920f, 180f));
            line = Label(canvasObject.transform, 32, FontStyle.Normal, new Vector2(0f, 80f), new Vector2(860f, 140f));
            line.color = new Color(Ink.r, Ink.g, Ink.b, 0.78f);

            dots = new Image[BeatCount];
            for (int i = 0; i < BeatCount; i++)
            {
                dots[i] = CreateImage(
                    canvasObject.transform,
                    "Dot",
                    Ink,
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    new Vector2(-36f + (i * 36f), 250f),
                    new Vector2(14f, 14f));
                dots[i].sprite = disc;
                dots[i].raycastTarget = false;
            }

            Button next = Button(canvasObject.transform, out nextLabel);
            next.onClick.AddListener(Advance);
            Button skip = SmallButton(canvasObject.transform, "Skip");
            skip.onClick.AddListener(Finish);
            ShowBeat();
        }

        void Update()
        {
            clock += Time.deltaTime;
            for (int i = 0; i < forms.Length; i++)
            {
                float speed = 0.55f + (i * 0.18f);
                float phase = i * 1.7f;
                Vector2 sway = new Vector2(
                    Mathf.Sin((clock * speed) + phase) * drifts[i].x,
                    Mathf.Cos((clock * speed * 0.8f) + phase) * drifts[i].y);
                RectTransform rect = forms[i].rectTransform;
                Vector2 home = homes[i] + (PageShift() * (i + 1));
                rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, home + sway, 1f - Mathf.Exp(-3.5f * Time.deltaTime));
                float pulse = 1f + (Mathf.Sin((clock * 0.9f) + phase) * 0.035f);
                rect.localScale = Vector3.Lerp(rect.localScale, new Vector3(pulse, pulse, 1f), 1f - Mathf.Exp(-4f * Time.deltaTime));
                Color target = i == 1 ? Warm[beat] : Lead[beat];
                forms[i].color = Color.Lerp(forms[i].color, target, 1f - Mathf.Exp(-3f * Time.deltaTime));
            }
        }

        Vector2 PageShift()
        {
            if (beat == 1)
                return new Vector2(70f, -40f);
            if (beat == 2)
                return new Vector2(-40f, 30f);
            return Vector2.zero;
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

        Image Form(Transform parent, Sprite disc, Vector2 home, Vector2 size)
        {
            Image image = CreateImage(parent, "Form", Lead[0], new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), home, size);
            image.sprite = disc;
            image.raycastTarget = false;
            return image;
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

        Button SmallButton(Transform parent, string words)
        {
            Image image = CreateImage(
                parent,
                words,
                new Color(0f, 0f, 0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-36f, -48f),
                new Vector2(160f, 72f));
            image.rectTransform.pivot = new Vector2(1f, 1f);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Text caption = Label(image.transform, 28, FontStyle.Normal, Vector2.zero, new Vector2(160f, 72f));
            caption.text = words;
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

        static Sprite SoftDisc()
        {
            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float centre = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centre, centre)) / centre;
                    float alpha = Mathf.Clamp01(1f - distance);
                    alpha = alpha * alpha * (3f - (2f * alpha));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
