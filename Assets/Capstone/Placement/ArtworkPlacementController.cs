using System;
using System.Collections.Generic;
using Capstone.Catalogue;
using Capstone.Dialogue;
using Capstone.Ranking;
using Capstone.Session;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
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
    /// In the editor, Play asks for Room 1, Room 2, or Room 3 in any scene, including a plain 3D scene.
    /// After that choice, Place uses the chosen room wall. The artwork keeps its stored centimetres.
    /// The app opens on a floating circle with no words. A tap brings three short screens, then the phone opens on the camera. Move until a vertical wall is detected, then press Place.
    /// </summary>
    [DefaultExecutionOrder(10000)]
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
        public const string WorksStepMessage = "Choose a work. The size and story are stored on it.";
        public const string ChooseRoomMessage = "Choose the room that fits. Then place a work, ask a question, and keep or dismiss.";
        public const string ScanWallMessage = "Move the phone slowly across the wall.";
        public const string WallFoundMessage = "Wall detected. You are ready to test the art piece. Press Place.";
        public const string WallStepMessage = "Press Place. The work keeps its stored centimetres.";
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
        Text stepLine;
        Text studyLabel;
        DemoRoomStage rooms;
        GameObject choicePanel;
        Button studyButtonWidget;
        Button consentButtonWidget;
        Button placeButton;
        GameObject notesButton;
        GameObject sendButton;
        GameObject swapButton;
        GameObject removeButton;
        Image aimMark;
        RectTransform statusShade;
        bool roomChosen;
        bool scanning;
        bool wallReady;
        CanvasScaler screenFit;
        RectTransform screenColumn;
        static readonly Color AimSearching = Color.white;
        static readonly Color AimReady = new Color(0.25f, 0.9f, 0.4f, 1f);

        static readonly Color StepOn = new Color(0.15f, 0.35f, 0.75f, 0.95f);
        static readonly Color StepOff = new Color(0.1f, 0.1f, 0.1f, 0.85f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureInScene()
        {
            if (FindAnyObjectByType<ArtworkPlacementController>() != null)
                return;
            // A phone build still needs the AR raycast. The editor demo runs in a plain 3D scene.
            if (!Application.isEditor && FindAnyObjectByType<ARRaycastManager>() == null)
                return;

            GameObject host = new GameObject("Artwork Placement");
            host.AddComponent<ArtworkPlacementController>();
        }

        void Awake()
        {
            raycastManager = FindAnyObjectByType<ARRaycastManager>();
            arCamera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
            EnsurePointer();
            EnsureVerticalPlanes();
            SilenceTemplateSpawner();
            current = SampleArtwork.Create();
            shown = CatalogueFilter.Matching(SampleCatalogue.All, null, null, null, 0f, 0f);
            feedback = new FeedbackLoop(PreferenceProfile.FromInitialTags(current.Style, current.Mood, current.Colour));
            session = new BuyerSession("local-preview");
            surface = CreateSurface(current);
            roomChosen = !Application.isEditor;
            if (Application.isEditor)
            {
                rooms = new DemoRoomStage(DemoRoom.Three());
                rooms.Build(transform, surface);
                HideSimulatedRoom();
            }
            BuildControls();
        }

        void Update()
        {
            if (!scanning || status == null || step != PrototypeScreen.Wall)
                return;

            ARRaycastHit hit;
            ARPlane plane;
            bool found = TryHitVerticalWall(out hit, out plane);
            string next = found ? WallFoundMessage : ScanWallMessage;
            if (found == wallReady && status.text == next)
                return;

            wallReady = found;
            status.text = next;
            if (aimMark != null)
                aimMark.color = found ? AimReady : AimSearching;
            ApplyStepVisibility();
        }

        void LateUpdate()
        {
            FitScreen();
            if (!Application.isEditor || rooms == null || arCamera == null)
                return;

            HideSimulatedRoom();
            rooms.Frame(arCamera);
        }

        void FitScreen()
        {
            if (screenFit == null || screenColumn == null)
                return;

            float width = Screen.width;
            float height = Screen.height;
            if (width < 2f || height < 2f)
                return;

            bool portrait = height >= width;
            float match = portrait ? 0f : 1f;
            if (!Mathf.Approximately(screenFit.matchWidthOrHeight, match))
                screenFit.matchWidthOrHeight = match;

            float scale = screenFit.scaleFactor;
            if (scale < 0.01f)
                return;

            Rect safe = Screen.safeArea;
            float bottom = safe.yMin / scale;
            float top = (height - safe.yMax) / scale;
            Vector2 min = screenColumn.offsetMin;
            Vector2 max = screenColumn.offsetMax;
            if (Mathf.Approximately(min.y, bottom) && Mathf.Approximately(max.y, -top))
                return;

            min.y = bottom;
            max.y = -top;
            screenColumn.offsetMin = min;
            screenColumn.offsetMax = max;
        }

        static void HideSimulatedRoom()
        {
            GameObject simulated = GameObject.Find("Simulated Environment Scene");
            if (simulated != null)
                simulated.SetActive(false);
        }

        public void Place()
        {
            if (Blocked(StudyCondition.Placement))
                return;

            if (Application.isEditor && rooms != null && roomChosen)
            {
                Vector3 point;
                Vector3 normal;
                if (!rooms.TryHit(arCamera, out point, out normal))
                {
                    point = rooms.WallFace();
                    normal = Vector3.forward;
                }
                PlaceOn(point, normal);
                return;
            }

            if (!TryHitVerticalWall(out ARRaycastHit hit, out ARPlane plane))
            {
                status.text = NoWallMessage;
                return;
            }

            PlaceOn(hit.pose.position, plane.normal);
        }

        void PlaceOn(Vector3 point, Vector3 normal)
        {
            bool alreadyPlaced = placed != null;
            if (!alreadyPlaced)
                placed = CreateQuad();

            Vector3 cameraPosition = arCamera != null ? arCamera.transform.position : point + normal;
            WallPose pose = ArtworkWallPose.At(current, point, normal, cameraPosition);
            placed.transform.SetPositionAndRotation(pose.Position, pose.Rotation);
            ApplyArtwork(current);
            string roomLine = Application.isEditor && rooms != null && rooms.Current != null
                ? " " + rooms.Current.Describe()
                : "";
            scanning = false;
            wallReady = false;
            status.text = (alreadyPlaced ? MovedMessage : PlacedMessage) + " " + Describe(current) + roomLine;
            ApplyStepVisibility();
        }

        public void ShowWarmRoom()
        {
            ShowDemoRoom(0);
        }

        public void ShowGalleryRoom()
        {
            ShowDemoRoom(1);
        }

        public void ShowHallRoom()
        {
            ShowDemoRoom(2);
        }

        void ShowDemoRoom(int index)
        {
            if (rooms == null)
                return;

            roomChosen = true;
            rooms.Show(index);
            if (arCamera != null)
                rooms.Frame(arCamera);
            if (placed != null && current != null)
            {
                Vector3 cameraPosition = arCamera != null ? arCamera.transform.position : rooms.CameraPosition();
                WallPose pose = ArtworkWallPose.At(current, rooms.WallFace(), Vector3.forward, cameraPosition);
                placed.transform.SetPositionAndRotation(pose.Position, pose.Rotation);
            }

            OpenStep(PrototypeScreen.Wall);
        }

        public void ChangeRoom()
        {
            if (!Application.isEditor || rooms == null)
                return;
            roomChosen = false;
            ApplyStepVisibility();
            status.text = ChooseRoomMessage;
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
            scanning = false;
            string typed = concernField != null ? concernField.text : string.Empty;
            DialogueReply reply = RuleEngine.Match(typed, StarterRules.All);
            if (reply.Matched && feedback != null)
                feedback.Profile.NoteConcern(reply.Concern, current);
            if (reply.Matched && session != null && current != null)
                session.LogRule(current.Id, reply.RuleId, reply.Concern, DateTime.UtcNow);
            status.text = ReplyForCurrentWork(reply);
            ApplyStepVisibility();
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
            status.text = WorksStatus();
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
                status.text = WorksStatus();
        }

        void ApplyStepVisibility()
        {
            bool choosing = Application.isEditor && !roomChosen;
            bool phoneScan = !Application.isEditor && scanning && step == PrototypeScreen.Wall;
            if (choicePanel != null)
                choicePanel.SetActive(choosing);
            SetButtonActive(studyButtonWidget, !choosing && !phoneScan);
            SetButtonActive(consentButtonWidget, !choosing && !phoneScan);
            SetButtonActive(worksNavImage, !choosing && !phoneScan);
            SetButtonActive(wallNavImage, !choosing && !phoneScan);
            SetButtonActive(recommendNavImage, !choosing && !phoneScan);
            if (worksPanel != null)
                worksPanel.SetActive(!choosing && !phoneScan && PrototypeScreen.ShowsWorks(step));
            if (wallPanel != null)
                wallPanel.SetActive(!choosing && PrototypeScreen.ShowsWall(step));
            if (recommendPanel != null)
                recommendPanel.SetActive(!choosing && !phoneScan && PrototypeScreen.ShowsRecommend(step));
            SetObjectActive(notesButton, !phoneScan);
            SetObjectActive(sendButton, !phoneScan);
            SetObjectActive(swapButton, !phoneScan);
            SetObjectActive(removeButton, !phoneScan);
            if (concernField != null)
                concernField.gameObject.SetActive(!phoneScan);
            if (placeButton != null)
            {
                placeButton.gameObject.SetActive(!phoneScan || wallReady);
                RectTransform placeRect = placeButton.GetComponent<RectTransform>();
                if (phoneScan)
                {
                    placeRect.anchorMin = new Vector2(0.5f, 0f);
                    placeRect.anchorMax = new Vector2(0.5f, 0f);
                    placeRect.pivot = new Vector2(0.5f, 0f);
                    placeRect.sizeDelta = new Vector2(640f, 140f);
                    placeRect.anchoredPosition = new Vector2(0f, 40f);
                }
                else
                {
                    placeRect.anchorMin = new Vector2(0f, 0f);
                    placeRect.anchorMax = new Vector2(0f, 0f);
                    placeRect.pivot = new Vector2(0f, 0f);
                    placeRect.sizeDelta = new Vector2(320f, 140f);
                    placeRect.anchoredPosition = new Vector2(24f, 40f);
                }
            }
            if (aimMark != null)
            {
                aimMark.gameObject.SetActive(phoneScan);
                aimMark.color = wallReady ? AimReady : AimSearching;
            }
            if (statusShade != null)
                statusShade.sizeDelta = new Vector2(0f, phoneScan ? 200f : 280f);
            if (stepLine != null)
            {
                stepLine.gameObject.SetActive(!phoneScan);
                stepLine.text = choosing ? "Pick Room 1, Room 2, or Room 3." : StepInstruction();
            }
            PaintStep(worksNavImage, PrototypeScreen.ShowsWorks(step));
            PaintStep(wallNavImage, PrototypeScreen.ShowsWall(step));
            PaintStep(recommendNavImage, PrototypeScreen.ShowsRecommend(step));
        }

        static void SetObjectActive(GameObject widget, bool active)
        {
            if (widget != null)
                widget.SetActive(active);
        }

        static void SetButtonActive(Component widget, bool active)
        {
            if (widget != null)
                widget.gameObject.SetActive(active);
        }

        string StepInstruction()
        {
            if (step == PrototypeScreen.Wall)
                return Application.isEditor ? WallStepMessage : ScanWallMessage;
            if (step == PrototypeScreen.Recommend)
                return RecommendStepMessage;
            return WorksStepMessage;
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

        string WorksStatus()
        {
            if (current == null)
                return WorksHeading();
            string meaning = current.StoryNotes != null ? current.StoryNotes.Meaning : string.Empty;
            string note = string.IsNullOrWhiteSpace(meaning) ? ArtworkDetail.MissingNote : meaning;
            return WorksHeading() + " " + Describe(current) + " " + note;
        }

        string WallHeading()
        {
            string room = rooms != null && rooms.Current != null ? rooms.Current.Describe() + " " : "";
            if (current == null)
                return room + WaitingMessage;
            return room + current.Title + ". " + Describe(current);
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
            status.text = WorksStatus();
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
                    new Vector2(24f, 490f - (i * 92f)),
                    new Vector2(1032f, 84f),
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
            RefreshStudyLabel();
            status.text = session.Condition == StudyCondition.Baseline
                ? StudyCondition.BaselineMessage
                : StudyCondition.PrototypeMessage;
        }

        void RefreshStudyLabel()
        {
            if (studyLabel == null || session == null)
                return;
            studyLabel.text = session.Condition == StudyCondition.Baseline ? "To prototype" : "To catalogue";
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
            scanning = !Application.isEditor;
            wallReady = false;
            status.text = RemovedMessage;
            ApplyStepVisibility();
        }

        static void EnsurePointer()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
                return;

            GameObject events = new GameObject("EventSystem");
            events.AddComponent<EventSystem>();
            events.AddComponent<InputSystemUIInputModule>();
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
            GameObject templateUi = GameObject.Find("UI");
            if (templateUi != null)
                templateUi.SetActive(false);
            GameObject greeting = GameObject.Find("GreetingCTA");
            if (greeting != null)
                greeting.SetActive(false);

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
            screenFit = canvasObject.AddComponent<CanvasScaler>();
            screenFit.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            screenFit.referenceResolution = new Vector2(1080f, 1920f);
            screenFit.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            screenFit.matchWidthOrHeight = 1f;
            canvasObject.AddComponent<GraphicRaycaster>();

            GameObject column = CreatePanel(canvasObject.transform, "Prototype column");
            screenColumn = column.GetComponent<RectTransform>();
            screenColumn.anchorMin = new Vector2(0.5f, 0f);
            screenColumn.anchorMax = new Vector2(0.5f, 1f);
            screenColumn.pivot = new Vector2(0.5f, 0.5f);
            screenColumn.sizeDelta = new Vector2(1080f, 0f);
            screenColumn.anchoredPosition = Vector2.zero;

            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statusShade = CreateShade(column.transform);
            aimMark = CreateAimMark(column.transform);
            status = CreateLabel(column.transform, uiFont, WorksStatus(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), 30);
            status.rectTransform.sizeDelta = new Vector2(1000f, 150f);
            status.alignment = TextAnchor.UpperCenter;
            stepLine = CreateLabel(column.transform, uiFont, WorksStepMessage, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -180f), 26);
            stepLine.rectTransform.sizeDelta = new Vector2(1000f, 70f);
            studyButtonWidget = CreateButton(column.transform, uiFont, "To catalogue", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(16f, 760f), new Vector2(500f, 72f), ToggleStudy, 28);
            studyLabel = studyButtonWidget.GetComponentInChildren<Text>();
            consentButtonWidget = CreateButton(column.transform, uiFont, "Consent", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-16f, 760f), new Vector2(500f, 72f), GiveConsent, 28);
            worksNavImage = CreateButton(column.transform, uiFont, "Works", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(16f, 680f), new Vector2(340f, 70f), ShowWorks, 28).GetComponent<Image>();
            wallNavImage = CreateButton(column.transform, uiFont, "Wall", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(370f, 680f), new Vector2(340f, 70f), ShowWall, 28).GetComponent<Image>();
            recommendNavImage = CreateButton(column.transform, uiFont, "Recommend", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(724f, 680f), new Vector2(340f, 70f), ShowRecommendStep, 28).GetComponent<Image>();

            worksPanel = CreatePanel(column.transform, "Works panel");
            wallPanel = CreatePanel(column.transform, "Wall panel");
            recommendPanel = CreatePanel(column.transform, "Recommend panel");
            workList = CreatePanel(worksPanel.transform, "Work list").GetComponent<RectTransform>();

            CreateButton(worksPanel.transform, uiFont, "All", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(16f, 590f), new Vector2(250f, 70f), ShowAll, 28);
            CreateButton(worksPanel.transform, uiFont, "Blue", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(282f, 590f), new Vector2(250f, 70f), ShowBlue, 28);
            CreateButton(worksPanel.transform, uiFont, "Calm", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(548f, 590f), new Vector2(250f, 70f), ShowCalm, 28);
            CreateButton(worksPanel.transform, uiFont, "Fits 50", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(814f, 590f), new Vector2(250f, 70f), ShowFitsFifty, 28);
            RefreshWorkList();

            if (Application.isEditor)
            {
                choicePanel = CreatePanel(column.transform, "Choose room");
                CreateButton(choicePanel.transform, uiFont, "Room 1. Warm. Small ochre room, wall 2.4 m by 2.4 m.", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 980f), new Vector2(1032f, 150f), ShowWarmRoom, 32);
                CreateButton(choicePanel.transform, uiFont, "Room 2. Gallery. White room, wall 4.2 m by 3.2 m.", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 800f), new Vector2(1032f, 150f), ShowGalleryRoom, 32);
                CreateButton(choicePanel.transform, uiFont, "Room 3. Hall. Dark room, wall 7 m by 4 m.", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 620f), new Vector2(1032f, 150f), ShowHallRoom, 32);
                CreateButton(wallPanel.transform, uiFont, "Change room", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 500f), new Vector2(1032f, 80f), ChangeRoom, 28);
            }
            notesButton = CreateButton(wallPanel.transform, uiFont, "Notes", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 340f), new Vector2(1032f, 100f), ShowNotes, 36).gameObject;
            concernField = CreateInput(wallPanel.transform, uiFont);
            sendButton = CreateButton(wallPanel.transform, uiFont, "Send", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 200f), new Vector2(240f, 110f), Send).gameObject;
            placeButton = CreateButton(wallPanel.transform, uiFont, "Place", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 40f), new Vector2(320f, 140f), Place);
            swapButton = CreateButton(wallPanel.transform, uiFont, "Swap", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(320f, 140f), Swap).gameObject;
            removeButton = CreateButton(wallPanel.transform, uiFont, "Remove", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 40f), new Vector2(320f, 140f), Remove).gameObject;

            CreateButton(recommendPanel.transform, uiFont, "Keep", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 320f), new Vector2(320f, 120f), Keep);
            CreateButton(recommendPanel.transform, uiFont, "Save", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 320f), new Vector2(320f, 120f), Save);
            CreateButton(recommendPanel.transform, uiFont, "Dismiss", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 320f), new Vector2(320f, 120f), Dismiss);
            CreateButton(recommendPanel.transform, uiFont, "Use 1", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 180f), new Vector2(320f, 120f), ChooseFirst);
            CreateButton(recommendPanel.transform, uiFont, "Use 2", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 180f), new Vector2(320f, 120f), ChooseSecond);
            CreateButton(recommendPanel.transform, uiFont, "Use 3", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 180f), new Vector2(320f, 120f), ChooseThird);
            ApplyStepVisibility();
            if (Application.isEditor && !roomChosen)
                status.text = ChooseRoomMessage;
            else if (!Application.isEditor)
            {
                scanning = true;
                wallReady = false;
                OpenStep(PrototypeScreen.Wall);
                status.text = ScanWallMessage;
            }

            canvasObject.SetActive(false);
            OpeningSequence.Play(uiFont, () =>
            {
                if (canvasObject != null)
                    canvasObject.SetActive(true);
            });
        }

        static RectTransform CreateShade(Transform parent)
        {
            GameObject shade = new GameObject("Status shade", typeof(RectTransform));
            shade.transform.SetParent(parent, false);
            Image image = shade.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.62f);
            image.raycastTarget = false;
            RectTransform rect = shade.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 280f);
            rect.anchoredPosition = Vector2.zero;
            return rect;
        }

        static Image CreateAimMark(Transform parent)
        {
            GameObject mark = new GameObject("Aim", typeof(RectTransform));
            mark.transform.SetParent(parent, false);
            Image image = mark.AddComponent<Image>();
            Texture2D texture = AimTexture();
            image.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            image.color = AimSearching;
            image.raycastTarget = false;
            RectTransform rect = mark.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(96f, 96f);
            rect.anchoredPosition = Vector2.zero;
            mark.SetActive(false);
            return image;
        }

        static Texture2D AimTexture()
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(1f, 1f, 1f, 0f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int dx = x - 31;
                    int dy = y - 31;
                    int distance = dx * dx + dy * dy;
                    bool ring = distance >= 22 * 22 && distance <= 28 * 28;
                    texture.SetPixel(x, y, ring ? Color.white : clear);
                }
            }

            texture.Apply();
            return texture;
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
