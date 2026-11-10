using System;
using Capstone.Session;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Capstone.Placement
{
    /// <summary>
    /// A cube turns through the works, one face at a time, then the glass circle rests in that place.
    /// A tap on the circle asks for a consent code. A matching code starts the session. Any other code asks for another attempt.
    /// </summary>
    public sealed class OpeningSequence : MonoBehaviour
    {
        static readonly string[] Faces =
        {
            "TrialPieces/reclining",
            "TrialPieces/folded",
            "TrialPieces/green-dress",
            "TrialPieces/hands"
        };

        static readonly Color Paper = Color.black;
        static readonly Color Mount = new Color(0.96f, 0.95f, 0.93f, 1f);
        static readonly Color ActionBlue = new Color(0f, 102f / 255f, 204f / 255f, 1f);
        const int StageLayer = 3;
        const float CircleSize = 640f;
        const float MoveDamping = 1f;
        const float MoveResponse = 0.4f;
        const float TurnResponse = 0.55f;
        const float Hold = 0.85f;

        Action<string, string> finished;
        Image paper;
        RectTransform orb;
        CanvasGroup orbFade;
        CanvasGroup stageFade;
        Camera stageCamera;
        RenderTexture frameTexture;
        Transform cube;
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
        float yaw;
        float yawVelocity;
        float yawTarget;
        int shownFace;
        float hold;
        bool circle;
        float orbScale = 0.16f;
        float orbScaleVel;
        float orbPress = 1f;
        float orbPressVel;
        float orbPressTarget = 1f;
        bool resting;
        bool done;

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

            RawImage stage = CreateStage(canvasObject.transform);
            stage.texture = StageCamera();
            stageFade = stage.gameObject.AddComponent<CanvasGroup>();
            stageFade.blocksRaycasts = false;
            cube = BuildCube();

            GameObject orbObject = new GameObject("Glass body", typeof(RectTransform));
            orbObject.transform.SetParent(canvasObject.transform, false);
            orb = orbObject.GetComponent<RectTransform>();
            orb.anchorMin = new Vector2(0.5f, 0.5f);
            orb.anchorMax = new Vector2(0.5f, 0.5f);
            orb.pivot = new Vector2(0.5f, 0.5f);
            orb.sizeDelta = new Vector2(CircleSize, CircleSize);
            orb.anchoredPosition = Vector2.zero;
            orb.localScale = Vector3.one * 0.16f;
            Image glass = CreateImage(orb, "Glass", Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            glass.sprite = GlassShell();
            glass.raycastTarget = true;
            glass.alphaHitTestMinimumThreshold = 0f;
            Button bodyTap = glass.gameObject.AddComponent<Button>();
            bodyTap.targetGraphic = glass;
            bodyTap.transition = Selectable.Transition.None;
            bodyTap.onClick.AddListener(OpenSession);
            EventTrigger press = glass.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener(_ => orbPressTarget = 0.97f);
            press.triggers.Add(down);
            EventTrigger.Entry up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            up.callback.AddListener(_ => orbPressTarget = 1f);
            press.triggers.Add(up);
            EventTrigger.Entry leave = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            leave.callback.AddListener(_ => orbPressTarget = 1f);
            press.triggers.Add(leave);
            orbFade = orbObject.AddComponent<CanvasGroup>();
            orbFade.alpha = 0f;
            orb.gameObject.SetActive(false);
        }

        void Update()
        {
            if (recordingCode && !done)
                ReadCodeKeys();

            if (done || recordingCode)
                return;

            float step = Mathf.Min(Time.deltaTime, 0.033f);
            if (circle)
                RevealCircle(step);
            else if (resting)
                Rest(step);
            else
                TurnCube(step);
        }

        void TurnCube(float step)
        {
            Spring(ref yaw, ref yawVelocity, yawTarget, MoveDamping, TurnResponse, step);
            if (cube != null)
                cube.localRotation = Quaternion.Euler(0f, yaw, 0f);

            if (Mathf.Abs(Mathf.DeltaAngle(yaw, yawTarget)) > 0.6f || Mathf.Abs(yawVelocity) > 12f)
            {
                hold = 0f;
                return;
            }

            hold += step;
            if (hold < Hold)
                return;

            if (shownFace >= Faces.Length - 1)
            {
                circle = true;
                return;
            }

            shownFace++;
            yawTarget += 90f;
            hold = 0f;
        }

        void RevealCircle(float step)
        {
            if (!orb.gameObject.activeSelf)
                orb.gameObject.SetActive(true);

            Spring(ref orbScale, ref orbScaleVel, 1f, MoveDamping, MoveResponse, step);
            Spring(ref orbPress, ref orbPressVel, orbPressTarget, MoveDamping, 0.12f, step);
            orb.anchoredPosition = Vector2.zero;
            orb.localScale = Vector3.one * orbScale * orbPress;
            float opened = Mathf.Clamp01((orbScale - 0.16f) / 0.84f);
            orbFade.alpha = opened;
            if (stageFade != null)
                stageFade.alpha = 1f - opened;
            if (opened < 0.98f || Mathf.Abs(orbScaleVel) > 0.15f)
                return;

            circle = false;
            resting = true;
            orbFade.alpha = 1f;
            if (stageFade != null)
                stageFade.alpha = 0f;
            if (stageCamera != null)
                stageCamera.enabled = false;
        }

        void Rest(float step)
        {
            Spring(ref orbScale, ref orbScaleVel, 1f, MoveDamping, MoveResponse, step);
            Spring(ref orbPress, ref orbPressVel, orbPressTarget, MoveDamping, 0.12f, step);
            orb.anchoredPosition = Vector2.zero;
            orb.localScale = Vector3.one * orbScale * orbPress;
        }

        RenderTexture StageCamera()
        {
            int width = Screen.width;
            int height = Screen.height;
            if (width < 200 || height < 200)
            {
                width = 1080;
                height = 1920;
            }
            frameTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            frameTexture.antiAliasing = 1;
            frameTexture.Create();

            GameObject cameraObject = new GameObject("Cube camera");
            cameraObject.transform.SetParent(transform, false);
            stageCamera = cameraObject.AddComponent<Camera>();
            stageCamera.clearFlags = CameraClearFlags.SolidColor;
            stageCamera.backgroundColor = Color.black;
            stageCamera.cullingMask = 1 << StageLayer;
            stageCamera.depth = 80f;
            stageCamera.orthographic = false;
            stageCamera.fieldOfView = 32f;
            stageCamera.nearClipPlane = 0.05f;
            stageCamera.farClipPlane = 40f;
            stageCamera.allowHDR = false;
            stageCamera.allowMSAA = false;
            stageCamera.stereoTargetEye = StereoTargetEyeMask.None;
            stageCamera.targetTexture = frameTexture;
            stageCamera.transform.position = new Vector3(3.95f, 3.32f, -5.58f);
            stageCamera.transform.LookAt(Vector3.zero);
            float aspect = width / (float)height;
            const float phoneAspect = 0.46f;
            if (aspect < phoneAspect)
            {
                float phoneHalf = Mathf.Tan(16f * Mathf.Deg2Rad) * phoneAspect;
                stageCamera.fieldOfView = 2f * Mathf.Atan(phoneHalf / aspect) * Mathf.Rad2Deg;
            }
            return frameTexture;
        }

        Transform BuildCube()
        {
            GameObject root = new GameObject("Rotating cube");
            root.transform.SetParent(transform, false);
            root.transform.localScale = Vector3.one * 1.15f;
            AddFace(root.transform, new Vector3(0f, 0f, -0.5f), Quaternion.Euler(0f, 180f, 0f), Faces[0]);
            AddFace(root.transform, new Vector3(0.5f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), Faces[1]);
            AddFace(root.transform, new Vector3(0f, 0f, 0.5f), Quaternion.Euler(0f, 0f, 0f), Faces[2]);
            AddFace(root.transform, new Vector3(-0.5f, 0f, 0f), Quaternion.Euler(0f, -90f, 0f), Faces[3]);
            AddFace(root.transform, new Vector3(0f, 0.5f, 0f), Quaternion.Euler(-90f, 180f, 0f), Faces[0]);
            AddFace(root.transform, new Vector3(0f, -0.5f, 0f), Quaternion.Euler(90f, 0f, 0f), null);
            SetLayer(root.transform);
            return root.transform;
        }

        void AddFace(Transform parent, Vector3 position, Quaternion rotation, string picture)
        {
            GameObject face = new GameObject("Face");
            face.transform.SetParent(parent, false);
            face.transform.localPosition = position;
            face.transform.localRotation = rotation;
            AddQuad(face.transform, "Mount", null, Mount, 1.012f, 0f);
            if (picture == null)
                return;

            Texture2D art = Resources.Load<Texture2D>(picture);
            if (art == null)
                return;

            float wide = art.width;
            float tall = art.height;
            float longest = Mathf.Max(wide, tall);
            float fitted = 0.78f / longest;
            AddQuad(face.transform, "Work", art, Color.white, new Vector2(wide * fitted, tall * fitted), 0.012f);
        }

        static void AddQuad(Transform parent, string name, Texture texture, Color color, float size, float outward)
        {
            AddQuad(parent, name, texture, color, new Vector2(size, size), outward);
        }

        static void AddQuad(Transform parent, string name, Texture texture, Color color, Vector2 size, float outward)
        {
            GameObject quad = new GameObject(name);
            MeshFilter filter = quad.AddComponent<MeshFilter>();
            filter.sharedMesh = OutwardQuad();
            MeshRenderer renderer = quad.AddComponent<MeshRenderer>();
            quad.transform.SetParent(parent, false);
            quad.transform.localPosition = new Vector3(0f, 0f, outward);
            quad.transform.localRotation = Quaternion.identity;
            quad.transform.localScale = new Vector3(size.x, size.y, 1f);
            renderer.sharedMaterial = FaceMaterial(texture, color);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        static Mesh OutwardQuad()
        {
            if (outwardQuad != null)
                return outwardQuad;

            outwardQuad = new Mesh { name = "Outward quad" };
            outwardQuad.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f)
            };
            outwardQuad.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            };
            outwardQuad.triangles = new[] { 0, 1, 2, 3, 4, 5 };
            outwardQuad.RecalculateNormals();
            return outwardQuad;
        }

        static Mesh outwardQuad;

        static Material FaceMaterial(Texture texture, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Texture");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            Material material = new Material(shader);
            material.color = color;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (texture == null)
                return material;

            material.mainTexture = texture;
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);
            return material;
        }

        void SetLayer(Transform root)
        {
            root.gameObject.layer = StageLayer;
            for (int i = 0; i < root.childCount; i++)
                SetLayer(root.GetChild(i));
        }

        static RawImage CreateStage(Transform parent)
        {
            GameObject host = new GameObject("Cube view", typeof(RectTransform));
            host.transform.SetParent(parent, false);
            RawImage image = host.AddComponent<RawImage>();
            image.color = Color.white;
            image.raycastTarget = false;
            Stretch(image.rectTransform);
            return image;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void Spring(ref float value, ref float velocity, float target, float dampingRatio, float response, float step)
        {
            float omega = (2f * Mathf.PI) / Mathf.Max(0.05f, response);
            float stiffness = omega * omega;
            float damping = 2f * dampingRatio * omega;
            float accel = (stiffness * (target - value)) - (damping * velocity);
            velocity += accel * step;
            value += velocity * step;
        }

        void OpenSession()
        {
            if (!resting || done || recordingCode)
                return;

            resting = false;
            if (orb != null)
                orb.gameObject.SetActive(false);
            if (stageFade != null)
                stageFade.alpha = 0f;
            if (stageCamera != null)
                stageCamera.enabled = false;
            if (cube != null)
                cube.gameObject.SetActive(false);
            if (paper != null)
                paper.color = new Color(0.07f, 0.18f, 0.42f, 1f);

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
                paper.color = new Color(0.86f, 0.93f, 1f, 1f);

            Image backCard = CreateImage(
                root,
                "Back card",
                new Color(0.78f, 0.9f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(150f, -30f),
                new Vector2(640f, 860f));
            backCard.raycastTarget = false;
            backCard.transform.SetAsLastSibling();
            Image blueCircle = CreateImage(
                backCard.transform,
                "Blue circle",
                new Color(0.35f, 0.62f, 1f, 1f),
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
            heading.color = new Color(0.06f, 0.12f, 0.28f, 1f);
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
                new Color(0.06f, 0.12f, 0.28f, 1f),
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

        void OnDestroy()
        {
            if (frameTexture == null)
                return;

            frameTexture.Release();
            Destroy(frameTexture);
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

                    float edge = Mathf.Clamp01((1.01f - distance) / 0.028f);
                    float rim = Mathf.Clamp01((distance - 0.82f) / 0.12f) * edge;
                    float topLight = Mathf.Clamp01((dy - 0.55f) / 0.4f) * rim;
                    float highlight = Mathf.Clamp01(1f - (Mathf.Sqrt(((dx + 0.32f) * (dx + 0.32f)) + ((dy - 0.38f) * (dy - 0.38f))) / 0.42f));
                    highlight = highlight * highlight;
                    Color color = Color.Lerp(ActionBlue, Color.white, (highlight * 0.82f) + (topLight * 0.45f));
                    color.a = edge * Mathf.Clamp01(0.58f + (rim * 0.32f) + (highlight * 0.28f));
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }

    }
}
