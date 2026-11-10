using System;
using UnityEngine;
using UnityEngine.UI;

namespace Capstone.Placement
{
    /// <summary>
    /// The instructions turn like a cube in the hand. Each chapter is one face.
    /// A switch pitches, yaws, and rolls around the painting, then the face being turned to settles upright.
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

        static readonly Color Card = new Color(1f, 1f, 1f, 0.82f);
        static readonly Color Title = Color.white;
        static readonly Color Mist = new Color(1f, 1f, 1f, 0.78f);
        static readonly Color PricingBlue = new Color(0f, 0.4f, 0.8f, 1f);
        const float EnterDuration = 0.75f;
        const float HoldDuration = 3.2f;
        const float LeaveDuration = 1.2f;
        const int StageLayer = 3;

        static Sprite pill;
        static Sprite plate;

        Action finished;
        int index;
        float motionTime;
        Beat beat;
        bool ready;
        bool closing;
        float laidHalf = -1f;
        sealed class Face
        {
            public GameObject Root;
            public RectTransform Rect;
            public CanvasGroup Group;
            public Text Kicker;
            public Text Claim;
            public Text ClaimSoft;
            public Text Line;
            public RectTransform Frame;
            public RectTransform Rim;
            public RawImage Picture;
            public RectTransform PictureRect;
        }

        Canvas stageCanvas;
        Canvas hud;
        Camera stageCamera;
        RenderTexture frameTexture;
        CanvasGroup rootFade;
        CanvasGroup hudFade;
        CanvasGroup controlFade;
        Face shown;
        Face next;
        Image[] dots;
        Vector2 frameRest;
        Vector2 faceRest;

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
            stageCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            stageCanvas.worldCamera = StageCamera();
            stageCanvas.planeDistance = 12f;
            stageCanvas.sortingOrder = 900;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            rootFade = canvasObject.AddComponent<CanvasGroup>();

            Image paper = CreateImage(canvasObject.transform, "Stage", Color.black);
            paper.raycastTarget = true;
            Stretch(paper.rectTransform);
            Button page = paper.gameObject.AddComponent<Button>();
            page.targetGraphic = paper;
            page.transition = Selectable.Transition.None;
            page.onClick.AddListener(Advance);

            shown = BuildFace(canvasObject.transform, typeface, 0);
            next = BuildFace(canvasObject.transform, typeface, 1);
            shown.Group.alpha = 0f;
            shown.Rect.localRotation = Quaternion.Euler(-32f, 40f, -14f);
            next.Rect.localRotation = Quaternion.identity;
            next.Root.SetActive(false);

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
            PresentOnTop(controls);
            Layout(960f);
            SetLayer(canvasObject.transform, StageLayer);
        }

        void PresentOnTop(GameObject controls)
        {
            GameObject overlayObject = new GameObject("Instructions overlay");
            overlayObject.transform.SetParent(transform, false);
            hud = overlayObject.AddComponent<Canvas>();
            hud.renderMode = RenderMode.ScreenSpaceOverlay;
            hud.sortingOrder = 900;
            CanvasScaler scaler = overlayObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            overlayObject.AddComponent<GraphicRaycaster>();
            hudFade = overlayObject.AddComponent<CanvasGroup>();

            GameObject plate = new GameObject("Tilt", typeof(RectTransform));
            plate.transform.SetParent(overlayObject.transform, false);
            RawImage image = plate.AddComponent<RawImage>();
            image.texture = frameTexture;
            image.raycastTarget = true;
            Stretch(image.rectTransform);
            Button page = plate.AddComponent<Button>();
            page.targetGraphic = image;
            page.transition = Selectable.Transition.None;
            page.onClick.AddListener(Advance);
            controls.transform.SetParent(overlayObject.transform, false);
        }

        Camera StageCamera()
        {
            int width = Mathf.Max(16, Screen.width);
            int height = Mathf.Max(16, Screen.height);
            frameTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            frameTexture.antiAliasing = 1;
            frameTexture.Create();

            GameObject cameraObject = new GameObject("Instructions camera");
            cameraObject.transform.SetParent(transform, false);
            stageCamera = cameraObject.AddComponent<Camera>();
            stageCamera.clearFlags = CameraClearFlags.SolidColor;
            stageCamera.backgroundColor = Color.black;
            stageCamera.cullingMask = 1 << StageLayer;
            stageCamera.depth = 100f;
            stageCamera.orthographic = false;
            stageCamera.fieldOfView = 34f;
            stageCamera.nearClipPlane = 0.05f;
            stageCamera.farClipPlane = 40f;
            stageCamera.allowHDR = false;
            stageCamera.allowMSAA = false;
            stageCamera.stereoTargetEye = StereoTargetEyeMask.None;
            stageCamera.targetTexture = frameTexture;
            return stageCamera;
        }

        Face BuildFace(Transform parent, Font typeface, int stepIndex)
        {
            Face face = new Face();
            GameObject root = new GameObject("Face", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            face.Root = root;
            face.Rect = root.GetComponent<RectTransform>();
            face.Rect.anchorMin = new Vector2(0.5f, 0.5f);
            face.Rect.anchorMax = new Vector2(0.5f, 0.5f);
            face.Rect.pivot = new Vector2(0.5f, 0.5f);
            face.Rect.sizeDelta = new Vector2(1080f, 1920f);
            face.Group = root.AddComponent<CanvasGroup>();
            face.Group.blocksRaycasts = false;

            Step step = Steps[Mathf.Clamp(stepIndex, 0, Steps.Length - 1)];
            GameObject copy = new GameObject("Copy", typeof(RectTransform));
            copy.transform.SetParent(root.transform, false);
            RectTransform copyRect = copy.GetComponent<RectTransform>();
            copyRect.anchorMin = new Vector2(0.5f, 0.5f);
            copyRect.anchorMax = new Vector2(0.5f, 0.5f);
            copyRect.sizeDelta = new Vector2(1080f, 1920f);
            face.Kicker = CreateText(copy.transform, typeface, (stepIndex + 1).ToString("00"), 28, Title, FontStyle.Normal);
            face.Kicker.alignment = TextAnchor.MiddleLeft;
            face.ClaimSoft = CreateText(copy.transform, typeface, step.Claim, 78, new Color(1f, 1f, 1f, 0f), FontStyle.Bold);
            face.Claim = CreateText(copy.transform, typeface, step.Claim, 78, Title, FontStyle.Bold);
            face.Claim.alignment = TextAnchor.MiddleLeft;
            face.ClaimSoft.alignment = TextAnchor.MiddleLeft;
            face.Line = CreateText(copy.transform, typeface, step.Line, 30, Mist, FontStyle.Normal);
            face.Line.alignment = TextAnchor.MiddleLeft;

            GameObject rim = new GameObject("Rim", typeof(RectTransform));
            rim.transform.SetParent(root.transform, false);
            face.Rim = rim.GetComponent<RectTransform>();
            face.Rim.anchorMin = new Vector2(0.5f, 0.5f);
            face.Rim.anchorMax = new Vector2(0.5f, 0.5f);
            face.Rim.pivot = new Vector2(0.5f, 0.5f);
            Image rimImage = rim.AddComponent<Image>();
            rimImage.sprite = Plate();
            rimImage.type = Image.Type.Sliced;
            rimImage.color = new Color(1f, 1f, 1f, 0.35f);
            rimImage.raycastTarget = false;

            GameObject frame = new GameObject("Frame", typeof(RectTransform));
            frame.transform.SetParent(root.transform, false);
            face.Frame = frame.GetComponent<RectTransform>();
            face.Frame.anchorMin = new Vector2(0.5f, 0.5f);
            face.Frame.anchorMax = new Vector2(0.5f, 0.5f);
            face.Frame.pivot = new Vector2(0.5f, 0.5f);
            Image framePlate = frame.AddComponent<Image>();
            framePlate.sprite = Plate();
            framePlate.type = Image.Type.Sliced;
            framePlate.color = Card;
            framePlate.raycastTarget = false;
            frame.AddComponent<Mask>().showMaskGraphic = true;

            CanvasGroup pictureFade;
            face.Picture = AddPicture(frame.transform, "Picture", out face.PictureRect, out pictureFade);
            pictureFade.blocksRaycasts = false;
            Fill(face, stepIndex);
            return face;
        }

        void Update()
        {
            float half = VisibleHalfHeight(hud != null ? hud : stageCanvas);
            if (Mathf.Abs(half - laidHalf) > 12f)
                Layout(half);

            float step = Mathf.Min(Time.deltaTime, 0.033f);
            motionTime += step;
            if (closing)
            {
                float gone = Ease(motionTime / 0.45f);
                if (rootFade != null)
                    rootFade.alpha = 1f - gone;
                if (hudFade != null)
                    hudFade.alpha = 1f - gone;
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
            if (shown != null && shown.Rect != null)
            {
                shown.Rect.localRotation = Quaternion.Slerp(Quaternion.Euler(-32f, 40f, -14f), Quaternion.identity, along);
                Vector3 from = new Vector3(faceRest.x - 56f, faceRest.y - 40f, -80f);
                Vector3 to = new Vector3(faceRest.x, faceRest.y, 0f);
                shown.Rect.localPosition = Vector3.Lerp(from, to, along);
                float scale = Mathf.Lerp(1.08f, 1f, along);
                shown.Rect.localScale = new Vector3(scale, scale, 1f);
            }

            if (shown != null && shown.Group != null)
                shown.Group.alpha = along;
            if (controlFade != null)
            {
                controlFade.alpha = Ease((motionTime - 0.28f) / 0.4f);
                controlFade.blocksRaycasts = controlFade.alpha > 0.55f;
            }

            if (motionTime < EnterDuration)
                return;

            Rest(shown);
            if (shown != null && shown.Group != null)
                shown.Group.alpha = 1f;
            beat = Beat.Hold;
            motionTime = 0f;
            ready = true;
        }

        void PlayLeave()
        {
            float along = Ease(motionTime / LeaveDuration);
            Tip(along);
            if (along < 1f)
                return;

            index += 1;
            Settle();
            PaintDots();
            beat = Beat.Hold;
            motionTime = 0f;
            ready = true;
        }

        void Tip(float along)
        {
            float t = Mathf.Clamp01(along);
            float side = index % 2 == 0 ? 1f : -1f;
            float leave = Mathf.Clamp01(t / 0.58f);
            float arrive = Mathf.Clamp01((t - 0.4f) / 0.6f);
            float leaveLift = Mathf.Sin(leave * Mathf.PI);
            float arriveLift = Mathf.Sin(arrive * Mathf.PI);

            Quaternion leaveSpin = Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(26f, -48f * side, 12f * side), leave);
            Quaternion leaveWrist = Quaternion.Euler(leaveLift * -10f, leaveLift * 8f * side, leaveLift * 6f * side);
            Vector3 leaveAt = new Vector3(
                faceRest.x + (Mathf.Lerp(0f, 560f, leave) * side),
                faceRest.y + Mathf.Lerp(0f, 80f, leave),
                Mathf.Lerp(0f, 160f, leave));
            Pose(shown, leaveWrist * leaveSpin, leaveAt, Mathf.Lerp(1f, 0.88f, leave));
            if (shown != null && shown.Group != null)
                shown.Group.alpha = 1f - Mathf.SmoothStep(0.28f, 1f, leave);

            Quaternion arriveFrom = Quaternion.Euler(-22f, 44f * side, -10f * side);
            Quaternion arriveSpin = Quaternion.Slerp(arriveFrom, Quaternion.identity, arrive);
            Quaternion arriveWrist = Quaternion.Euler(arriveLift * -8f, arriveLift * 6f * side, arriveLift * 5f * side);
            Vector3 arriveAt = new Vector3(
                faceRest.x + (Mathf.Lerp(-560f, 0f, arrive) * side),
                faceRest.y + Mathf.Lerp(-64f, 0f, arrive),
                Mathf.Lerp(90f, 0f, arrive));
            Pose(next, arriveWrist * arriveSpin, arriveAt, Mathf.Lerp(0.92f, 1f, arrive));
            if (next != null && next.Group != null)
                next.Group.alpha = Mathf.SmoothStep(0.08f, 0.72f, arrive);
        }

        void Pose(Face face, Quaternion rotation, Vector3 travel, float scale)
        {
            if (face == null || face.Rect == null)
                return;

            face.Rect.localRotation = rotation;
            face.Rect.localPosition = travel;
            face.Rect.localScale = new Vector3(scale, scale, 1f);
        }

        void Rest(Face face)
        {
            if (face == null || face.Rect == null)
                return;

            face.Rect.localRotation = Quaternion.identity;
            face.Rect.localPosition = new Vector3(faceRest.x, faceRest.y, 0f);
            face.Rect.localScale = Vector3.one;
        }

        void Settle()
        {
            Fill(shown, index);
            Rest(shown);
            if (shown != null && shown.Group != null)
                shown.Group.alpha = 1f;
            if (next == null)
                return;

            next.Rect.localRotation = Quaternion.identity;
            next.Rect.localScale = Vector3.one;
            if (next.Group != null)
                next.Group.alpha = 0f;
            next.Root.SetActive(false);
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

            Fill(next, index + 1);
            if (next != null)
            {
                float side = index % 2 == 0 ? 1f : -1f;
                next.Root.SetActive(true);
                next.Rect.localRotation = Quaternion.Euler(-22f, 44f * side, -10f * side);
                next.Rect.localPosition = new Vector3(faceRest.x - (560f * side), faceRest.y - 64f, 90f);
                next.Rect.localScale = new Vector3(0.92f, 0.92f, 1f);
                if (next.Group != null)
                    next.Group.alpha = 0f;
            }

            beat = Beat.Leave;
            motionTime = 0f;
        }

        void Fill(Face face, int stepIndex)
        {
            if (face == null || stepIndex < 0 || stepIndex >= Steps.Length)
                return;

            Step step = Steps[stepIndex];
            if (face.Kicker != null)
                face.Kicker.text = (stepIndex + 1).ToString("00");
            if (face.Claim != null)
                face.Claim.text = step.Claim;
            if (face.ClaimSoft != null)
                face.ClaimSoft.text = step.Claim;
            if (face.Line != null)
                face.Line.text = step.Line;

            Texture2D picture = Load(step.Picture);
            if (face.Picture != null)
                face.Picture.texture = picture;
            Fit(face.Picture, face.PictureRect, picture);
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
                dots[i].color = new Color(1f, 1f, 1f, current ? 1f : 0.4f);
                dots[i].rectTransform.sizeDelta = current ? new Vector2(36f, 10f) : new Vector2(10f, 10f);
            }
        }

        void Layout(float half)
        {
            laidHalf = half;
            float top = half - 72f;
            float cardWidth = Mathf.Min(460f, half * 0.72f);
            float cardHeight = cardWidth * 0.72f;
            float cardY = top - 36f - (cardHeight * 0.5f) - 28f;
            frameRest = new Vector2(0f, cardY);
            PlaceFace(shown, half, cardWidth, cardHeight, cardY);
            PlaceFace(next, half, cardWidth, cardHeight, cardY);

            float buttonY = -half + 118f;
            float skipY = -half + 42f;
            float dotsY = buttonY + 78f;

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

        void PlaceFace(Face face, float half, float cardWidth, float cardHeight, float cardY)
        {
            if (face == null)
                return;

            if (face.Rect != null)
            {
                float height = Mathf.Max(1f, face.Rect.sizeDelta.y);
                face.Rect.pivot = new Vector2(0.5f, 0.5f + (cardY / height));
                face.Rect.anchoredPosition = new Vector2(0f, cardY);
                faceRest = face.Rect.anchoredPosition;
            }

            float top = half - 72f;
            Place(face.Kicker.rectTransform, -40f, top, 280f, 40f);
            Place(face.Claim.rectTransform, -20f, cardY - (cardHeight * 0.5f) - 78f, 860f, 150f);
            if (face.ClaimSoft != null && face.Claim != null)
                Place(face.ClaimSoft.rectTransform, -20f, face.Claim.rectTransform.anchoredPosition.y, 860f, 150f);
            if (face.Line != null && face.Claim != null)
                Place(face.Line.rectTransform, -20f, face.Claim.rectTransform.anchoredPosition.y - 120f, 760f, 110f);

            if (face.Frame != null)
            {
                face.Frame.sizeDelta = new Vector2(cardWidth, cardHeight);
                face.Frame.anchoredPosition = frameRest;
            }

            if (face.Rim != null)
            {
                face.Rim.sizeDelta = new Vector2(cardWidth + 18f, cardHeight + 18f);
                face.Rim.anchoredPosition = frameRest;
            }

            Fit(face.Picture, face.PictureRect, face.Picture != null ? face.Picture.texture as Texture2D : null);
        }

        static void SetLayer(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (int i = 0; i < root.childCount; i++)
                SetLayer(root.GetChild(i), layer);
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

        void OnDestroy()
        {
            if (stageCamera != null)
                stageCamera.targetTexture = null;
            if (frameTexture == null)
                return;

            frameTexture.Release();
            Destroy(frameTexture);
            frameTexture = null;
        }

        void Finish()
        {
            Action done = finished;
            finished = null;
            Destroy(gameObject);
            if (done != null)
                done();
        }

        static void SetFade(Text text, Color color, float alpha)
        {
            if (text == null)
                return;

            color.a = Mathf.Clamp01(alpha);
            text.color = color;
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
