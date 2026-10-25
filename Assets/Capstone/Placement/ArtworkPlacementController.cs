using System;
using System.Collections.Generic;
using Capstone.Catalogue;
using Capstone.Dialogue;
using Capstone.Ranking;
using Capstone.Session;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Capstone.Placement
{
    /// <summary>
    /// Places one sample artwork on a detected vertical wall and removes it in the same session.
    /// A second Place moves that same quad. Swap replaces it with the other sample, in place.
    /// Send matches typed text against the written keyword table.
    /// Keep, save, and dismiss update the tag weights and show the top three other works.
    /// Use 1, Use 2, or Use 3 makes that recommended work the one Place puts on the wall.
    /// Notes reads the story stored on the current work and does not invent one.
    /// Keep, save, dismiss, and a matched question are stored on this session. Consent is not marked.
    /// Next walks the catalogue in stored order and brings that work's own centimetres.
    /// No wall means a retry message and no invented size.
    /// </summary>
    public sealed class ArtworkPlacementController : MonoBehaviour
    {
        public const string WaitingMessage = "Point the centre of the screen at a wall, then press Place.";
        public const string NoWallMessage = "Move closer to a clearer wall and try again.";
        public const string PlacedMessage = "Placed on the wall.";
        public const string MovedMessage = "Moved on the wall. The size is unchanged.";
        public const string RemovedMessage = "Removed. You can place it again.";
        public const string SwapNeedsPlacementMessage = "Place a work before swapping.";
        public const string SwappedMessage = "Swapped the work. The size follows the new piece.";
        public const string ChooseNeedsRankMessage = "Keep, save, or dismiss before choosing a work.";
        public const string ChooseMissingMessage = "That rank is not in the list.";
        public const string ChosenMessage = "Next Place uses this work.";
        public const string ChosenOnWallMessage = "Replaced the wall preview with the chosen work.";
        public const string NextMessage = "Next work.";
        public const string NextOnWallMessage = "Replaced the wall preview with the next work.";
        public const string NoCatalogueMessage = "No work is in the catalogue.";

        static readonly List<ARRaycastHit> Hits = new List<ARRaycastHit>();

        ARRaycastManager raycastManager;
        Camera arCamera;
        GameObject placed;
        Text status;
        Material surface;
        Artwork current;
        InputField concernField;
        FeedbackLoop feedback;
        BuyerSession session;
        List<Recommendation> offered;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureInScene()
        {
            if (FindAnyObjectByType<ArtworkPlacementController>() != null)
                return;
            if (FindAnyObjectByType<ARRaycastManager>() == null)
                return;

            GameObject host = new GameObject("Artwork Placement");
            host.AddComponent<ArtworkPlacementController>();
        }

        void Awake()
        {
            raycastManager = FindAnyObjectByType<ARRaycastManager>();
            arCamera = Camera.main;
            EnsureVerticalPlanes();
            SilenceTemplateSpawner();
            current = SampleArtwork.Create();
            feedback = new FeedbackLoop(PreferenceProfile.FromInitialTags(current.Style, current.Mood, current.Colour));
            session = new BuyerSession("local-preview");
            surface = CreateSurface(current);
            BuildControls();
        }

        public void Place()
        {
            if (!TryHitVerticalWall(out ARRaycastHit hit, out ARPlane plane))
            {
                status.text = NoWallMessage;
                return;
            }

            bool alreadyPlaced = placed != null;
            if (!alreadyPlaced)
                placed = CreateQuad();

            WallPose pose = ArtworkWallPose.At(
                current,
                hit.pose.position,
                plane.normal,
                arCamera != null ? arCamera.transform.position : hit.pose.position + plane.normal);
            placed.transform.SetPositionAndRotation(pose.Position, pose.Rotation);
            ApplyArtwork(current);
            status.text = (alreadyPlaced ? MovedMessage : PlacedMessage) + " " + Describe(current);
        }

        public void Swap()
        {
            if (placed == null)
            {
                status.text = SwapNeedsPlacementMessage;
                return;
            }

            Vector3 position = placed.transform.position;
            Quaternion rotation = placed.transform.rotation;
            current = SampleArtwork.OtherThan(current);
            ApplyArtwork(current);
            placed.transform.SetPositionAndRotation(position, rotation);
            status.text = SwappedMessage + " " + Describe(current);
        }

        public void Send()
        {
            string typed = concernField != null ? concernField.text : string.Empty;
            DialogueReply reply = RuleEngine.Match(typed, StarterRules.All);
            if (reply.Matched && feedback != null)
                feedback.Profile.NoteConcern(reply.Concern, current);
            if (reply.Matched && session != null && current != null)
                session.LogRule(current.Id, reply.RuleId, reply.Concern, DateTime.UtcNow);
            status.text = ReplyForCurrentWork(reply);
        }

        string ReplyForCurrentWork(DialogueReply reply)
        {
            if (!reply.Matched || reply.Language != "en" || current == null)
                return reply.Text;
            if (reply.Concern == "size")
                return reply.Text + " " + Describe(current);
            if (reply.Concern == "colour")
                return reply.Text + " Stored colour: " + current.Colour + ".";
            if (reply.Concern == "price")
                return reply.Text + " Stored price band: " + current.PriceBand + ".";
            return reply.Text;
        }

        public void Keep()
        {
            RecordFeedback("Kept", "Keep", loop => loop.Accept(current));
        }

        public void Save()
        {
            RecordFeedback("Saved", "Save", loop => loop.Save(current));
        }

        public void Dismiss()
        {
            RecordFeedback("Dismissed", "Dismiss", loop => loop.Dismiss(current));
        }

        void RecordFeedback(string verb, string action, System.Action<FeedbackLoop> apply)
        {
            if (feedback == null || current == null)
                return;
            apply(feedback);
            if (session != null)
                session.LogAction(current.Id, action, feedback.Profile.LastConcern, DateTime.UtcNow);
            offered = Recommender.RankThree(SampleCatalogue.All, feedback.Profile, current.Id);
            status.text = verb + " " + current.Title + ".\n" + DescribeRecommendations(offered) + "\nLogged.";
        }

        static string DescribeRecommendations(List<Recommendation> ranked)
        {
            if (ranked == null || ranked.Count == 0)
                return "No other works to rank.";

            System.Text.StringBuilder text = new System.Text.StringBuilder();
            for (int i = 0; i < ranked.Count; i++)
            {
                Recommendation recommendation = ranked[i];
                if (i > 0)
                    text.Append('\n');
                text.Append(recommendation.Rank);
                text.Append(". ");
                text.Append(recommendation.Artwork.Title);
                text.Append(" (");
                text.Append(recommendation.Score);
                text.Append(")");
                string meaning = recommendation.Artwork.StoryNotes.Meaning;
                if (meaning.Length > 0)
                {
                    text.Append(". ");
                    text.Append(meaning);
                }
            }

            return text.ToString();
        }

        public void ChooseFirst()
        {
            ChooseRank(1);
        }

        public void ChooseSecond()
        {
            ChooseRank(2);
        }

        public void ChooseThird()
        {
            ChooseRank(3);
        }

        public void ChooseRank(int oneBasedRank)
        {
            if (offered == null || offered.Count == 0)
            {
                status.text = ChooseNeedsRankMessage;
                return;
            }

            Artwork chosen = Recommender.Choose(offered, oneBasedRank);
            if (chosen == null)
            {
                status.text = ChooseMissingMessage;
                return;
            }

            current = chosen;
            if (placed == null)
            {
                status.text = ChosenMessage + " " + current.Title + ". " + Describe(current);
                return;
            }

            Vector3 position = placed.transform.position;
            Quaternion rotation = placed.transform.rotation;
            ApplyArtwork(current);
            placed.transform.SetPositionAndRotation(position, rotation);
            status.text = ChosenOnWallMessage + " " + Describe(current);
        }

        public void Advance()
        {
            string currentId = current != null ? current.Id : string.Empty;
            Artwork next = CatalogueSequence.Next(SampleCatalogue.All, currentId);
            if (next == null)
            {
                status.text = NoCatalogueMessage;
                return;
            }

            current = next;
            if (placed == null)
            {
                status.text = NextMessage + " " + current.Title + ". " + Describe(current);
                return;
            }

            Vector3 position = placed.transform.position;
            Quaternion rotation = placed.transform.rotation;
            ApplyArtwork(current);
            placed.transform.SetPositionAndRotation(position, rotation);
            status.text = NextOnWallMessage + " " + Describe(current);
        }

        public void ShowNotes()
        {
            status.text = ArtworkDetail.Describe(current);
        }

        public void Remove()
        {
            if (placed == null)
            {
                status.text = RemovedMessage;
                return;
            }

            Destroy(placed);
            placed = null;
            status.text = RemovedMessage;
        }

        static void EnsureVerticalPlanes()
        {
            ARPlaneManager planes = FindAnyObjectByType<ARPlaneManager>();
            if (planes == null)
                return;

            planes.requestedDetectionMode |= PlaneDetectionMode.Vertical;
        }

        static void SilenceTemplateSpawner()
        {
            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour != null && behaviour.GetType().Name == "ARInteractorSpawnTrigger")
                    behaviour.enabled = false;
            }
        }

        bool TryHitVerticalWall(out ARRaycastHit hit, out ARPlane plane)
        {
            hit = default;
            plane = null;
            if (raycastManager == null)
                return false;

            Vector2 centre = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            if (!raycastManager.Raycast(centre, Hits, TrackableType.PlaneWithinPolygon))
                return false;

            foreach (ARRaycastHit candidate in Hits)
            {
                if (candidate.trackable is ARPlane candidatePlane
                    && candidatePlane.alignment == PlaneAlignment.Vertical)
                {
                    hit = candidate;
                    plane = candidatePlane;
                    return true;
                }
            }

            return false;
        }

        GameObject CreateQuad()
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Sample artwork";
            Collider collider = quad.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = surface;
            return quad;
        }

        void ApplyArtwork(Artwork artwork)
        {
            WallSize size = artwork.WallSizeMetres;
            placed.transform.localScale = new Vector3(size.WidthMetres, size.HeightMetres, 1f);
            placed.name = artwork.Title;
            Texture2D image = ImageFor(artwork);
            surface.SetTexture("_BaseMap", image);
            surface.SetTexture("_MainTex", image);
        }

        static string Describe(Artwork artwork)
        {
            return artwork.WidthCm.ToString("0") + " cm wide, " + artwork.HeightCm.ToString("0") + " cm tall.";
        }

        Material CreateSurface(Artwork artwork)
        {
            Material template = Resources.Load<Material>("SampleArtworkSurface");
            Material material = template != null
                ? new Material(template)
                : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            Texture2D image = ImageFor(artwork);
            material.SetTexture("_BaseMap", image);
            material.SetTexture("_MainTex", image);
            material.SetColor("_BaseColor", Color.white);
            return material;
        }

        static Texture2D ImageFor(Artwork artwork)
        {
            return SampleTexture(FillFor(artwork));
        }

        static Color FillFor(Artwork artwork)
        {
            string colour = artwork != null ? artwork.Colour.Trim().ToLowerInvariant() : string.Empty;
            if (colour == "orange")
                return new Color(0.85f, 0.45f, 0.12f, 1f);
            if (colour == "red")
                return new Color(0.75f, 0.15f, 0.15f, 1f);
            if (colour == "blue")
                return new Color(0.15f, 0.35f, 0.75f, 1f);
            return new Color(0.45f, 0.45f, 0.45f, 1f);
        }

        static Texture2D SampleTexture(Color fill)
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color white = Color.white;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool border = x < 3 || y < 3 || x >= size - 3 || y >= size - 3;
                    bool topBand = y >= size - 14;
                    texture.SetPixel(x, y, border || topBand ? white : fill);
                }
            }

            texture.Apply();
            return texture;
        }

        void BuildControls()
        {
            GameObject canvasObject = new GameObject("Artwork placement controls");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            canvasObject.AddComponent<GraphicRaycaster>();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            status = CreateLabel(canvasObject.transform, font, WaitingMessage, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), 36);
            status.rectTransform.sizeDelta = new Vector2(900f, 240f);
            CreateLabel(
                canvasObject.transform,
                font,
                "White band is the top. Swap keeps the quad and changes the piece.",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -160f),
                28);
            CreateButton(canvasObject.transform, font, "Use 1", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 550f), new Vector2(320f, 100f), ChooseFirst);
            CreateButton(canvasObject.transform, font, "Use 2", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 550f), new Vector2(320f, 100f), ChooseSecond);
            CreateButton(canvasObject.transform, font, "Use 3", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 550f), new Vector2(320f, 100f), ChooseThird);
            CreateButton(canvasObject.transform, font, "Notes", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 320f), new Vector2(320f, 100f), ShowNotes);
            CreateButton(canvasObject.transform, font, "Next", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 320f), new Vector2(320f, 100f), Advance);
            CreateButton(canvasObject.transform, font, "Keep", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 430f), new Vector2(320f, 100f), Keep);
            CreateButton(canvasObject.transform, font, "Save", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 430f), new Vector2(320f, 100f), Save);
            CreateButton(canvasObject.transform, font, "Dismiss", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 430f), new Vector2(320f, 100f), Dismiss);
            concernField = CreateInput(canvasObject.transform, font);
            CreateButton(canvasObject.transform, font, "Send", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 200f), new Vector2(240f, 110f), Send);
            CreateButton(canvasObject.transform, font, "Place", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 40f), new Vector2(320f, 140f), Place);
            CreateButton(canvasObject.transform, font, "Swap", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(320f, 140f), Swap);
            CreateButton(canvasObject.transform, font, "Remove", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 40f), new Vector2(320f, 140f), Remove);
        }

        static Text CreateLabel(Transform parent, Font font, string message, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, int fontSize)
        {
            GameObject label = new GameObject(message);
            label.transform.SetParent(parent, false);
            Text text = label.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = message;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(900f, 120f);
            rect.anchoredPosition = position;
            return text;
        }

        static void CreateButton(Transform parent, Font font, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObject = new GameObject(label);
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(anchorMin.x, 0f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            GameObject caption = new GameObject("Label");
            caption.transform.SetParent(buttonObject.transform, false);
            Text text = caption.AddComponent<Text>();
            text.font = font;
            text.fontSize = 48;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        static InputField CreateInput(Transform parent, Font font)
        {
            GameObject fieldObject = new GameObject("Concern");
            fieldObject.transform.SetParent(parent, false);
            Image image = fieldObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.92f);
            InputField field = fieldObject.AddComponent<InputField>();
            RectTransform rect = fieldObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.sizeDelta = new Vector2(760f, 110f);
            rect.anchoredPosition = new Vector2(24f, 200f);

            Text text = CreateFieldText(fieldObject.transform, font, string.Empty, Color.black, FontStyle.Normal, 36);
            Text placeholder = CreateFieldText(fieldObject.transform, font, "Ask about size, colour, or price", new Color(0.3f, 0.3f, 0.3f, 0.8f), FontStyle.Italic, 32);
            field.textComponent = text;
            field.placeholder = placeholder;
            return field;
        }

        static Text CreateFieldText(Transform parent, Font font, string message, Color color, FontStyle style, int fontSize)
        {
            GameObject textObject = new GameObject(message.Length == 0 ? "Text" : "Placeholder");
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.text = message;
            text.supportRichText = false;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(16f, 8f);
            rect.offsetMax = new Vector2(-16f, -8f);
            return text;
        }
    }
}
