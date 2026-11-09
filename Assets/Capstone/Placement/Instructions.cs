using System;
using UnityEngine;
using UnityEngine.UI;

namespace Capstone.Placement
{
    /// <summary>
    /// The first-visit instructions, in the white gallery. The work stays the event.
    /// Continue moves the line. Skip, or the last Continue, starts the opening film.
    /// </summary>
    public sealed class Instructions : MonoBehaviour
    {
        struct Step
        {
            public string Kicker;
            public string Claim;
            public string Line;
        }

        static readonly Step[] Steps =
        {
            new Step { Kicker = "Choose", Claim = "One piece.", Line = "Read the size it was made." },
            new Step { Kicker = "The wall", Claim = "Hold it up.", Line = "Point the circle at an open wall." },
            new Step { Kicker = "One question", Claim = "Ask, or skip.", Line = "The answer comes from the written rules." },
            new Step { Kicker = "Then", Claim = "Keep one.", Line = "Save, dismiss, or take one of three." }
        };

        static readonly Color Gallery = Color.white;
        static readonly Color Ink = new Color(0.114f, 0.114f, 0.122f, 1f);
        static readonly Color Slate = new Color(0.439f, 0.439f, 0.439f, 1f);
        static readonly Color AppleBlue = new Color(0f, 0.4f, 0.8f, 1f);
        static readonly Color PricingBlue = new Color(0f, 0.443f, 0.890f, 1f);
        const float EnterDuration = 0.9f;
        const float LeaveDuration = 0.36f;

        static Sprite pill;

        Action finished;
        int index;
        float motionTime;
        bool leaving;
        bool enteredOnce;
        Text kicker;
        Text claim;
        Text line;
        Text count;
        RawImage work;
        CanvasGroup workFade;
        CanvasGroup copyFade;
        CanvasGroup controlFade;
        RectTransform workRect;
        readonly Vector2 workRest = new Vector2(0f, -80f);

        public static void Play(Font typeface, Action onFinished)
        {
            GameObject host = new GameObject("Instructions");
            Instructions guide = host.AddComponent<Instructions>();
            guide.finished = onFinished;
            guide.Build(typeface);
        }

        void Build(Font typeface)
        {
            GameObject canvasObject = new GameObject("Instructions canvas");
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            Image paper = CreateImage(canvasObject.transform, "Gallery", Gallery);
            Stretch(paper.rectTransform);

            count = CreateText(canvasObject.transform, typeface, "1 of 4", 28, Slate, FontStyle.Normal);
            Place(count.rectTransform, 0f, 760f, 400f, 40f);

            kicker = CreateText(canvasObject.transform, typeface, Steps[0].Kicker, 36, Ink, FontStyle.Bold);
            claim = CreateText(canvasObject.transform, typeface, Steps[0].Claim, 72, Ink, FontStyle.Bold);
            line = CreateText(canvasObject.transform, typeface, Steps[0].Line, 32, Slate, FontStyle.Normal);
            GameObject copy = new GameObject("Copy", typeof(RectTransform));
            copy.transform.SetParent(canvasObject.transform, false);
            RectTransform copyRect = copy.GetComponent<RectTransform>();
            copyRect.anchorMin = new Vector2(0.5f, 0.5f);
            copyRect.anchorMax = new Vector2(0.5f, 0.5f);
            copyRect.sizeDelta = new Vector2(1080f, 1920f);
            kicker.transform.SetParent(copy.transform, false);
            claim.transform.SetParent(copy.transform, false);
            line.transform.SetParent(copy.transform, false);
            Place(kicker.rectTransform, 0f, 620f, 900f, 52f);
            Place(claim.rectTransform, 0f, 530f, 900f, 96f);
            Place(line.rectTransform, 0f, 450f, 900f, 80f);
            copyFade = copy.AddComponent<CanvasGroup>();
            copyFade.alpha = 0f;

            Texture2D picture = Resources.Load<Texture2D>("TrialPieces/reclining");
            if (picture != null)
            {
                GameObject stage = new GameObject("Work", typeof(RectTransform));
                stage.transform.SetParent(canvasObject.transform, false);
                work = stage.AddComponent<RawImage>();
                work.texture = picture;
                work.raycastTarget = false;
                float aspect = picture.width / (float)Mathf.Max(1, picture.height);
                float width = 900f;
                float height = width / aspect;
                if (height > 640f)
                {
                    height = 640f;
                    width = height * aspect;
                }

                workRect = stage.GetComponent<RectTransform>();
                workRect.anchorMin = new Vector2(0.5f, 0.5f);
                workRect.anchorMax = new Vector2(0.5f, 0.5f);
                workRect.pivot = new Vector2(0.5f, 0.5f);
                workRect.sizeDelta = new Vector2(width, height);
                workRect.anchoredPosition = workRest + new Vector2(0f, -120f);
                workRect.localScale = new Vector3(0.86f, 0.86f, 1f);
                workFade = stage.AddComponent<CanvasGroup>();
                workFade.alpha = 0f;
            }

            GameObject controls = new GameObject("Controls", typeof(RectTransform));
            controls.transform.SetParent(canvasObject.transform, false);
            RectTransform controlsRect = controls.GetComponent<RectTransform>();
            controlsRect.anchorMin = new Vector2(0.5f, 0.5f);
            controlsRect.anchorMax = new Vector2(0.5f, 0.5f);
            controlsRect.sizeDelta = new Vector2(1080f, 1920f);

            Image buttonImage = CreateImage(controls.transform, "Continue", PricingBlue);
            buttonImage.sprite = Pill();
            buttonImage.type = Image.Type.Sliced;
            Place(buttonImage.rectTransform, 0f, -760f, 280f, 88f);
            Button button = buttonImage.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            ColorBlock colors = button.colors;
            colors.fadeDuration = 0.12f;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.95f, 1f, 1f);
            colors.pressedColor = new Color(0.82f, 0.88f, 0.96f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            button.onClick.AddListener(Continue);
            Text label = CreateText(buttonImage.transform, typeface, "Continue", 32, Color.white, FontStyle.Normal);
            Stretch(label.rectTransform);

            Text skipLabel = CreateText(controls.transform, typeface, "Skip", 32, AppleBlue, FontStyle.Normal);
            skipLabel.raycastTarget = true;
            Button skip = skipLabel.gameObject.AddComponent<Button>();
            skip.targetGraphic = skipLabel;
            skip.onClick.AddListener(Finish);
            Place(skip.GetComponent<RectTransform>(), 0f, -860f, 240f, 48f);

            controlFade = controls.AddComponent<CanvasGroup>();
            controlFade.alpha = 0f;
            controlFade.blocksRaycasts = false;
        }

        void Update()
        {
            float step = Mathf.Min(Time.deltaTime, 0.033f);
            motionTime += step;
            if (leaving)
            {
                float along = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(motionTime / LeaveDuration));
                if (copyFade != null)
                    copyFade.alpha = 1f - along;
                if (controlFade != null)
                    controlFade.alpha = 1f - along;
                if (along < 1f)
                    return;

                index += 1;
                if (index >= Steps.Length)
                {
                    Finish();
                    return;
                }

                ShowStep();
                leaving = false;
                motionTime = 0f;
                return;
            }

            float enter = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(motionTime / EnterDuration));
            if (!enteredOnce && workFade != null && workRect != null)
            {
                workFade.alpha = enter;
                float scale = Mathf.Lerp(0.86f, 1f, enter);
                workRect.localScale = new Vector3(scale, scale, 1f);
                workRect.anchoredPosition = Vector2.Lerp(workRest + new Vector2(0f, -120f), workRest, enter);
            }

            if (copyFade != null)
                copyFade.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((motionTime - 0.16f) / 0.55f));
            if (controlFade != null)
            {
                float shown = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((motionTime - 0.4f) / 0.4f));
                controlFade.alpha = shown;
                controlFade.blocksRaycasts = shown > 0.85f;
            }

            if (enter >= 1f)
                enteredOnce = true;
        }

        void Continue()
        {
            if (leaving || controlFade == null || !controlFade.blocksRaycasts)
                return;

            leaving = true;
            motionTime = 0f;
            controlFade.blocksRaycasts = false;
        }

        void ShowStep()
        {
            Step step = Steps[index];
            if (kicker != null)
                kicker.text = step.Kicker;
            if (claim != null)
                claim.text = step.Claim;
            if (line != null)
                line.text = step.Line;
            if (count != null)
                count.text = (index + 1) + " of " + Steps.Length;
        }

        void Finish()
        {
            Action done = finished;
            finished = null;
            Destroy(gameObject);
            if (done != null)
                done();
        }

        static Sprite Pill()
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
            image.raycastTarget = name != "Gallery";
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

        static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, y);
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
