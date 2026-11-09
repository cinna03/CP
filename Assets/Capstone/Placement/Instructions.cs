using System;
using UnityEngine;
using UnityEngine.UI;

namespace Capstone.Placement
{
    /// <summary>
    /// The instructions play like the iPad Pro page. A black stage, the work as the picture,
    /// and the line changes in a crossfade the way the highlights reel does.
    /// The last chapter, or Skip, starts the opening film.
    /// </summary>
    public sealed class Instructions : MonoBehaviour
    {
        struct Step
        {
            public string Kicker;
            public string Claim;
            public string Line;
            public string Picture;
        }

        enum Beat
        {
            Enter,
            Hold,
            Leave
        }

        static readonly Step[] Steps =
        {
            new Step { Kicker = "Choose", Claim = "One piece.", Line = "Read the size it was made.", Picture = "reclining" },
            new Step { Kicker = "The wall", Claim = "Hold it up.", Line = "Point the circle at an open wall.", Picture = "folded" },
            new Step { Kicker = "One question", Claim = "Ask, or skip.", Line = "The answer comes from the written rules.", Picture = "green-dress" },
            new Step { Kicker = "Then", Claim = "Keep one.", Line = "Save, dismiss, or take one of three.", Picture = "hands" }
        };

        static readonly Color Stage = Color.black;
        static readonly Color Ink = Color.white;
        static readonly Color Mist = new Color(1f, 1f, 1f, 0.72f);
        static readonly Color PricingBlue = new Color(0f, 0.443f, 0.890f, 1f);
        const float EnterDuration = 1.05f;
        const float HoldDuration = 3.2f;
        const float LeaveDuration = 0.72f;

        static Sprite pill;
        static Sprite plate;

        Action finished;
        int index;
        float motionTime;
        Beat beat;
        bool ready;
        bool swapped;
        bool closing;
        float laidHalf = -1f;
        Canvas stageCanvas;
        CanvasGroup rootFade;
        CanvasGroup copyFade;
        CanvasGroup frameFade;
        CanvasGroup frontFade;
        CanvasGroup backFade;
        CanvasGroup controlFade;
        RectTransform copyRect;
        RectTransform frameRect;
        RectTransform frontRect;
        RectTransform backRect;
        RawImage front;
        RawImage back;
        Text kicker;
        Text claim;
        Text line;
        Image[] dots;
        Vector2 frameRest;

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
            stageCanvas = canvasObject.AddComponent<Canvas>();
            stageCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            stageCanvas.sortingOrder = 900;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            rootFade = canvasObject.AddComponent<CanvasGroup>();

            Image paper = CreateImage(canvasObject.transform, "Stage", Stage);
            paper.raycastTarget = true;
            Stretch(paper.rectTransform);
            Button page = paper.gameObject.AddComponent<Button>();
            page.targetGraphic = paper;
            page.transition = Selectable.Transition.None;
            page.onClick.AddListener(Advance);

            GameObject copy = new GameObject("Copy", typeof(RectTransform));
            copy.transform.SetParent(canvasObject.transform, false);
            copyRect = copy.GetComponent<RectTransform>();
            copyRect.anchorMin = new Vector2(0.5f, 0.5f);
            copyRect.anchorMax = new Vector2(0.5f, 0.5f);
            copyRect.sizeDelta = new Vector2(1080f, 1920f);
            kicker = CreateText(copy.transform, typeface, Steps[0].Kicker, 28, Mist, FontStyle.Normal);
            claim = CreateText(copy.transform, typeface, Steps[0].Claim, 78, Ink, FontStyle.Bold);
            line = CreateText(copy.transform, typeface, Steps[0].Line, 30, Mist, FontStyle.Normal);
            copyFade = copy.AddComponent<CanvasGroup>();
            copyFade.alpha = 0f;

            GameObject frame = new GameObject("Frame", typeof(RectTransform));
            frame.transform.SetParent(canvasObject.transform, false);
            frameRect = frame.GetComponent<RectTransform>();
            frameRect.anchorMin = new Vector2(0.5f, 0.5f);
            frameRect.anchorMax = new Vector2(0.5f, 0.5f);
            frameRect.pivot = new Vector2(0.5f, 0.5f);
            Image framePlate = frame.AddComponent<Image>();
            framePlate.sprite = Plate();
            framePlate.type = Image.Type.Sliced;
            framePlate.color = Stage;
            framePlate.raycastTarget = false;
            frame.AddComponent<Mask>().showMaskGraphic = true;
            frameFade = frame.AddComponent<CanvasGroup>();
            frameFade.alpha = 0f;

            front = AddPicture(frame.transform, "Front", out frontRect, out frontFade);
            back = AddPicture(frame.transform, "Back", out backRect, out backFade);
            Texture2D first = Load(Steps[0].Picture);
            front.texture = first;
            backFade.alpha = 0f;

            GameObject controls = new GameObject("Controls", typeof(RectTransform));
            controls.transform.SetParent(canvasObject.transform, false);
            RectTransform controlsRect = controls.GetComponent<RectTransform>();
            controlsRect.anchorMin = new Vector2(0.5f, 0.5f);
            controlsRect.anchorMax = new Vector2(0.5f, 0.5f);
            controlsRect.sizeDelta = new Vector2(1080f, 1920f);

            dots = new Image[Steps.Length];
            for (int i = 0; i < dots.Length; i++)
            {
                Image dot = CreateImage(controls.transform, "Dot", Color.white);
                dot.sprite = Pill();
                dot.type = Image.Type.Sliced;
                dot.raycastTarget = false;
                dots[i] = dot;
            }

            Image buttonImage = CreateImage(controls.transform, "Continue", PricingBlue);
            buttonImage.sprite = Pill();
            buttonImage.type = Image.Type.Sliced;
            Button button = buttonImage.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            ColorBlock colors = button.colors;
            colors.fadeDuration = 0.12f;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.94f, 1f, 1f);
            colors.pressedColor = new Color(0.8f, 0.86f, 0.96f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            button.onClick.AddListener(Advance);
            Text label = CreateText(buttonImage.transform, typeface, "Continue", 30, Color.white, FontStyle.Normal);
            Stretch(label.rectTransform);

            Text skipLabel = CreateText(controls.transform, typeface, "Skip", 30, Mist, FontStyle.Normal);
            skipLabel.raycastTarget = true;
            Button skip = skipLabel.gameObject.AddComponent<Button>();
            skip.targetGraphic = skipLabel;
            skip.onClick.AddListener(Finish);

            controlFade = controls.AddComponent<CanvasGroup>();
            controlFade.alpha = 0f;
            controlFade.blocksRaycasts = false;
            PaintDots();
            Layout(960f);
        }

        void Update()
        {
            float half = VisibleHalfHeight(stageCanvas);
            if (Mathf.Abs(half - laidHalf) > 12f)
                Layout(half);

            float step = Mathf.Min(Time.deltaTime, 0.033f);
            motionTime += step;
            if (closing)
            {
                float gone = Ease(motionTime / 0.45f);
                if (rootFade != null)
                    rootFade.alpha = 1f - gone;
                if (gone >= 1f)
                    Finish();
                return;
            }

            if (beat == Beat.Enter)
                PlayEnter();
            else if (beat == Beat.Hold)
            {
                ready = true;
                if (controlFade != null)
                    controlFade.blocksRaycasts = true;
                if (motionTime >= HoldDuration)
                    Advance();
            }
            else
                PlayLeave();
        }

        void PlayEnter()
        {
            float along = Ease(motionTime / EnterDuration);
            if (frameFade != null)
                frameFade.alpha = along;
            if (frameRect != null)
            {
                float scale = Mathf.Lerp(1.08f, 1f, along);
                frameRect.localScale = new Vector3(scale, scale, 1f);
                frameRect.anchoredPosition = Vector2.Lerp(frameRest + new Vector2(0f, -70f), frameRest, along);
            }

            float copy = Ease((motionTime - 0.14f) / 0.62f);
            if (copyFade != null)
                copyFade.alpha = copy;
            if (copyRect != null)
                copyRect.anchoredPosition = Vector2.Lerp(new Vector2(0f, -28f), Vector2.zero, copy);
            if (controlFade != null)
            {
                controlFade.alpha = Ease((motionTime - 0.32f) / 0.45f);
                controlFade.blocksRaycasts = controlFade.alpha > 0.55f;
            }

            if (motionTime < EnterDuration)
                return;

            beat = Beat.Hold;
            motionTime = 0f;
            ready = true;
        }

        void PlayLeave()
        {
            float along = Ease(motionTime / LeaveDuration);
            float copy = along < 0.42f ? 1f - (along / 0.42f) : (along - 0.42f) / 0.58f;
            if (copyFade != null)
                copyFade.alpha = copy;
            if (copyRect != null)
            {
                float rise = along < 0.42f ? along / 0.42f : (along - 0.42f) / 0.58f;
                float y = along < 0.42f ? Mathf.Lerp(0f, 22f, rise) : Mathf.Lerp(-22f, 0f, rise);
                copyRect.anchoredPosition = new Vector2(0f, y);
            }

            if (!swapped && along >= 0.42f)
            {
                ApplyCopy(index + 1);
                swapped = true;
            }

            if (backFade != null)
                backFade.alpha = along;
            if (frontFade != null)
                frontFade.alpha = 1f - along;
            if (along < 1f)
                return;

            if (front != null && back != null)
                front.texture = back.texture;
            Fit(front, frontRect, front != null ? front.texture as Texture2D : null);
            if (frontFade != null)
                frontFade.alpha = 1f;
            if (backFade != null)
                backFade.alpha = 0f;
            index += 1;
            swapped = false;
            PaintDots();
            beat = Beat.Hold;
            motionTime = 0f;
            ready = true;
        }

        void Advance()
        {
            if (!ready || beat == Beat.Leave || closing)
                return;

            ready = false;
            if (controlFade != null)
                controlFade.blocksRaycasts = false;
            if (index >= Steps.Length - 1)
            {
                closing = true;
                motionTime = 0f;
                return;
            }

            Texture2D picture = Load(Steps[index + 1].Picture);
            if (back != null)
                back.texture = picture;
            Fit(back, backRect, picture);
            if (backFade != null)
                backFade.alpha = 0f;
            beat = Beat.Leave;
            motionTime = 0f;
            swapped = false;
        }

        void ApplyCopy(int stepIndex)
        {
            if (stepIndex < 0 || stepIndex >= Steps.Length)
                return;

            Step step = Steps[stepIndex];
            if (kicker != null)
                kicker.text = step.Kicker;
            if (claim != null)
                claim.text = step.Claim;
            if (line != null)
                line.text = step.Line;
        }

        void PaintDots()
        {
            if (dots == null)
                return;

            for (int i = 0; i < dots.Length; i++)
            {
                if (dots[i] == null)
                    continue;
                bool current = i == index;
                dots[i].color = new Color(1f, 1f, 1f, current ? 1f : 0.35f);
                dots[i].rectTransform.sizeDelta = current ? new Vector2(36f, 10f) : new Vector2(10f, 10f);
            }
        }

        void Layout(float half)
        {
            laidHalf = half;
            float top = half - 36f;
            Place(kicker.rectTransform, 0f, top, 920f, 40f);
            Place(claim.rectTransform, 0f, top - 96f, 980f, 100f);
            Place(line.rectTransform, 0f, top - 186f, 920f, 72f);

            float lineBottom = (top - 186f) - 36f;
            float buttonY = -half + 118f;
            float skipY = -half + 42f;
            float dotsY = buttonY + 78f;
            float frameTop = lineBottom - 20f;
            float frameBottom = dotsY + 28f;
            float height = Mathf.Clamp(frameTop - frameBottom, 240f, 720f);
            float center = (frameTop + frameBottom) * 0.5f;
            float width = Mathf.Min(960f, height * 1.35f);
            frameRest = new Vector2(0f, center);
            if (frameRect != null)
            {
                frameRect.sizeDelta = new Vector2(width, height);
                if (beat != Beat.Enter)
                    frameRect.anchoredPosition = frameRest;
            }

            Fit(front, frontRect, front != null ? front.texture as Texture2D : null);
            Fit(back, backRect, back != null ? back.texture as Texture2D : null);

            float dotX = -((dots.Length - 1) * 42f) * 0.5f;
            for (int i = 0; i < dots.Length; i++)
            {
                bool current = i == index;
                Place(dots[i].rectTransform, dotX + (i * 42f), dotsY, current ? 36f : 10f, 10f);
            }

            Transform button = controlFade.transform.Find("Continue");
            if (button != null)
                Place(button.GetComponent<RectTransform>(), 0f, buttonY, 280f, 84f);
            Transform skip = controlFade.transform.Find("Skip");
            if (skip != null)
                Place(skip.GetComponent<RectTransform>(), 0f, skipY, 220f, 44f);
        }

        static RawImage AddPicture(Transform parent, string name, out RectTransform rect, out CanvasGroup fade)
        {
            GameObject stage = new GameObject(name, typeof(RectTransform));
            stage.transform.SetParent(parent, false);
            RawImage image = stage.AddComponent<RawImage>();
            image.raycastTarget = false;
            rect = stage.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            fade = stage.AddComponent<CanvasGroup>();
            return image;
        }

        static void Fit(RawImage image, RectTransform rect, Texture2D picture)
        {
            if (image == null || rect == null || picture == null)
                return;

            RectTransform frame = rect.parent as RectTransform;
            if (frame == null)
                return;

            float frameWidth = Mathf.Max(1f, frame.sizeDelta.x);
            float frameHeight = Mathf.Max(1f, frame.sizeDelta.y);
            float aspect = picture.width / (float)Mathf.Max(1, picture.height);
            float width = frameWidth;
            float height = width / aspect;
            if (height > frameHeight)
            {
                height = frameHeight;
                width = height * aspect;
            }

            rect.sizeDelta = new Vector2(width, height);
        }

        static Texture2D Load(string picture)
        {
            return Resources.Load<Texture2D>("TrialPieces/" + picture);
        }

        void Finish()
        {
            Action done = finished;
            finished = null;
            Destroy(gameObject);
            if (done != null)
                done();
        }

        static float Ease(float value)
        {
            float clamped = Mathf.Clamp01(value);
            return clamped * clamped * (3f - (2f * clamped));
        }

        static float VisibleHalfHeight(Canvas canvas)
        {
            if (canvas == null)
                return 960f;

            Canvas.ForceUpdateCanvases();
            float scale = canvas.scaleFactor;
            if (scale < 0.01f)
                scale = 1f;

            float height = canvas.pixelRect.height / scale;
            if (height < 400f)
                height = 1920f;
            return height * 0.5f;
        }

        static Sprite Pill()
        {
            if (pill != null)
                return pill;

            pill = Disc(64);
            return pill;
        }

        static Sprite Plate()
        {
            if (plate != null)
                return plate;

            const int size = 128;
            const int radius = 36;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            Color clear = new Color(1f, 1f, 1f, 0f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(radius - x, 0, x - (size - 1 - radius));
                    float dy = Mathf.Max(radius - y, 0, y - (size - 1 - radius));
                    bool inside = (dx * dx) + (dy * dy) <= radius * radius;
                    texture.SetPixel(x, y, inside ? Color.white : clear);
                }
            }

            texture.Apply();
            plate = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
            return plate;
        }

        static Sprite Disc(int size)
        {
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
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(30f, 30f, 30f, 30f));
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

        static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            if (rect == null)
                return;

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
