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
    /// Keep, save, dismiss, a matched question, and a chosen rank are stored on this session after consent.
    /// A keep, save, or dismiss also stores the recommended artwork ids in rank order.
    /// Next walks the current list in stored order and brings that work's own centimetres.
    /// All, Blue, Calm, and Fits 50 change that list. A filtered work keeps its stored size.
    /// Study switches to the catalogue-only condition and back. The session starts as the prototype.
    /// Works, Wall, and Recommend are the three steps. The catalogue condition stays on Works.
    /// Consent must be marked before a keep, save, dismiss, matched question, or chosen rank is stored.
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
        public const string NoMatchMessage = "No work matches this filter.";
        public const string ConsentMessage = "Consent is marked. Later actions can be stored.";
        public const string NotLoggedMessage = "Not logged. Consent is not marked.";
        public const string RecommendStepMessage = "Keep, save, or dismiss, then choose a recommended work.";

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
        List<Artwork> shown;
        string step = PrototypeScreen.Works;
        Font uiFont;
        GameObject worksPanel;
        GameObject wallPanel;
        GameObject recommendPanel;
        RectTransform workList;
        Image worksNavImage;
        Image wallNavImage;
        Image recommendNavImage;

        static readonly Color StepOn = new Color(0.15f, 0.35f, 0.75f, 0.95f);
        static readonly Color StepOff = new Color(0.1f, 0.1f, 0.1f, 0.85f);

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
            shown = CatalogueFilter.Matching(SampleCatalogue.All, null, null, null, 0f, 0f);
            feedback = new FeedbackLoop(PreferenceProfile.FromInitialTags(current.Style, current.Mood, current.Colour));
            session = new BuyerSession("local-preview");
            surface = CreateSurface(current);
            BuildControls();
        }

        public void Place()
        {
            if (Blocked(StudyCondition.Placement))
                return;
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
            if (Blocked(StudyCondition.Placement))
                return;
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
            if (Blocked(StudyCondition.Dialogue))
                return;
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
            if (reply.Concern == "size" || reply.Concern == "fit")
                return reply.Text + " " + Describe(current);
            if (reply.Concern == "meaning")
            {
                string meaning = current.StoryNotes.Meaning;
                string note = string.IsNullOrWhiteSpace(meaning) ? ArtworkDetail.MissingNote : meaning;
                return reply.Text + " " + note;
            }
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
            if (Blocked(StudyCondition.Ranking))
                return;
            if (feedback == null || current == null)
                return;
            apply(feedback);
            offered = Recommender.RankThree(shown, feedback.Profile, current.Id);
            string[] recommendedIds = IdsInRankOrder(offered);
            bool logged = session != null && session.LogAction(
                current.Id,
                action,
                feedback.Profile.LastConcern,
                DateTime.UtcNow,
                recommendedIds);
            status.text = verb + " " + current.Title + ".\n" + DescribeRecommendations(offered)
                + "\n" + (logged ? LoggedRanks(recommendedIds) : NotLoggedMessage);
        }

        static string[] IdsInRankOrder(List<Recommendation> ranked)
        {
            if (ranked == null || ranked.Count == 0)
                return new string[0];

            string[] ids = new string[ranked.Count];
            for (int i = 0; i < ranked.Count; i++)
                ids[i] = ranked[i].Artwork.Id;
            return ids;
        }

        static string LoggedRanks(string[] recommendedIds)
        {
            if (recommendedIds == null || recommendedIds.Length == 0)
                return "Logged.";
            return "Logged. " + string.Join(", ", recommendedIds);
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
            if (Blocked(StudyCondition.Ranking))
                return;
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
            string concern = feedback != null && feedback.Profile != null ? feedback.Profile.LastConcern : string.Empty;
            bool logged = session != null && session.LogChoice(current.Id, oneBasedRank, concern, DateTime.UtcNow);
            string loggedLine = logged ? "Logged." : NotLoggedMessage;
            if (placed == null)
            {
                status.text = ChosenMessage + " " + current.Title + ". " + Describe(current) + "\n" + loggedLine;
                return;
            }

            Vector3 position = placed.transform.position;
            Quaternion rotation = placed.transform.rotation;
            ApplyArtwork(current);
            placed.transform.SetPositionAndRotation(position, rotation);
            status.text = ChosenOnWallMessage + " " + Describe(current) + "\n" + loggedLine;
        }

        public void Advance()
        {
            string currentId = current != null ? current.Id : string.Empty;
            Artwork next = CatalogueSequence.Next(shown, currentId);
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

        public void ShowAll()
        {
            UseFilter(null, null, null, 0f, 0f);
        }

        public void ShowBlue()
        {
            UseFilter(null, null, "blue", 0f, 0f);
        }

        public void ShowCalm()
        {
            UseFilter(null, "calm", null, 0f, 0f);
        }

        public void ShowFitsFifty()
        {
            UseFilter(null, null, null, 50f, 50f);
        }

        void UseFilter(string style, string mood, string colour, float maxWidthCm, float maxHeightCm)
        {
            shown = CatalogueFilter.Matching(SampleCatalogue.All, style, mood, colour, maxWidthCm, maxHeightCm);
            offered = null;
            if (shown.Count == 0)
            {
                RefreshWorkList();
                status.text = NoMatchMessage;
                return;
            }

            bool currentStillListed = false;
            for (int i = 0; i < shown.Count; i++)
            {
                if (current != null && shown[i].Id == current.Id)
                {
                    currentStillListed = true;
                    break;
                }
            }

            if (!currentStillListed)
            {
                current = shown[0];
                if (placed != null)
                {
                    Vector3 position = placed.transform.position;
                    Quaternion rotation = placed.transform.rotation;
                    ApplyArtwork(current);
                    placed.transform.SetPositionAndRotation(position, rotation);
                }
            }

            RefreshWorkList();
            status.text = WorksHeading();
            if (!currentStillListed && current != null)
                status.text = status.text + " " + Describe(current);
        }

        public void ShowWorks()
        {
            OpenStep(PrototypeScreen.Works);
        }

        public void ShowWall()
        {
            OpenStep(PrototypeScreen.Wall);
        }

        public void ShowRecommendStep()
        {
            OpenStep(PrototypeScreen.Recommend);
        }

        void OpenStep(string requested)
        {
            string condition = session != null ? session.Condition : StudyCondition.Prototype;
            string opened = PrototypeScreen.Open(condition, requested);
            step = opened;
            ApplyStepVisibility();
            if (opened != requested)
            {
                status.text = StudyCondition.BaselineMessage;
                return;
            }

            if (opened == PrototypeScreen.Wall)
                status.text = WallHeading();
            else if (opened == PrototypeScreen.Recommend)
                status.text = RecommendStepMessage;
            else
                status.text = WorksHeading();
        }

        void ApplyStepVisibility()
        {
            if (worksPanel != null)
                worksPanel.SetActive(PrototypeScreen.ShowsWorks(step));
            if (wallPanel != null)
                wallPanel.SetActive(PrototypeScreen.ShowsWall(step));
            if (recommendPanel != null)
                recommendPanel.SetActive(PrototypeScreen.ShowsRecommend(step));
            PaintStep(worksNavImage, PrototypeScreen.ShowsWorks(step));
            PaintStep(wallNavImage, PrototypeScreen.ShowsWall(step));
            PaintStep(recommendNavImage, PrototypeScreen.ShowsRecommend(step));
        }

        static void PaintStep(Image image, bool open)
        {
            if (image != null)
                image.color = open ? StepOn : StepOff;
        }

        string WorksHeading()
        {
            int count = shown != null ? shown.Count : 0;
            string title = current != null ? current.Title : "No work";
            return count + " works. Selected: " + title + ".";
        }

        string WallHeading()
        {
            if (current == null)
                return WaitingMessage;
            return current.Title + ". " + Describe(current) + " " + WaitingMessage;
        }

        void SelectShown(int index)
        {
            if (shown == null || index < 0 || index >= shown.Count || shown[index] == null)
                return;

            current = shown[index];
            if (placed != null)
            {
                Vector3 position = placed.transform.position;
                Quaternion rotation = placed.transform.rotation;
                ApplyArtwork(current);
                placed.transform.SetPositionAndRotation(position, rotation);
            }

            RefreshWorkList();
            status.text = ArtworkDetail.Describe(current);
        }

        void RefreshWorkList()
        {
            if (workList == null || uiFont == null)
                return;

            for (int child = workList.childCount - 1; child >= 0; child--)
                Destroy(workList.GetChild(child).gameObject);

            if (shown == null)
                return;

            for (int i = 0; i < shown.Count; i++)
            {
                Artwork work = shown[i];
                if (work == null)
                    continue;

                int captured = i;
                string label = work.Title + "   " + work.WidthCm.ToString("0") + " x " + work.HeightCm.ToString("0") + " cm";
                Button button = CreateButton(
                    workList,
                    uiFont,
                    label,
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f),
                    new Vector2(24f, 640f - (i * 104f)),
                    new Vector2(1032f, 96f),
                    () => SelectShown(captured),
                    30);
                if (current != null && work.Id == current.Id)
                    button.GetComponent<Image>().color = StepOn;
            }
        }

        public void ShowNotes()
        {
            status.text = ArtworkDetail.Describe(current);
        }

        public void GiveConsent()
        {
            if (session == null)
                return;
            session.MarkConsent();
            status.text = ConsentMessage;
        }

        public void ToggleStudy()
        {
            if (session == null)
                return;
            if (session.Condition == StudyCondition.Baseline)
                session.UsePrototype();
            else
                session.UseBaseline();

            if (placed != null)
                placed.SetActive(session.Condition != StudyCondition.Baseline);
            if (session.Condition == StudyCondition.Baseline)
                OpenStep(PrototypeScreen.Works);
            status.text = session.Condition == StudyCondition.Baseline
                ? StudyCondition.BaselineMessage
                : StudyCondition.PrototypeMessage;
        }

        bool Blocked(string feature)
        {
            string condition = session != null ? session.Condition : StudyCondition.Prototype;
            if (StudyCondition.Allows(condition, feature))
                return false;
            status.text = StudyCondition.BaselineMessage;
            return true;
        }

        public void Remove()
        {
            if (Blocked(StudyCondition.Placement))
                return;
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

            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            status = CreateLabel(canvasObject.transform, uiFont, WorksHeading(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), 36);
            status.rectTransform.sizeDelta = new Vector2(980f, 340f);
            CreateLabel(
                canvasObject.transform,
                uiFont,
                "White band is the top of the work.",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 1220f),
                28);
            CreateButton(canvasObject.transform, uiFont, "Study", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(16f, 980f), new Vector2(240f, 80f), ToggleStudy, 32);
            CreateButton(canvasObject.transform, uiFont, "Consent", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(272f, 980f), new Vector2(240f, 80f), GiveConsent, 32);
            worksNavImage = CreateButton(canvasObject.transform, uiFont, "Works", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(16f, 880f), new Vector2(340f, 80f), ShowWorks, 32).GetComponent<Image>();
            wallNavImage = CreateButton(canvasObject.transform, uiFont, "Wall", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(370f, 880f), new Vector2(340f, 80f), ShowWall, 32).GetComponent<Image>();
            recommendNavImage = CreateButton(canvasObject.transform, uiFont, "Recommend", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(724f, 880f), new Vector2(340f, 80f), ShowRecommendStep, 32).GetComponent<Image>();

            worksPanel = CreatePanel(canvasObject.transform, "Works panel");
            wallPanel = CreatePanel(canvasObject.transform, "Wall panel");
            recommendPanel = CreatePanel(canvasObject.transform, "Recommend panel");
            workList = CreatePanel(worksPanel.transform, "Work list").GetComponent<RectTransform>();

            CreateButton(worksPanel.transform, uiFont, "All", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(16f, 760f), new Vector2(250f, 80f), ShowAll, 32);
            CreateButton(worksPanel.transform, uiFont, "Blue", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(282f, 760f), new Vector2(250f, 80f), ShowBlue, 32);
            CreateButton(worksPanel.transform, uiFont, "Calm", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(548f, 760f), new Vector2(250f, 80f), ShowCalm, 32);
            CreateButton(worksPanel.transform, uiFont, "Fits 50", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(814f, 760f), new Vector2(250f, 80f), ShowFitsFifty, 32);
            RefreshWorkList();

            CreateButton(wallPanel.transform, uiFont, "Notes", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 340f), new Vector2(1032f, 100f), ShowNotes, 36);
            concernField = CreateInput(wallPanel.transform, uiFont);
            CreateButton(wallPanel.transform, uiFont, "Send", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 200f), new Vector2(240f, 110f), Send);
            CreateButton(wallPanel.transform, uiFont, "Place", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 40f), new Vector2(320f, 140f), Place);
            CreateButton(wallPanel.transform, uiFont, "Swap", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(320f, 140f), Swap);
            CreateButton(wallPanel.transform, uiFont, "Remove", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 40f), new Vector2(320f, 140f), Remove);

            CreateButton(recommendPanel.transform, uiFont, "Keep", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 320f), new Vector2(320f, 120f), Keep);
            CreateButton(recommendPanel.transform, uiFont, "Save", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 320f), new Vector2(320f, 120f), Save);
            CreateButton(recommendPanel.transform, uiFont, "Dismiss", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 320f), new Vector2(320f, 120f), Dismiss);
            CreateButton(recommendPanel.transform, uiFont, "Use 1", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 180f), new Vector2(320f, 120f), ChooseFirst);
            CreateButton(recommendPanel.transform, uiFont, "Use 2", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 180f), new Vector2(320f, 120f), ChooseSecond);
            CreateButton(recommendPanel.transform, uiFont, "Use 3", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 180f), new Vector2(320f, 120f), ChooseThird);
            ApplyStepVisibility();
        }

        static GameObject CreatePanel(Transform parent, string name)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return panel;
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

        static Button CreateButton(Transform parent, Font font, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction onClick, int labelFontSize = 48)
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
            text.fontSize = labelFontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            return button;
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
            Text placeholder = CreateFieldText(fieldObject.transform, font, "Ask about size, fit, light, colour, meaning, or price", new Color(0.3f, 0.3f, 0.3f, 0.8f), FontStyle.Italic, 28);
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
