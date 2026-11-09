using System;
using Capstone.Session;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Capstone.Placement
{
    /// <summary>
    /// Animation one plays through: a white ball bounces on each word, then covers the page.
    /// Animation two is the blue circle. It stays until the circle itself is tapped.
    /// That tap asks for a consent code. A matching code starts the session. Any other code asks for another attempt.
    /// </summary>
    public sealed class OpeningSequence : MonoBehaviour
    {
        static readonly string[] Line =
        {
            "See", "it", "on", "your", "wall", "at", "its", "real", "size,", "and", "ask", "one", "thing."
        };

        static readonly Color Paper = Color.white;
        static readonly Color Ink = new Color(0.114f, 0.114f, 0.122f, 1f);
        static readonly Color Slate = new Color(0.439f, 0.439f, 0.439f, 1f);
        static readonly Color BlueTop = new Color(0.22f, 0.52f, 1f, 1f);
        static readonly Color BlueDeep = new Color(0.04f, 0.22f, 0.86f, 1f);
        const float Gravity = 10800f;
        const float Centre = 0f;
        const float BallSize = 58f;

        Action<string, string> finished;
        Image paper;
        Image field;
        Image shadow;
        RectTransform ball;
        RectTransform orb;
        RectTransform ribbonA;
        RectTransform ribbonB;
        CanvasGroup orbFade;
        bool recordingCode;
        GameObject consentPanel;
        Text codeLine;
        Text consentHint;
        string typedCode = string.Empty;
        string consentCode = string.Empty;
        TouchScreenKeyboard touchKeyboard;
        Font cardFont;
        Transform canvasRoot;
        string chosenId;
        Text[] words;
        float velocity;
        float horizontalVelocity;
        float apex;
        float hop;
        int nextWord;
        bool risen;
        bool towardWords;
        bool aimed;
        bool covering;
        float coverTime;
        Vector2 coverOrigin;
        bool resting;
        float restTime;
        bool done;
        bool staged;
        float stageTime;
        RawImage work;
        CanvasGroup workFade;
        CanvasGroup claimFade;

        public static void Play(Font typeface, Action<string, string> onFinished)
        {
            GameObject host = new GameObject("Opening");
            OpeningSequence sequence = host.AddComponent<OpeningSequence>();
            sequence.finished = onFinished;
            sequence.Build(typeface);
        }

        void Build(Font typeface)
        {
            GameObject canvasObject = new GameObject("Opening canvas");
            canvasRoot = canvasObject.transform;
            cardFont = typeface;
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 800;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0f;
            canvasObject.AddComponent<GraphicRaycaster>();

            paper = CreateImage(canvasObject.transform, "Paper", Paper, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            paper.raycastTarget = false;

            field = CreateImage(canvasObject.transform, "Blue field", new Color(1f, 1f, 1f, 0f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            field.sprite = VerticalGradient(BlueTop, BlueDeep);
            field.raycastTarget = false;

            float half = VisibleHalfHeight(canvas);
            apex = half * 0.9f;
            hop = half * 0.42f;
            words = CreateWords(canvasObject.transform, typeface);
            BuildGallery(canvasObject.transform, typeface);

            shadow = CreateImage(
                canvasObject.transform,
                "Shadow",
                new Color(0.02f, 0.08f, 0.28f, 0.2f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -18f),
                new Vector2(70f, 18f));
            shadow.sprite = SoftEllipse();
            shadow.raycastTarget = false;
            shadow.color = new Color(shadow.color.r, shadow.color.g, shadow.color.b, 0f);

            Image ballImage = CreateImage(
                canvasObject.transform,
                "Ball",
                Color.white,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, half + BallSize),
                new Vector2(BallSize, BallSize));
            ballImage.sprite = BallSprite();
            ballImage.color = Ink;
            ballImage.raycastTarget = false;
            ball = ballImage.rectTransform;
            ball.gameObject.SetActive(false);

            GameObject orbObject = new GameObject("Glass body", typeof(RectTransform));
            orbObject.transform.SetParent(canvasObject.transform, false);
            orb = orbObject.GetComponent<RectTransform>();
            orb.anchorMin = new Vector2(0.5f, 0.5f);
            orb.anchorMax = new Vector2(0.5f, 0.5f);
            orb.pivot = new Vector2(0.5f, 0.5f);
            orb.sizeDelta = new Vector2(640f, 640f);
            orb.anchoredPosition = new Vector2(0f, 80f);
            orb.localScale = Vector3.one * 0.1f;
            ribbonA = Ribbon(orb, "Ribbon", 0);
            ribbonB = Ribbon(orb, "Inner ribbon", 1);
            Image glass = CreateImage(orb, "Glass", Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            glass.sprite = GlassShell();
            glass.raycastTarget = true;
            glass.alphaHitTestMinimumThreshold = 0f;
            Button bodyTap = glass.gameObject.AddComponent<Button>();
            bodyTap.targetGraphic = glass;
            bodyTap.transition = Selectable.Transition.None;
            bodyTap.onClick.AddListener(OpenSession);
            orbFade = orbObject.AddComponent<CanvasGroup>();
            orb.gameObject.SetActive(false);
            velocity = 0f;
        }

        void Update()
        {
            if (recordingCode && !done)
                ReadCodeKeys();

            if (done || recordingCode)
                return;

            float step = Mathf.Min(Time.deltaTime, 0.033f);
            if (!staged)
            {
                StageWork(step);
                return;
            }
            if (covering)
            {
                Cover(step);
                return;
            }

            if (resting)
            {
                Rest(step);
                return;
            }

            velocity -= Gravity * step;
            Vector2 position = ball.anchoredPosition;
            position.y += velocity * step;
            if (aimed)
                position.x += horizontalVelocity * step;
            ball.anchoredPosition = position;
            ball.localScale = Vector3.Lerp(ball.localScale, Vector3.one, 1f - Mathf.Exp(-28f * step));
            PlaceBounceShadow();

            if (!towardWords)
            {
                if (!risen && velocity < 0f && position.y <= Centre)
                {
                    Kick(0f, Centre, apex);
                    risen = true;
                    return;
                }

                if (!risen || velocity > 0f)
                    return;

                towardWords = true;
            }

            if (!aimed && velocity <= 0f)
            {
                Aim(nextWord);
                aimed = true;
            }

            if (!aimed || velocity >= 0f || nextWord >= words.Length)
                return;

            if (position.y > Centre)
                return;

            words[nextWord].color = Ink;
            if (nextWord == words.Length - 1)
            {
                covering = true;
                coverTime = 0f;
                coverOrigin = new Vector2(words[nextWord].rectTransform.anchoredPosition.x, Centre);
                ball.anchoredPosition = coverOrigin;
                ball.localScale = Vector3.one;
                return;
            }

            float landedX = words[nextWord].rectTransform.anchoredPosition.x;
            nextWord++;
            Kick(landedX, Centre, hop);
        }

        void StageWork(float step)
        {
            stageTime += step;
            float along = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(stageTime / 1.05f));
            if (workFade != null)
                workFade.alpha = along;
            if (work != null)
            {
                float scale = Mathf.Lerp(0.82f, 1f, along);
                work.rectTransform.localScale = new Vector3(scale, scale, 1f);
                work.rectTransform.anchoredPosition = Vector2.Lerp(new Vector2(0f, 220f), new Vector2(0f, 340f), along);
            }

            if (claimFade != null)
                claimFade.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((stageTime - 0.28f) / 0.7f));
            if (along < 1f)
                return;

            staged = true;
            if (ball != null)
                ball.gameObject.SetActive(true);
        }

        void BuildGallery(Transform parent, Font typeface)
        {
            Text kicker = CreateWord(parent, typeface, "Reclining figure", 36);
            kicker.color = Ink;
            kicker.rectTransform.anchoredPosition = new Vector2(0f, 620f);
            kicker.rectTransform.sizeDelta = new Vector2(900f, 52f);
            Text claim = CreateWord(parent, typeface, "On your wall.", 72);
            claim.color = Ink;
            claim.rectTransform.anchoredPosition = new Vector2(0f, 530f);
            claim.rectTransform.sizeDelta = new Vector2(900f, 90f);
            Text line = CreateWord(parent, typeface, "At the size it was made.", 32);
            line.fontStyle = FontStyle.Normal;
            line.color = Slate;
            line.rectTransform.anchoredPosition = new Vector2(0f, 460f);
            line.rectTransform.sizeDelta = new Vector2(900f, 48f);

            GameObject claims = new GameObject("Claims", typeof(RectTransform));
            claims.transform.SetParent(parent, false);
            RectTransform claimsRect = claims.GetComponent<RectTransform>();
            claimsRect.anchorMin = new Vector2(0.5f, 0.5f);
            claimsRect.anchorMax = new Vector2(0.5f, 0.5f);
            claimsRect.sizeDelta = new Vector2(1080f, 1920f);
            kicker.transform.SetParent(claims.transform, true);
            claim.transform.SetParent(claims.transform, true);
            line.transform.SetParent(claims.transform, true);
            claimFade = claims.AddComponent<CanvasGroup>();
            claimFade.alpha = 0f;

            Texture2D picture = Resources.Load<Texture2D>("TrialPieces/reclining");
            if (picture == null)
                return;

            GameObject stage = new GameObject("Work", typeof(RectTransform));
            stage.transform.SetParent(parent, false);
            work = stage.AddComponent<RawImage>();
            work.texture = picture;
            work.raycastTarget = false;
            float aspect = picture.width / (float)Mathf.Max(1, picture.height);
            float width = 860f;
            float height = width / aspect;
            if (height > 520f)
            {
                height = 520f;
                width = height * aspect;
            }

            RectTransform stageRect = stage.GetComponent<RectTransform>();
            stageRect.anchorMin = new Vector2(0.5f, 0.5f);
            stageRect.anchorMax = new Vector2(0.5f, 0.5f);
            stageRect.pivot = new Vector2(0.5f, 0.5f);
            stageRect.sizeDelta = new Vector2(width, height);
            stageRect.anchoredPosition = new Vector2(0f, 220f);
            stageRect.localScale = new Vector3(0.82f, 0.82f, 1f);
            workFade = stage.AddComponent<CanvasGroup>();
            workFade.alpha = 0f;
        }

        void Aim(int index)
        {
            float drop = ball.anchoredPosition.y - Centre;
            float wordX = words[index].rectTransform.anchoredPosition.x;
            if (drop < 12f)
            {
                horizontalVelocity = 0f;
                return;
            }

            float a = 0.5f * Gravity;
            float b = -velocity;
            float disc = (b * b) + (2f * Gravity * drop);
            float time = (-b + Mathf.Sqrt(Mathf.Max(0f, disc))) / (2f * a);
            if (time < 0.04f)
                time = 0.04f;
            horizontalVelocity = (wordX - ball.anchoredPosition.x) / time;
        }

        void Kick(float x, float height, float rise)
        {
            ball.anchoredPosition = new Vector2(x, height);
            velocity = Mathf.Sqrt(2f * Gravity * rise);
            horizontalVelocity = 0f;
            aimed = false;
            ball.localScale = new Vector3(1.38f, 0.62f, 1f);
        }

        void PlaceBounceShadow()
        {
            float height = Mathf.Max(0f, ball.anchoredPosition.y - Centre);
            float travel = Mathf.Clamp01(height / apex);
            shadow.rectTransform.anchoredPosition = new Vector2(ball.anchoredPosition.x, -16f);
            shadow.rectTransform.sizeDelta = new Vector2(70f, 18f);
            float width = Mathf.Lerp(1.1f, 0.4f, travel);
            shadow.rectTransform.localScale = new Vector3(width, Mathf.Lerp(1f, 0.55f, travel), 1f);
            Color color = shadow.color;
            color.a = Mathf.Lerp(0.22f, 0.04f, travel);
            shadow.color = color;
        }

        void Cover(float step)
        {
            coverTime += step;
            float t = Mathf.Clamp01(coverTime / 0.95f);
            float rush = t * t;
            ball.anchoredPosition = Vector2.Lerp(coverOrigin, Vector2.zero, rush);
            float scale = Mathf.Lerp(1f, 56f, rush);
            ball.localScale = new Vector3(scale, scale, 1f);
            if (workFade != null)
                workFade.alpha = 1f - rush;
            if (claimFade != null)
                claimFade.alpha = 1f - rush;
            for (int i = 0; i < words.Length; i++)
            {
                Color color = words[i].color;
                color.a = 1f - rush;
                words[i].color = color;
            }

            Color shade = shadow.color;
            shade.a = Mathf.Lerp(0.2f, 0f, rush);
            shadow.color = shade;
            if (t < 1f)
                return;

            ArriveOnWhite();
        }

        void ArriveOnWhite()
        {
            covering = false;
            resting = true;
            restTime = 0f;
            if (ball != null)
                ball.gameObject.SetActive(false);
            if (field != null)
                field.color = new Color(1f, 1f, 1f, 0f);
            if (words != null)
            {
                for (int i = 0; i < words.Length; i++)
                {
                    Color color = words[i].color;
                    color.a = 0f;
                    words[i].color = color;
                }
            }

            if (shadow != null)
            {
                Color shade = shadow.color;
                shade.a = 0f;
                shadow.color = shade;
            }
        }

        void Rest(float step)
        {
            restTime += step;
            float arrive = Mathf.Clamp01((restTime - 0.12f) / 0.8f);
            float eased = 1f - ((1f - arrive) * (1f - arrive));
            if (!orb.gameObject.activeSelf)
            {
                orb.gameObject.SetActive(true);
                orb.anchoredPosition = new Vector2(0f, 40f);
                orb.localScale = Vector3.one * 0.08f;
            }

            float breathe = 1f + (Mathf.Sin(restTime * 1.4f) * 0.018f);
            orb.localScale = Vector3.one * Mathf.Lerp(0.08f, 1f, eased) * breathe;
            float lift = Mathf.Sin(restTime * 1.15f) * 22f;
            Vector2 home = new Vector2(Mathf.Sin(restTime * 0.7f) * 10f, 90f + lift);
            orb.anchoredPosition = Vector2.Lerp(new Vector2(0f, 40f), home, eased);
            ribbonA.localRotation = Quaternion.Euler(0f, 0f, restTime * 26f);
            ribbonB.localRotation = Quaternion.Euler(0f, 0f, -restTime * 17f);

            float height = Mathf.InverseLerp(-22f, 22f, lift);
            shadow.rectTransform.sizeDelta = new Vector2(420f, 90f);
            shadow.rectTransform.anchoredPosition = new Vector2(orb.anchoredPosition.x, -250f);
            shadow.rectTransform.localScale = new Vector3(Mathf.Lerp(1.05f, 0.72f, height), 1f, 1f);
            Color shade = shadow.color;
            shade.a = Mathf.Lerp(0f, 0.18f, eased) * Mathf.Lerp(1f, 0.55f, height);
            shadow.color = shade;
        }

        void OpenSession()
        {
            if (!resting || done || recordingCode)
                return;

            resting = false;
            if (orb != null)
                orb.gameObject.SetActive(false);
            if (ball != null)
                ball.gameObject.SetActive(false);
            if (paper != null)
                paper.color = Color.white;
            if (field != null)
                field.gameObject.SetActive(false);
            if (shadow != null)
                shadow.gameObject.SetActive(false);
            if (words != null)
            {
                for (int i = 0; i < words.Length; i++)
                {
                    if (words[i] != null)
                        words[i].gameObject.SetActive(false);
                }
            }

            recordingCode = true;
            typedCode = string.Empty;
            ShowConsent();
            if (!Application.isEditor)
                touchKeyboard = TouchScreenKeyboard.Open(string.Empty, TouchScreenKeyboardType.ASCIICapable, false, false, false, false, "Consent code");
        }

        void ShowConsent()
        {
            Transform root = canvasRoot != null ? canvasRoot : (paper != null ? paper.transform.parent : null);
            if (paper != null)
                paper.color = Color.white;

            Image backCard = CreateImage(
                root,
                "Back card",
                new Color(0.961f, 0.961f, 0.969f, 1f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(150f, -30f),
                new Vector2(640f, 860f));
            backCard.raycastTarget = false;
            backCard.transform.SetAsLastSibling();
            Image blueCircle = CreateImage(
                backCard.transform,
                "Blue circle",
                new Color(0f, 0.443f, 0.890f, 1f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(90f, 40f),
                new Vector2(340f, 340f));
            blueCircle.sprite = SolidCircle();
            blueCircle.raycastTarget = false;

            Image card = CreateImage(
                root,
                "Code card",
                Color.white,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-90f, 50f),
                new Vector2(680f, 820f));
            consentPanel = card.gameObject;
            card.transform.SetAsLastSibling();

            Text heading = CreateWord(card.transform, cardFont, "Consent code", 56);
            heading.color = new Color(0.114f, 0.114f, 0.122f, 1f);
            heading.alignment = TextAnchor.MiddleLeft;
            heading.rectTransform.anchoredPosition = new Vector2(0f, 280f);
            heading.rectTransform.sizeDelta = new Vector2(560f, 90f);

            Image well = CreateImage(
                card.transform,
                "Code",
                new Color(0.9f, 0.95f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 80f),
                new Vector2(520f, 110f));
            well.raycastTarget = false;
            codeLine = CreateWord(well.transform, cardFont, "Type your code", 36);
            codeLine.fontStyle = FontStyle.Italic;
            codeLine.color = new Color(0.4f, 0.5f, 0.65f, 1f);
            codeLine.alignment = TextAnchor.MiddleLeft;
            codeLine.rectTransform.anchoredPosition = new Vector2(16f, 0f);
            codeLine.rectTransform.sizeDelta = new Vector2(470f, 80f);

            consentHint = CreateWord(card.transform, cardFont, string.Empty, 30);
            consentHint.color = new Color(0.6f, 0.1f, 0.18f, 1f);
            consentHint.alignment = TextAnchor.MiddleLeft;
            consentHint.rectTransform.anchoredPosition = new Vector2(0f, -10f);
            consentHint.rectTransform.sizeDelta = new Vector2(520f, 50f);

            Image enter = CreateImage(
                card.transform,
                "Enter",
                new Color(0f, 0.443f, 0.890f, 1f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(180f, -250f),
                new Vector2(160f, 160f));
            enter.sprite = SolidCircle();
            Button enterButton = enter.gameObject.AddComponent<Button>();
            enterButton.targetGraphic = enter;
            enterButton.onClick.AddListener(SubmitConsentCode);
            Text enterLabel = CreateWord(enter.transform, cardFont, "Enter", 28);
            enterLabel.color = Color.white;
            enterLabel.alignment = TextAnchor.MiddleCenter;
            enterLabel.raycastTarget = false;
            enterLabel.rectTransform.anchoredPosition = Vector2.zero;
            enterLabel.rectTransform.sizeDelta = new Vector2(140f, 60f);
        }

        static Sprite SolidCircle()
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            float centre = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - centre) / centre;
                    float dy = (y - centre) / centre;
                    bool inside = (dx * dx) + (dy * dy) <= 1f;
                    texture.SetPixel(x, y, inside ? Color.white : new Color(1f, 1f, 1f, 0f));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }

        void ReadCodeKeys()
        {
            if (touchKeyboard != null)
            {
                typedCode = touchKeyboard.text ?? string.Empty;
                PaintCode();
                if (touchKeyboard.status == TouchScreenKeyboard.Status.Done)
                    SubmitConsentCode();
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                SubmitConsentCode();
                return;
            }

            if (keyboard.backspaceKey.wasPressedThisFrame)
            {
                if (typedCode.Length > 0)
                    typedCode = typedCode.Substring(0, typedCode.Length - 1);
                PaintCode();
                return;
            }

            if (typedCode.Length >= 12)
                return;

            string next = TypedCharacter(keyboard);
            if (next.Length == 0)
                return;

            typedCode += next;
            PaintCode();
        }

        static string TypedCharacter(Keyboard keyboard)
        {
            for (int letter = (int)Key.A; letter <= (int)Key.Z; letter++)
            {
                Key key = (Key)letter;
                if (keyboard[key].wasPressedThisFrame)
                    return key.ToString();
            }

            return string.Empty;
        }

        void PaintCode()
        {
            if (codeLine == null)
                return;

            if (typedCode.Length == 0)
            {
                codeLine.text = "Consent code";
                codeLine.fontStyle = FontStyle.Italic;
                codeLine.color = new Color(0.45f, 0.55f, 0.7f, 0.95f);
                return;
            }

            codeLine.text = typedCode;
            codeLine.fontStyle = FontStyle.Bold;
            codeLine.color = new Color(0.08f, 0.16f, 0.32f, 1f);
        }

        void SubmitConsentCode()
        {
            if (done)
                return;

            BuyerSession started;
            if (!BuyerSession.TryStart(typedCode, out started))
            {
                typedCode = string.Empty;
                PaintCode();
                if (consentHint != null)
                    consentHint.text = "Try again.";
                if (touchKeyboard != null)
                    touchKeyboard.text = string.Empty;
                return;
            }

            consentCode = started.ParticipantCode;
            if (consentPanel != null)
                consentPanel.SetActive(false);
            Finish();
        }

        void Finish()
        {
            if (done)
                return;

            done = true;
            Action<string, string> complete = finished;
            string id = chosenId;
            string code = consentCode;
            finished = null;
            if (complete != null)
                complete(id, code);
            Destroy(gameObject);
        }

        static float VisibleHalfHeight(Canvas canvas)
        {
            Canvas.ForceUpdateCanvases();
            float scale = canvas.scaleFactor;
            if (scale < 0.01f)
                scale = 1f;

            float height = canvas.pixelRect.height / scale;
            if (height < 400f)
                height = 1920f;
            return height * 0.5f;
        }

        static Text[] CreateWords(Transform parent, Font typeface)
        {
            const float maxWidth = 980f;
            int fontSize = 46;
            float gap = 16f;
            Text[] placed = new Text[Line.Length];
            for (int i = 0; i < Line.Length; i++)
                placed[i] = CreateWord(parent, typeface, Line[i], fontSize);

            float[] widths = new float[Line.Length];
            for (int attempt = 0; attempt < 20; attempt++)
            {
                float width = gap * (Line.Length - 1);
                for (int i = 0; i < Line.Length; i++)
                {
                    placed[i].fontSize = fontSize;
                    widths[i] = placed[i].preferredWidth;
                    if (widths[i] < 8f)
                        widths[i] = Line[i].Length * fontSize * 0.56f;
                    width += widths[i];
                }

                if (width <= maxWidth || fontSize <= 30)
                    break;

                if (gap > 10f)
                    gap -= 2f;
                else
                    fontSize -= 2;
            }

            float total = gap * (Line.Length - 1);
            for (int i = 0; i < Line.Length; i++)
                total += widths[i];

            float x = -total * 0.5f;
            for (int i = 0; i < Line.Length; i++)
            {
                RectTransform rect = placed[i].rectTransform;
                rect.sizeDelta = new Vector2(widths[i] + 4f, fontSize + 18f);
                rect.anchoredPosition = new Vector2(x + (widths[i] * 0.5f), -42f);
                x += widths[i] + gap;
            }

            return placed;
        }

        static Text CreateWord(Transform parent, Font typeface, string message, int fontSize)
        {
            GameObject host = new GameObject(message);
            host.transform.SetParent(parent, false);
            Text text = host.AddComponent<Text>();
            text.font = typeface;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 1f, 1f, 0f);
            text.raycastTarget = false;
            text.text = message;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(240f, 72f);
            return text;
        }

        static RectTransform Ribbon(Transform parent, string name, int band)
        {
            Image image = CreateImage(parent, name, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            image.sprite = GlassRibbon(band);
            image.raycastTarget = false;
            return image.rectTransform;
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

        static Sprite BallSprite()
        {
            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            float centre = (size - 1) * 0.5f;
            float radius = centre - 1.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - centre;
                    float dy = y - centre;
                    float distance = Mathf.Sqrt((dx * dx) + (dy * dy));
                    float edge = Mathf.Clamp01((radius + 1.4f - distance) / 2.4f);
                    if (edge <= 0f)
                    {
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, 0f));
                        continue;
                    }

                    float nx = dx / radius;
                    float ny = dy / radius;
                    float light = Mathf.Clamp01(0.72f + (ny * 0.22f) - (nx * 0.08f));
                    float highlight = Mathf.Clamp01(1f - (Mathf.Sqrt(((nx + 0.28f) * (nx + 0.28f)) + ((ny - 0.34f) * (ny - 0.34f))) / 0.42f));
                    highlight = highlight * highlight;
                    Color color = Color.Lerp(new Color(0.78f, 0.88f, 1f, 1f), Color.white, light);
                    color = Color.Lerp(color, Color.white, highlight * 0.95f);
                    color.a = edge;
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
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
                    float body = Mathf.Clamp01(1f - distance) * 0.55f;
                    float highlight = Mathf.Clamp01(1f - (Mathf.Sqrt(((dx + 0.32f) * (dx + 0.32f)) + ((dy - 0.38f) * (dy - 0.38f))) / 0.42f));
                    highlight = highlight * highlight;
                    Color color = Color.Lerp(new Color(0.2f, 0.48f, 0.95f, 1f), new Color(0.75f, 0.88f, 1f, 1f), highlight);
                    color.a = Mathf.Clamp01((body + (rim * 0.9f) + (highlight * 0.35f)) * edge);
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
                ? new Color(0.45f, 0.72f, 1f, 1f)
                : new Color(0.12f, 0.38f, 0.92f, 1f);
            Color outer = band == 0
                ? new Color(0.7f, 0.86f, 1f, 1f)
                : new Color(0.28f, 0.58f, 1f, 1f);
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
                    float alpha = across * sweep * (band == 0 ? 0.34f : 0.26f);
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
