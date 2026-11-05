using System;
using System.Collections;
using System.Collections.Generic;
using Capstone.Catalogue;
using Capstone.Dialogue;
using Capstone.Ranking;
using Capstone.Session;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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
    /// Use 1, Use 2, or Use 3 starts placement of that recommended work.
    /// Notes reads the story stored on the current work and does not invent one.
    /// Keep, save, dismiss, a matched question, and a chosen rank are stored on this session after consent.
    /// A keep, save, or dismiss also stores the recommended artwork ids in rank order.
    /// Next walks the current list in stored order and brings that work's own centimetres.
    /// All, Blue, Calm, and Fits 50 change that list. A filtered work keeps its stored size.
    /// Study switches to the catalogue-only condition and back. The session starts as the prototype.
    /// Works, Wall, and Recommend are the three steps. The catalogue condition stays on Works.
    /// Consent must be marked before a keep, save, dismiss, matched question, or chosen rank is stored.
    /// No wall means a retry message and no invented size.
    /// In the editor, a chosen work is placed on the plain wall. On a phone, the camera opens so that work can be placed on a real wall. The artwork keeps its stored centimetres.
    /// The app opens on blue. A white ball bounces on each word and turns it white, then comes toward the camera until the page is white. The blue circle stays until that circle is tapped. The tap asks for a consent code. A matching code starts this session. Any other code asks for another attempt. The buyer is asked whether they already have a piece. A piece they bring asks for a clear picture, the painted width, the painted height, and a frame, then goes to the wall at the outer size. Exploring answers three questions. An answer stores a style, a mood, or a colour. I don't know leaves that category open. The catalogue opens on those matches as the top picks. Browse for more lists every piece. The camera opens when a chosen work is placed. After the work is on the wall the buyer may ask one thing or skip. Keep, save, dismiss, or ask for alternatives ranks three other works with their story notes. The buyer can answer the questions again and place another work, or end. The five uncertainty questions stay on the paper form.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public sealed class ArtworkPlacementController : MonoBehaviour
    {
        public const string WaitingMessage = "Point the centre of the screen at a wall, then press Place.";
        public const string NoWallMessage = "Sweep across the wall so a corner or frame is in view, then try again.";
        public const string PlacedMessage = "Placed on the wall.";
        public const string MovedMessage = "Moved on the wall. The size is unchanged.";
        public const string RemovedMessage = "Removed. You can place it again.";
        public const string SwapNeedsPlacementMessage = "Place a work before swapping.";
        public const string SwappedMessage = "Swapped the work. The size follows the new piece.";
        public const string ChooseNeedsRankMessage = "Keep, save, dismiss, or ask for alternatives before choosing a work.";
        public const string ChooseMissingMessage = "That rank is not in the list.";
        public const string ChosenMessage = "Placing the recommended work.";
        public const string NextMessage = "Next work.";
        public const string NextOnWallMessage = "Replaced the wall preview with the next work.";
        public const string NoCatalogueMessage = "No work is in the catalogue.";
        public const string NoMatchMessage = "No work matches this filter.";
        public const string ConsentMessage = "Consent is marked. Later actions can be stored.";
        public const string NotLoggedMessage = "Not logged. Consent is not marked.";
        public const string WorksStepMessage = "Choose a work. The size and story are stored on it.";
        public const string ScanWallMessage = "Sweep slowly. A corner or a frame helps, then hold the circle on the open wall.";
        public const string FloorMessage = "That is the floor. Lift the phone and put the circle on the wall.";
        public const string FurnitureMessage = "That is furniture. Hold the circle on the open wall.";
        public const string HoldWallMessage = "Hold the circle still on the open wall.";
        public const string WallFoundMessage = "Wall detected. You are ready to test the art piece. Press Place.";
        public const string HoldingWallMessage = "Wall detected. Saving this view.";
        public const string WallHeldMessage = "This view is saved. You can lower the phone.";
        public const string WallStepMessage = "Press Place. The work keeps its stored centimetres.";
        public const string RecommendStepMessage = "Keep, save, dismiss, or ask for alternatives, then choose a recommended work.";
        public const string BuyerPieceId = "buyer-piece";
        public const string TagsMessage = "What are you looking for?";
        public const string OwnPieceMessage = "Width and height are the painted surface, in centimetres.";
        public const string PictureMessage = "Upload a clear picture of the painting.";
        public const string PictureAddedMessage = "Picture added. Enter the painted width and height.";
        public const string PictureReadMessage = "That picture could not be read. Use a clear JPG or PNG.";
        public const string OwnPieceSizeMessage = "Enter the painting width and height in centimetres.";
        public const string OwnPieceFrameMessage = "Enter the frame width in centimetres.";
        public const string MatchMessage = "Top picks for what you said.";
        public const string NoQuizMatchMessage = "Nothing matches every answer. Browse for more.";
        public const string BrowseMessage = "Every piece in the catalogue.";
        public const string AskOrSkipMessage = "Ask one thing, or skip.";
        public const string SkippedMessage = "Skipped. Keep, save, dismiss, or ask for alternatives.";
        public const string EndedMessage = "Session ended. The five uncertainty questions stay on the paper form.";

        /// <summary>
        /// Opens the four trial pictures straight away. Set this to false to play the opening, the consent code, and the questions.
        /// </summary>
        public const bool SkipOpeningForPreview = true;

        static readonly List<ARRaycastHit> Hits = new List<ARRaycastHit>();
        static readonly List<ARPlane> PlaneChoices = new List<ARPlane>();
        static readonly PlaneClassifications WallKinds =
            PlaneClassifications.WallFace
            | PlaneClassifications.InvisibleWallFace
            | PlaneClassifications.InnerWallFace
            | PlaneClassifications.WallArt
            | PlaneClassifications.DoorFrame
            | PlaneClassifications.WindowFrame;
        static readonly PlaneClassifications FurnitureKinds =
            PlaneClassifications.Couch
            | PlaneClassifications.Seat
            | PlaneClassifications.Table
            | PlaneClassifications.Floor
            | PlaneClassifications.Ceiling;
        const float MinWallDistance = 0.45f;
        const float MaxWallDistance = 5.5f;
        const float WallSettleSeconds = 0.25f;
        static readonly Vector2[] AimSpots =
        {
            new Vector2(0.50f, 0.55f),
            new Vector2(0.50f, 0.68f),
            new Vector2(0.28f, 0.55f),
            new Vector2(0.72f, 0.55f),
            new Vector2(0.50f, 0.40f),
            new Vector2(0.18f, 0.42f),
            new Vector2(0.82f, 0.42f),
        };

        ARRaycastManager raycastManager;
        ARPlaneManager planeManager;
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
        ScrollRect workScroll;
        Image worksNavImage;
        Image wallNavImage;
        Image recommendNavImage;
        Text stepLine;
        Text studyLabel;
        DemoRoomStage rooms;
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
        bool wallHeld;
        bool savingView;
        bool triedSave;
        bool aimingAtFloor;
        bool aimingAtFurniture;
        bool wallSettling;
        TrackableId pendingPlane;
        float pendingSince;
        Vector3 heldPoint;
        Vector3 heldNormal;
        Vector3 heldCameraPosition;
        Quaternion heldCameraRotation;
        GameObject frozenCanvas;
        RawImage frozenPhoto;
        RawImage frozenPiece;
        Texture2D savedPhoto;
        readonly List<MonoBehaviour> pausedTracking = new List<MonoBehaviour>();
        bool tagsChosen;
        bool ended;
        string pathStep = BuyerPath.Start();
        GameObject skipButton;
        PreferenceQuiz quiz = new PreferenceQuiz();
        bool narrowed;
        bool browsing;
        bool enteringOwnPiece;
        bool framed;
        Texture2D buyerTexture;
        Texture2D buyerPhoto;
        Text pictureLabel;
        Color frameColor = new Color(0.12f, 0.10f, 0.08f, 1f);
        GameObject ownPiecePanel;
        GameObject frameDetails;
        Text widthValue;
        Text heightValue;
        Text mouldingValue;
        Text selectedValue;
        Image widthBox;
        Image heightBox;
        Image mouldingBox;
        GameObject tagsPanel;
        Text quizPrompt;
        Button[] quizButtons = new Button[4];
        Text[] quizLabels = new Text[4];
        Image[] quizImages = new Image[4];
        GameObject browseButton;
        GameObject topPicksButton;
        GameObject browseFilters;
        Text catalogueHeading;
        Text catalogueSubtitle;
        RectTransform workViewRect;
        CanvasScaler screenFit;
        RectTransform screenColumn;
        static readonly Color AimSearching = Color.white;
        static readonly Color AimReady = new Color(0.25f, 0.9f, 0.4f, 1f);

        static readonly Color PageInk = new Color(0.07f, 0.18f, 0.42f, 1f);
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
            planeManager = FindAnyObjectByType<ARPlaneManager>();
            arCamera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
            EnsurePointer();
            PrepareTracking();
            SilenceTemplateSpawner();
            current = SampleArtwork.Create();
            shown = CatalogueFilter.Matching(CatalogueSource(), null, null, null, 0f, 0f);
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
            TypeIntoSelectedMeasure();
            if (!scanning || status == null || step != PrototypeScreen.Wall)
                return;
            if (savingView || wallHeld)
                return;

            ARRaycastHit hit;
            ARPlane plane;
            bool found = TryHitVerticalWall(out hit, out plane);
            if (found && !triedSave)
            {
                triedSave = true;
                savingView = true;
                wallReady = true;
                status.text = HoldingWallMessage;
                if (aimMark != null)
                    aimMark.color = AimReady;
                ApplyStepVisibility();
                StartCoroutine(SaveWallView());
                return;
            }
            string next = found
                ? WallFoundMessage
                : wallSettling
                    ? HoldWallMessage
                    : aimingAtFurniture
                        ? FurnitureMessage
                        : aimingAtFloor
                            ? FloorMessage
                            : ScanWallMessage;
            HideTrackingDots();
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

            if (wallHeld)
            {
                PlaceOn(heldPoint, heldNormal);
                status.text = WallHeldMessage + " " + Describe(current) + " " + AskOrSkipMessage;
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
            scanning = false;
            wallReady = false;
            pathStep = BuyerPath.Next(pathStep, BuyerPath.Placed);
            status.text = "On the wall. " + Describe(current) + " Ask one thing, or skip.";
            ApplyStepVisibility();
        }

        void PlaceAskBar()
        {
            if (concernField != null)
            {
                RectTransform field = concernField.GetComponent<RectTransform>();
                field.anchoredPosition = new Vector2(24f, 160f);
                field.sizeDelta = new Vector2(1032f, 110f);
            }

            PlaceBottom(skipButton, 24f, "Skip");
            PlaceBottom(sendButton, 556f, "Send");
        }

        static void PlaceBottom(GameObject widget, float x, string label)
        {
            if (widget == null)
                return;
            RectTransform rect = widget.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(x, 28f);
            rect.sizeDelta = new Vector2(500f, 110f);
            Text text = widget.GetComponentInChildren<Text>();
            if (text != null)
                text.text = label;
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

            OpenWall();
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
            pathStep = BuyerPath.Next(pathStep, BuyerPath.Send);
            KeepTranscript();
            if (pathStep == BuyerPath.Rank)
                OpenStep(PrototypeScreen.Recommend);
            status.text = ReplyForCurrentWork(reply) + " Keep, save, dismiss, or ask for alternatives.";
            ApplyStepVisibility();
        }

        public void SkipQuestion()
        {
            if (Blocked(StudyCondition.Dialogue))
                return;

            scanning = false;
            pathStep = BuyerPath.Next(pathStep, BuyerPath.Skip);
            if (pathStep == BuyerPath.Rank)
                OpenStep(PrototypeScreen.Recommend);
            status.text = SkippedMessage;
        }

        public void ConfirmTags()
        {
            ShowMatches();
        }

        void AnswerQuiz(string tag)
        {
            quiz.Answer(tag);
            if (quiz.Finished)
            {
                if (quiz.BringingOwnPiece)
                    ShowOwnPieceForm();
                else
                    ShowMatches();
                return;
            }

            ShowQuestion();
            if (status != null)
                status.text = quiz.Prompt();
        }

        void ShowMatches()
        {
            feedback = new FeedbackLoop(PreferenceProfile.FromInitialTags(quiz.Style, quiz.Mood, quiz.Colour));
            tagsChosen = true;
            ended = false;
            narrowed = quiz.AnyAnswered();
            browsing = !narrowed;
            pathStep = BuyerPath.Next(pathStep, BuyerPath.Continue);
            UseFilter(Blank(quiz.Style), Blank(quiz.Mood), Blank(quiz.Colour), 0f, 0f);
            OpenStep(PrototypeScreen.Works);
            if (status != null)
                status.text = MatchLine();
        }

        string MatchLine()
        {
            if (!narrowed || browsing)
                return BrowseMessage;
            int count = shown != null ? shown.Count : 0;
            if (count == 0)
                return NoQuizMatchMessage;
            return MatchMessage;
        }

        static string Blank(string tag)
        {
            return string.IsNullOrWhiteSpace(tag) ? null : tag;
        }

        public void BeginPlacement()
        {
            if (current == null || shown == null || shown.Count == 0)
            {
                status.text = shown != null && shown.Count == 0 ? NoMatchMessage : NoCatalogueMessage;
                return;
            }

            if (Application.isEditor && rooms != null && !roomChosen)
                ShowDemoRoom(0);
            else
                OpenWall();

            if (Application.isEditor && roomChosen)
                Place();
        }

        public void ChangeTags()
        {
            quiz.Restart();
            tagsChosen = false;
            narrowed = false;
            browsing = false;
            ended = false;
            scanning = false;
            pathStep = BuyerPath.Next(BuyerPath.Rank, BuyerPath.ChangeTags);
            ShowQuestion();
            OpenStep(PrototypeScreen.Works);
            status.text = quiz.Prompt();
        }

        public void EndSession()
        {
            ended = true;
            scanning = false;
            pathStep = BuyerPath.Next(BuyerPath.Rank, BuyerPath.End);
            ApplyStepVisibility();
            status.text = EndedMessage;
            if (stepLine != null)
                stepLine.text = EndedMessage;
        }

        void OpenWall()
        {
            if (SkipOpeningForPreview && (shown == null || shown.Count == 0 || current == null || current.Id == SampleArtwork.FirstId))
            {
                StartPreview();
                return;
            }

            pathStep = BuyerPath.Next(BuyerPath.Catalogue, BuyerPath.Place);
            if (!Application.isEditor && BuyerPath.OpensCamera(pathStep))
            {
                if (wallHeld)
                {
                    scanning = false;
                    OpenStep(PrototypeScreen.Wall);
                    PlaceOn(heldPoint, heldNormal);
                    status.text = WallHeldMessage + " " + Describe(current) + " " + AskOrSkipMessage;
                    return;
                }

                scanning = true;
                wallReady = false;
                pendingPlane = TrackableId.invalidId;
                wallSettling = false;
            }

            OpenStep(PrototypeScreen.Wall);
        }

        void KeepTranscript()
        {
            if (session == null)
                return;

            try
            {
                string path = System.IO.Path.Combine(Application.persistentDataPath, "capstone-session.txt");
                System.IO.File.WriteAllText(path, SessionTranscript.Format(session));
            }
            catch (System.IO.IOException)
            {
                // The session object still holds the events.
            }
            catch (System.UnauthorizedAccessException)
            {
                // The session object still holds the events.
            }
        }

        static System.Collections.Generic.IReadOnlyList<Artwork> CatalogueSource()
        {
            return SkipOpeningForPreview ? TrialPieces.All : SampleCatalogue.All;
        }

        void StartPreview()
        {
            BuyerSession started;
            if (BuyerSession.TryStart("AZURE", out started))
                session = started;
            tagsChosen = true;
            narrowed = false;
            browsing = true;
            ended = false;
            enteringOwnPiece = false;
            quiz.Restart();
            pathStep = BuyerPath.Catalogue;
            shown = new System.Collections.Generic.List<Artwork>(TrialPieces.All);
            if (shown.Count > 0)
                current = shown[0];
            feedback = new FeedbackLoop(PreferenceProfile.FromInitialTags(null, null, null));
            OpenStep(PrototypeScreen.Works);
            PaintCatalogueHeading();
            RefreshWorkList();
        }

        void StartBuyerPath()
        {
            ReleaseSavedWall();
            scanning = false;
            wallReady = false;
            tagsChosen = false;
            narrowed = false;
            browsing = false;
            ended = false;
            quiz.Restart();
            pathStep = BuyerPath.Start();
            ShowQuestion();
            OpenStep(PrototypeScreen.Works);
            if (status != null)
                status.text = quiz.Prompt();
        }

        void ShowOwnPieceForm()
        {
            enteringOwnPiece = true;
            for (int i = 0; i < quizButtons.Length; i++)
            {
                if (quizButtons[i] != null)
                    quizButtons[i].gameObject.SetActive(false);
            }

            if (ownPiecePanel != null)
            {
                ownPiecePanel.SetActive(true);
                ownPiecePanel.transform.SetAsLastSibling();
            }

            if (quizPrompt != null)
            {
                quizPrompt.text = PictureMessage;
                quizPrompt.rectTransform.anchoredPosition = new Vector2(0f, -300f);
                quizPrompt.rectTransform.sizeDelta = new Vector2(960f, 140f);
                quizPrompt.transform.SetAsLastSibling();
            }

            SelectMeasure(heightValue, heightBox);
            if (pictureLabel != null)
                pictureLabel.text = buyerPhoto != null ? "Picture added" : "Upload a clear picture";

            if (status != null)
                status.text = buyerPhoto != null ? PictureAddedMessage : PictureMessage;
            ApplyStepVisibility();
        }

        public void TryOwnPiece()
        {
            if (buyerPhoto == null)
            {
                status.text = PictureMessage;
                return;
            }

            float width;
            float height;
            if (!TryCentimetres(widthValue != null ? widthValue.text : string.Empty, out width)
                || !TryCentimetres(heightValue != null ? heightValue.text : string.Empty, out height))
            {
                status.text = OwnPieceSizeMessage;
                return;
            }

            float moulding = 0f;
            if (framed && !TryCentimetres(mouldingValue != null ? mouldingValue.text : string.Empty, out moulding))
            {
                status.text = OwnPieceFrameMessage;
                return;
            }

            if (buyerTexture != null)
                Destroy(buyerTexture);
            buyerTexture = PaintBuyerPiece(width, height, framed ? moulding : 0f, frameColor, buyerPhoto);
            float outerWidth = width + (framed ? moulding * 2f : 0f);
            float outerHeight = height + (framed ? moulding * 2f : 0f);
            current = new Artwork(
                BuyerPieceId,
                "Your piece",
                string.Empty,
                0,
                "Painting",
                outerHeight,
                outerWidth,
                string.Empty,
                string.Empty,
                string.Empty,
                "none",
                string.Empty,
                StoryNotes.Empty);
            if (surface != null)
            {
                surface.SetTexture("_BaseMap", buyerTexture);
                surface.SetTexture("_MainTex", buyerTexture);
            }

            enteringOwnPiece = false;
            tagsChosen = true;
            narrowed = false;
            browsing = false;
            feedback = new FeedbackLoop(PreferenceProfile.FromInitialTags(string.Empty, string.Empty, string.Empty));
            pathStep = BuyerPath.Next(BuyerPath.Tags, BuyerPath.Continue);
            BeginPlacement();
        }

        public void ChooseBarePiece()
        {
            framed = false;
            if (frameDetails != null)
                frameDetails.SetActive(false);
            if (selectedValue == mouldingValue)
                SelectMeasure(heightValue, heightBox);
        }

        public void ChooseFramedPiece()
        {
            framed = true;
            if (frameDetails != null)
                frameDetails.SetActive(true);
            SelectMeasure(mouldingValue, mouldingBox);
        }

        public void ChooseBlackFrame()
        {
            frameColor = new Color(0.12f, 0.10f, 0.08f, 1f);
        }

        public void ChooseWhiteFrame()
        {
            frameColor = new Color(0.93f, 0.91f, 0.86f, 1f);
        }

        public void ChooseOakFrame()
        {
            frameColor = new Color(0.55f, 0.36f, 0.18f, 1f);
        }

        public void ChooseGoldFrame()
        {
            frameColor = new Color(0.72f, 0.58f, 0.28f, 1f);
        }

        static bool TryCentimetres(string text, out float centimetres)
        {
            centimetres = 0f;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            string trimmed = text.Trim().Replace(',', '.');
            return float.TryParse(
                    trimmed,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out centimetres)
                && centimetres > 0f;
        }

        static Texture2D PaintBuyerPiece(float paintingWidthCm, float paintingHeightCm, float mouldingCm, Color frame, Texture2D photo)
        {
            float outerWidth = paintingWidthCm + mouldingCm * 2f;
            float outerHeight = paintingHeightCm + mouldingCm * 2f;
            const int width = 256;
            int height = Mathf.Clamp(Mathf.RoundToInt(width * outerHeight / outerWidth), 64, 512);
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            int borderX = mouldingCm <= 0f ? 0 : Mathf.Clamp(Mathf.RoundToInt(width * mouldingCm / outerWidth), 1, (width / 2) - 1);
            int borderY = mouldingCm <= 0f ? 0 : Mathf.Clamp(Mathf.RoundToInt(height * mouldingCm / outerHeight), 1, (height / 2) - 1);
            Color[] pixels = photo.GetPixels();
            int photoWidth = photo.width;
            int photoHeight = photo.height;
            int innerWidth = Mathf.Max(1, width - (borderX * 2));
            int innerHeight = Mathf.Max(1, height - (borderY * 2));
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool framePixel = x < borderX || y < borderY || x >= width - borderX || y >= height - borderY;
                    if (framePixel)
                    {
                        texture.SetPixel(x, y, frame);
                        continue;
                    }

                    int sampleX = Mathf.Clamp((x - borderX) * photoWidth / innerWidth, 0, photoWidth - 1);
                    int sampleY = Mathf.Clamp((y - borderY) * photoHeight / innerHeight, 0, photoHeight - 1);
                    texture.SetPixel(x, y, pixels[(sampleY * photoWidth) + sampleX]);
                }
            }

            texture.Apply();
            return texture;
        }

        public void UploadPainting()
        {
#if UNITY_EDITOR
            string path = UnityEditor.EditorUtility.OpenFilePanelWithFilters(
                "Clear picture of the painting",
                "",
                new[] { "Image files", "png,jpg,jpeg" });
            OnPaintingPicked(path);
#elif UNITY_IOS
            CapstonePickPainting(gameObject.name);
#elif UNITY_ANDROID
            OpenAndroidPicker();
#else
            if (status != null)
                status.text = PictureMessage;
#endif
        }

        public void OnPaintingPicked(string path)
        {
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                if (status != null && buyerPhoto == null)
                    status.text = PictureMessage;
                return;
            }

            byte[] bytes;
            try
            {
                bytes = System.IO.File.ReadAllBytes(path);
            }
            catch (System.IO.IOException)
            {
                if (status != null)
                    status.text = PictureReadMessage;
                return;
            }

            Texture2D loaded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!loaded.LoadImage(bytes))
            {
                Destroy(loaded);
                if (status != null)
                    status.text = PictureReadMessage;
                return;
            }

            Texture2D scaled = ScalePhoto(loaded);
            if (scaled != loaded)
                Destroy(loaded);
            if (buyerPhoto != null)
                Destroy(buyerPhoto);
            buyerPhoto = scaled;
            if (pictureLabel != null)
                pictureLabel.text = "Picture added";
            if (status != null)
                status.text = PictureAddedMessage;
        }

        static Texture2D ScalePhoto(Texture2D source)
        {
            const int maxEdge = 1024;
            int longEdge = Mathf.Max(source.width, source.height);
            if (longEdge <= maxEdge)
                return source;

            float scale = maxEdge / (float)longEdge;
            int width = Mathf.Max(1, Mathf.RoundToInt(source.width * scale));
            int height = Mathf.Max(1, Mathf.RoundToInt(source.height * scale));
            RenderTexture target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(source, target);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            Texture2D copy = new Texture2D(width, height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            copy.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            return copy;
        }

#if UNITY_IOS && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        static extern void CapstonePickPainting(string gameObjectName);
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        void OpenAndroidPicker()
        {
            using (AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent"))
            {
                string packageName = activity.Call<string>("getPackageName");
                intent.Call<AndroidJavaObject>("setClassName", packageName, "com.cinna03.capstone.picker.PaintingPickActivity");
                intent.Call<AndroidJavaObject>("putExtra", "gameObject", gameObject.name);
                activity.Call("startActivity", intent);
            }
        }
#endif

        void ShowQuestion()
        {
            enteringOwnPiece = false;
            if (ownPiecePanel != null)
                ownPiecePanel.SetActive(false);
            if (quizPrompt != null)
            {
                quizPrompt.text = quiz.Prompt();
                quizPrompt.rectTransform.anchoredPosition = new Vector2(0f, -340f);
                quizPrompt.rectTransform.sizeDelta = new Vector2(960f, 220f);
            }

            PreferenceQuiz.Choice[] choices = quiz.CurrentChoices();
            for (int i = 0; i < quizButtons.Length; i++)
            {
                Button button = quizButtons[i];
                if (button == null)
                    continue;

                bool show = i < choices.Length;
                button.gameObject.SetActive(show);
                if (!show)
                    continue;

                PreferenceQuiz.Choice choice = choices[i];
                RectTransform rect = button.GetComponent<RectTransform>();
                int fromTop = choices.Length - 1 - i;
                rect.anchoredPosition = new Vector2(24f, 80f + (fromTop * 160f));
                if (quizLabels[i] != null)
                    quizLabels[i].text = choice.Label;

                bool unknown = string.IsNullOrEmpty(choice.Tag);
                if (quizImages[i] != null)
                    quizImages[i].color = unknown ? new Color(0.75f, 0.82f, 0.90f, 1f) : StepOn;
                if (quizLabels[i] != null)
                    quizLabels[i].color = unknown ? new Color(0.07f, 0.18f, 0.42f, 1f) : Color.white;

                string tag = choice.Tag;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => AnswerQuiz(tag));
            }
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

        public void AskForAlternatives()
        {
            RecordFeedback("Alternatives for", "Alternatives", loop => loop.RequestAlternatives(current));
        }

        void RecordFeedback(string verb, string action, System.Action<FeedbackLoop> apply)
        {
            if (Blocked(StudyCondition.Ranking))
                return;
            if (feedback == null || current == null)
                return;
            apply(feedback);
            offered = Recommender.RankThree(CatalogueSource(), feedback.Profile, current.Id);
            string[] recommendedIds = IdsInRankOrder(offered);
            bool logged = session != null && session.LogAction(
                current.Id,
                action,
                feedback.Profile.LastConcern,
                DateTime.UtcNow,
                recommendedIds);
            status.text = verb + " " + current.Title + ".\n" + DescribeRecommendations(offered)
                + "\n" + (logged ? LoggedRanks(recommendedIds) : NotLoggedMessage);
            KeepTranscript();
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

        public void ShowFirstNotes()
        {
            ShowRankNotes(1);
        }

        public void ShowSecondNotes()
        {
            ShowRankNotes(2);
        }

        public void ShowThirdNotes()
        {
            ShowRankNotes(3);
        }

        public void ShowRankNotes(int oneBasedRank)
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

            status.text = ArtworkDetail.Describe(chosen);
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
            KeepTranscript();
            if (shown == null || shown.Count == 0)
            {
                status.text = (logged ? "Logged. " : NotLoggedMessage + " ") + ChosenMessage;
                return;
            }

            if (placed != null)
            {
                Destroy(placed);
                placed = null;
            }

            pathStep = BuyerPath.Next(BuyerPath.Rank, BuyerPath.Place);
            BeginPlacement();
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
            browsing = true;
            UseFilter(null, null, null, 0f, 0f);
            ApplyStepVisibility();
            if (status != null)
                status.text = BrowseMessage;
        }

        public void ShowTopPicks()
        {
            browsing = false;
            narrowed = quiz.AnyAnswered();
            UseFilter(Blank(quiz.Style), Blank(quiz.Mood), Blank(quiz.Colour), 0f, 0f);
            ApplyStepVisibility();
            if (status != null)
                status.text = MatchLine();
        }

        public void ShowBlue()
        {
            browsing = true;
            UseFilter(null, null, "blue", 0f, 0f);
        }

        public void ShowCalm()
        {
            browsing = true;
            UseFilter(null, "calm", null, 0f, 0f);
        }

        public void ShowFitsFifty()
        {
            browsing = true;
            UseFilter(null, null, null, 50f, 50f);
        }

        void UseFilter(string style, string mood, string colour, float maxWidthCm, float maxHeightCm)
        {
            shown = CatalogueFilter.Matching(CatalogueSource(), style, mood, colour, maxWidthCm, maxHeightCm);
            offered = null;
            if (shown.Count == 0)
            {
                RefreshWorkList();
                PaintCatalogueHeading();
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
            PaintCatalogueHeading();
            status.text = WorksStatus();
        }

        string CatalogueLine()
        {
            int count = shown != null ? shown.Count : 0;
            if (count == 0)
                return NoQuizMatchMessage;
            if (!narrowed || browsing)
                return count + " pieces.";
            return count == 1 ? "1 piece matches what you said." : count + " pieces match what you said.";
        }

        void PaintCatalogueHeading()
        {
            if (catalogueHeading == null)
                return;

            catalogueHeading.text = SkipOpeningForPreview
                ? "Pieces to try"
                : (narrowed && !browsing ? "Top picks" : "All pieces");
            if (catalogueSubtitle != null)
                catalogueSubtitle.text = CatalogueLine();
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
            bool phoneScan = !Application.isEditor && tagsChosen && scanning && step == PrototypeScreen.Wall && !ended;
            bool showTags = !tagsChosen && !ended;
            if (tagsPanel != null)
                tagsPanel.SetActive(showTags);
            SetButtonActive(worksNavImage, false);
            SetButtonActive(wallNavImage, false);
            SetButtonActive(recommendNavImage, false);
            SetButtonActive(studyButtonWidget, false);
            SetButtonActive(consentButtonWidget, false);
            bool onWorks = tagsChosen && !ended && !phoneScan && PrototypeScreen.ShowsWorks(step);
            if (worksPanel != null)
                worksPanel.SetActive(onWorks);
            bool asking = pathStep == BuyerPath.Question && step == PrototypeScreen.Wall && !phoneScan && !ended;
            if (statusShade != null)
                statusShade.gameObject.SetActive(!onWorks);
            if (status != null)
                status.gameObject.SetActive(!onWorks);
            bool filtersOpen = (browsing || !narrowed) && !SkipOpeningForPreview;
            SetObjectActive(browseButton, onWorks && narrowed && !browsing);
            SetObjectActive(topPicksButton, onWorks && narrowed && browsing);
            SetObjectActive(browseFilters, onWorks && filtersOpen);
            if (workViewRect != null)
            {
                workViewRect.offsetMin = new Vector2(28f, filtersOpen ? 356f : 260f);
                workViewRect.offsetMax = new Vector2(-28f, -160f);
            }
            if (wallPanel != null)
                wallPanel.SetActive(tagsChosen && !ended && PrototypeScreen.ShowsWall(step));
            if (recommendPanel != null)
                recommendPanel.SetActive(tagsChosen && !ended && !phoneScan && PrototypeScreen.ShowsRecommend(step));
            SetObjectActive(notesButton, false);
            SetObjectActive(sendButton, asking);
            SetObjectActive(skipButton, asking);
            SetObjectActive(swapButton, false);
            SetObjectActive(removeButton, false);
            if (concernField != null)
                concernField.gameObject.SetActive(asking);
            if (asking)
                PlaceAskBar();
            if (placeButton != null)
            {
                placeButton.gameObject.SetActive(phoneScan && wallReady && !wallHeld);
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
                statusShade.sizeDelta = new Vector2(0f, phoneScan ? 160f : (asking ? 120f : 200f));
            if (stepLine != null)
            {
                stepLine.gameObject.SetActive(false);
                stepLine.text = StepInstruction();
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
            if (ended)
                return EndedMessage;
            if (!tagsChosen && enteringOwnPiece)
                return "Tap a box. The keys type into the blue one.";
            if (!tagsChosen)
                return "I don't know leaves this category open.";
            if (narrowed && !browsing && step == PrototypeScreen.Works)
                return "These are the top picks. Browse for more shows every piece.";
            if (browsing && narrowed && step == PrototypeScreen.Works)
                return "Top picks returns to the matches.";
            if (step == PrototypeScreen.Wall && pathStep == BuyerPath.Question)
                return "On the wall. Ask one thing, or skip.";
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

        void ChooseOpeningWork(string id)
        {
            if (string.IsNullOrEmpty(id) || shown == null)
                return;

            for (int i = 0; i < shown.Count; i++)
            {
                if (shown[i] != null && shown[i].Id == id)
                {
                    SelectShown(i);
                    return;
                }
            }
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

            RefreshWorkList(false);
            status.text = WorksStatus();
        }

        void RefreshWorkList(bool resetScroll = true)
        {
            if (workList == null || uiFont == null)
                return;

            for (int child = workList.childCount - 1; child >= 0; child--)
                Destroy(workList.GetChild(child).gameObject);

            if (shown == null)
                return;

            const float row = 176f;
            const float card = 160f;
            float height = Mathf.Max(400f, (shown.Count * row) + 12f);
            workList.anchorMin = new Vector2(0f, 1f);
            workList.anchorMax = new Vector2(1f, 1f);
            workList.pivot = new Vector2(0.5f, 1f);
            workList.sizeDelta = new Vector2(0f, height);
            workList.anchoredPosition = Vector2.zero;

            for (int i = 0; i < shown.Count; i++)
            {
                Artwork work = shown[i];
                if (work == null)
                    continue;

                bool selected = current != null && work.Id == current.Id;
                CreateCatalogueCard(work, i, 8f + (i * row), card, selected);
            }

            if (resetScroll && workScroll != null)
                workScroll.verticalNormalizedPosition = 1f;
        }

        void CreateCatalogueCard(Artwork work, int index, float top, float height, bool selected)
        {
            GameObject cardObject = new GameObject(work.Title, typeof(RectTransform));
            cardObject.transform.SetParent(workList, false);
            Image image = cardObject.AddComponent<Image>();
            image.color = selected ? PageInk : Color.white;
            Button button = cardObject.AddComponent<Button>();
            button.targetGraphic = image;
            int captured = index;
            button.onClick.AddListener(() => SelectShown(captured));
            RectTransform rect = cardObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, height);
            rect.anchoredPosition = new Vector2(0f, -top);

            GameObject chip = new GameObject("Colour", typeof(RectTransform));
            chip.transform.SetParent(cardObject.transform, false);
            Texture2D picture = PictureFor(work);
            float inset = 140f;
            if (picture != null)
            {
                RawImage raw = chip.AddComponent<RawImage>();
                raw.texture = picture;
                raw.raycastTarget = false;
                float aspect = picture.width / (float)Mathf.Max(1, picture.height);
                float chipHeight = 120f;
                float chipWidth = Mathf.Min(200f, chipHeight * aspect);
                chipHeight = chipWidth / aspect;
                RectTransform chipRect = chip.GetComponent<RectTransform>();
                chipRect.anchorMin = new Vector2(0f, 0.5f);
                chipRect.anchorMax = new Vector2(0f, 0.5f);
                chipRect.pivot = new Vector2(0f, 0.5f);
                chipRect.sizeDelta = new Vector2(chipWidth, chipHeight);
                chipRect.anchoredPosition = new Vector2(24f, 0f);
                inset = 44f + chipWidth;
            }
            else
            {
                Image chipImage = chip.AddComponent<Image>();
                chipImage.color = FillFor(work);
                chipImage.raycastTarget = false;
                RectTransform chipRect = chip.GetComponent<RectTransform>();
                chipRect.anchorMin = new Vector2(0f, 0.5f);
                chipRect.anchorMax = new Vector2(0f, 0.5f);
                chipRect.pivot = new Vector2(0f, 0.5f);
                chipRect.sizeDelta = new Vector2(96f, 96f);
                chipRect.anchoredPosition = new Vector2(24f, 0f);
            }

            Color ink = selected ? Color.white : PageInk;
            Color meta = selected ? new Color(0.82f, 0.9f, 1f, 1f) : new Color(0.35f, 0.48f, 0.64f, 1f);
            Text title = CreateCardLine(cardObject.transform, work.Title, 32, ink, new Vector2(inset, 18f), new Vector2(-24f, -16f), new Vector2(0f, 0.42f), Vector2.one);
            title.alignment = TextAnchor.LowerLeft;
            string detail = work.Artist + ", " + work.Year + "   " + work.WidthCm.ToString("0") + " x " + work.HeightCm.ToString("0") + " cm";
            Text line = CreateCardLine(cardObject.transform, detail, 24, meta, new Vector2(inset, 16f), new Vector2(-24f, -8f), Vector2.zero, new Vector2(1f, 0.42f));
            line.alignment = TextAnchor.UpperLeft;
        }

        Text CreateCardLine(Transform parent, string message, int fontSize, Color color, Vector2 offsetMin, Vector2 offsetMax, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject label = new GameObject(message);
            label.transform.SetParent(parent, false);
            Text text = label.AddComponent<Text>();
            text.font = uiFont;
            text.fontSize = fontSize;
            text.color = color;
            text.text = message;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = anchorMin;
            textRect.anchorMax = anchorMax;
            textRect.offsetMin = offsetMin;
            textRect.offsetMax = offsetMax;
            return text;
        }

        static void PaintPrimary(GameObject widget)
        {
            if (widget == null)
                return;
            Image image = widget.GetComponent<Image>();
            if (image != null)
                image.color = PageInk;
            Text text = widget.GetComponentInChildren<Text>();
            if (text != null)
                text.color = Color.white;
        }

        static void PaintQuiet(GameObject widget)
        {
            if (widget == null)
                return;
            Image image = widget.GetComponent<Image>();
            if (image != null)
                image.color = Color.white;
            Text text = widget.GetComponentInChildren<Text>();
            if (text != null)
                text.color = PageInk;
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
            if (frozenPiece != null)
                frozenPiece.enabled = false;
            scanning = wallHeld ? false : !Application.isEditor;
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

        static void PrepareTracking()
        {
            ARPlaneManager planes = FindAnyObjectByType<ARPlaneManager>();
            if (planes != null)
                planes.requestedDetectionMode = PlaneDetectionMode.Horizontal | PlaneDetectionMode.Vertical;
            HideTrackingDots();
        }

        static void HideTrackingDots()
        {
            ARPointCloudManager clouds = FindAnyObjectByType<ARPointCloudManager>();
            if (clouds != null && clouds.enabled)
                clouds.enabled = false;
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

        IEnumerator SaveWallView()
        {
            if (!TryHitVerticalWall(out ARRaycastHit hit, out ARPlane plane) || arCamera == null)
            {
                savingView = false;
                triedSave = false;
                yield break;
            }

            Vector3 point = hit.pose.position;
            Vector3 normal = plane.normal;
            Vector3 cameraPosition = arCamera.transform.position;
            Quaternion cameraRotation = arCamera.transform.rotation;
            SetScanChrome(false);
            yield return new WaitForEndOfFrame();
            Texture2D photo = CapturePhotograph();
            SetScanChrome(true);
            if (photo == null || LooksBlank(photo))
            {
                if (photo != null)
                    Destroy(photo);
                savingView = false;
                if (status != null)
                    status.text = WallFoundMessage;
                ApplyStepVisibility();
                yield break;
            }

            heldPoint = point;
            heldNormal = normal;
            heldCameraPosition = cameraPosition;
            heldCameraRotation = cameraRotation;
            ShowFrozenPhotograph(photo);
            FreezeTracking();
            wallHeld = true;
            savingView = false;
            PlaceOn(point, normal);
            status.text = WallHeldMessage + " " + Describe(current) + " " + AskOrSkipMessage;
        }

        void SetScanChrome(bool visible)
        {
            if (aimMark != null)
                aimMark.enabled = visible;
            if (status != null)
                status.enabled = visible;
            if (statusShade != null)
                statusShade.gameObject.SetActive(visible);
            if (stepLine != null)
                stepLine.enabled = visible;
        }

        Texture2D CapturePhotograph()
        {
            Texture2D fromCamera = CaptureCameraFeed();
            if (fromCamera != null && !LooksBlank(fromCamera))
                return fromCamera;
            if (fromCamera != null)
                Destroy(fromCamera);
            return CaptureScreen();
        }

        Texture2D CaptureCameraFeed()
        {
            if (arCamera == null)
                return null;

            ARCameraBackground background = arCamera.GetComponent<ARCameraBackground>();
            if (background == null || background.material == null)
                return null;

            int width = Screen.width;
            int height = Screen.height;
            if (width < 2 || height < 2)
                return null;

            RenderTexture buffer = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            Texture source = background.material.HasProperty("_MainTex") ? background.material.GetTexture("_MainTex") : null;
            try
            {
                Graphics.Blit(source, buffer, background.material);
                RenderTexture.active = buffer;
                Texture2D photo = new Texture2D(width, height, TextureFormat.RGB24, false);
                photo.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
                photo.Apply();
                return photo;
            }
            catch (System.Exception)
            {
                return null;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(buffer);
            }
        }

        static Texture2D CaptureScreen()
        {
            int width = Screen.width;
            int height = Screen.height;
            if (width < 2 || height < 2)
                return null;

            Texture2D photo = new Texture2D(width, height, TextureFormat.RGB24, false);
            photo.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
            photo.Apply();
            return photo;
        }

        static bool LooksBlank(Texture2D photo)
        {
            if (photo == null || photo.width < 2 || photo.height < 2)
                return true;

            int[] xs = { photo.width / 4, photo.width / 2, (photo.width * 3) / 4 };
            int[] ys = { photo.height / 4, photo.height / 2, (photo.height * 3) / 4 };
            for (int x = 0; x < xs.Length; x++)
            {
                for (int y = 0; y < ys.Length; y++)
                {
                    Color pixel = photo.GetPixel(xs[x], ys[y]);
                    if (pixel.r + pixel.g + pixel.b > 0.05f)
                        return false;
                }
            }

            return true;
        }

        void ShowFrozenPhotograph(Texture2D photo)
        {
            if (savedPhoto != null)
                Destroy(savedPhoto);
            savedPhoto = photo;
            if (frozenCanvas == null)
            {
                frozenCanvas = new GameObject("Saved wall");
                Canvas canvas = frozenCanvas.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                CanvasScaler scaler = frozenCanvas.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = 1f;

                GameObject photoObject = new GameObject("Photograph", typeof(RectTransform));
                photoObject.transform.SetParent(frozenCanvas.transform, false);
                frozenPhoto = photoObject.AddComponent<RawImage>();
                frozenPhoto.raycastTarget = false;
                RectTransform photoRect = photoObject.GetComponent<RectTransform>();
                photoRect.anchorMin = Vector2.zero;
                photoRect.anchorMax = Vector2.one;
                photoRect.offsetMin = Vector2.zero;
                photoRect.offsetMax = Vector2.zero;

                GameObject pieceObject = new GameObject("Piece on wall", typeof(RectTransform));
                pieceObject.transform.SetParent(frozenCanvas.transform, false);
                frozenPiece = pieceObject.AddComponent<RawImage>();
                frozenPiece.raycastTarget = false;
                frozenPiece.enabled = false;
            }

            frozenCanvas.SetActive(true);
            frozenPhoto.texture = photo;
            frozenPhoto.enabled = true;
        }

        void ShowPieceOnPhoto(Artwork artwork)
        {
            if (!wallHeld || artwork == null || arCamera == null || frozenPiece == null)
                return;

            arCamera.transform.SetPositionAndRotation(heldCameraPosition, heldCameraRotation);
            WallPose pose = ArtworkWallPose.At(artwork, heldPoint, heldNormal, heldCameraPosition);
            FrozenWall.Corners(pose, out Vector3 bottomLeft, out Vector3 bottomRight, out Vector3 topLeft, out Vector3 topRight);
            Vector3 first = arCamera.WorldToScreenPoint(bottomLeft);
            Vector3 second = arCamera.WorldToScreenPoint(bottomRight);
            Vector3 third = arCamera.WorldToScreenPoint(topLeft);
            Vector3 fourth = arCamera.WorldToScreenPoint(topRight);
            if (first.z <= 0f || second.z <= 0f || third.z <= 0f || fourth.z <= 0f)
                return;

            ScreenSpan span = FrozenWall.Span(first, second, third, fourth);
            frozenPiece.texture = ImageFor(artwork);
            frozenPiece.enabled = true;
            RectTransform rect = frozenPiece.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(span.XMin, span.YMin);
            rect.sizeDelta = new Vector2(span.Width, span.Height);
            if (placed != null)
            {
                MeshRenderer renderer = placed.GetComponent<MeshRenderer>();
                if (renderer != null)
                    renderer.enabled = false;
            }
        }

        void FreezeTracking()
        {
            if (arCamera == null)
                return;

            ARCameraBackground background = arCamera.GetComponent<ARCameraBackground>();
            if (background != null && background.enabled)
            {
                background.enabled = false;
                pausedTracking.Add(background);
            }

            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || !behaviour.enabled)
                    continue;
                if (behaviour.GetType().Name.IndexOf("PoseDriver", System.StringComparison.Ordinal) >= 0)
                {
                    behaviour.enabled = false;
                    pausedTracking.Add(behaviour);
                }
            }

            arCamera.transform.SetPositionAndRotation(heldCameraPosition, heldCameraRotation);
        }

        void ReleaseSavedWall()
        {
            wallHeld = false;
            savingView = false;
            triedSave = false;
            for (int i = 0; i < pausedTracking.Count; i++)
            {
                if (pausedTracking[i] != null)
                    pausedTracking[i].enabled = true;
            }

            pausedTracking.Clear();
            if (frozenPiece != null)
                frozenPiece.enabled = false;
            if (frozenCanvas != null)
                frozenCanvas.SetActive(false);
            if (savedPhoto != null)
            {
                Destroy(savedPhoto);
                savedPhoto = null;
            }
        }

        bool TryHitVerticalWall(out ARRaycastHit hit, out ARPlane plane)
        {
            hit = default;
            plane = null;
            aimingAtFloor = false;
            aimingAtFurniture = false;
            wallSettling = false;
            if (raycastManager == null)
                return false;

            CollectPlanes();
            ARPlane wall = ChooseOpenWall(out Vector3 point, out float distance, out bool sawFloor, out bool sawFurniture);
            if (wall == null)
            {
                pendingPlane = TrackableId.invalidId;
                aimingAtFurniture = sawFurniture;
                aimingAtFloor = sawFloor && !sawFurniture;
                return false;
            }

            if (pendingPlane != wall.trackableId)
            {
                pendingPlane = wall.trackableId;
                pendingSince = Time.time;
                wallSettling = true;
                return false;
            }

            if (Time.time - pendingSince < WallSettleSeconds)
            {
                wallSettling = true;
                return false;
            }

            hit = HitOnPlane(wall, point, distance);
            plane = wall;
            return true;
        }

        void CollectPlanes()
        {
            PlaneChoices.Clear();
            if (planeManager == null)
                planeManager = FindAnyObjectByType<ARPlaneManager>();
            if (planeManager != null)
            {
                foreach (ARPlane tracked in planeManager.trackables)
                    RememberPlane(tracked);
            }

            TrackableType surfaces = TrackableType.PlaneWithinPolygon
                | TrackableType.PlaneWithinBounds
                | TrackableType.PlaneEstimated
                | TrackableType.PlaneWithinInfinity;
            for (int i = 0; i < AimSpots.Length; i++)
            {
                Vector2 spot = new Vector2(Screen.width * AimSpots[i].x, Screen.height * AimSpots[i].y);
                if (!raycastManager.Raycast(spot, Hits, surfaces))
                    continue;

                foreach (ARRaycastHit candidate in Hits)
                    RememberPlane(candidate.trackable as ARPlane);
            }
        }

        static void RememberPlane(ARPlane candidate)
        {
            if (candidate == null || candidate.subsumedBy != null)
                return;
            if (candidate.trackingState != TrackingState.Tracking)
                return;
            if (candidate.alignment != PlaneAlignment.Vertical && candidate.alignment != PlaneAlignment.HorizontalUp)
                return;

            for (int i = 0; i < PlaneChoices.Count; i++)
            {
                if (PlaneChoices[i] == candidate)
                    return;
            }

            PlaneChoices.Add(candidate);
        }

        ARPlane ChooseOpenWall(out Vector3 point, out float distance, out bool sawFloor, out bool sawFurniture)
        {
            point = default;
            distance = 0f;
            sawFloor = false;
            sawFurniture = false;
            if (arCamera == null)
                return null;

            Ray center = arCamera.ScreenPointToRay(new Vector2(Screen.width * 0.5f, Screen.height * 0.55f));
            bool labelledWall = false;
            for (int i = 0; i < PlaneChoices.Count; i++)
            {
                if (PlaneChoices[i].alignment == PlaneAlignment.Vertical && IsWall(PlaneChoices[i]))
                    labelledWall = true;
            }

            ARPlane best = null;
            float bestScore = float.NegativeInfinity;
            for (int i = 0; i < PlaneChoices.Count; i++)
            {
                ARPlane candidate = PlaneChoices[i];
                if (!candidate.infinitePlane.Raycast(center, out float enter))
                    continue;
                if (enter < MinWallDistance || enter > MaxWallDistance)
                    continue;

                if (candidate.alignment == PlaneAlignment.HorizontalUp)
                {
                    if (Vector3.Dot(center.direction, Vector3.up) < -0.45f)
                        sawFloor = true;
                    continue;
                }

                if (IsFurniture(candidate))
                {
                    float furnitureOffset = Vector3.ProjectOnPlane(center.GetPoint(enter) - candidate.center, candidate.normal).magnitude;
                    if (furnitureOffset < 0.75f)
                        sawFurniture = true;
                    continue;
                }

                if (labelledWall && !IsWall(candidate))
                    continue;
                if (Mathf.Abs(Vector3.Dot(arCamera.transform.forward, candidate.normal)) < 0.2f)
                    continue;

                Vector3 onPlane = center.GetPoint(enter);
                float score = Mathf.Clamp(candidate.size.x * candidate.size.y, 0f, 4f);
                score += candidate.size.y;
                if (IsWall(candidate))
                    score += 10f;
                if (Mathf.Abs(candidate.center.y - arCamera.transform.position.y) < 0.85f)
                    score += 4f;
                float offset = Vector3.ProjectOnPlane(onPlane - candidate.center, candidate.normal).magnitude;
                if (offset < 1.2f)
                    score += 3f;
                else if (offset > 3f)
                    score -= 2f;
                score -= enter * 0.35f;
                if (score <= bestScore)
                    continue;

                bestScore = score;
                best = candidate;
                point = onPlane;
                distance = enter;
            }

            return best;
        }

        static bool IsWall(ARPlane candidate)
        {
            return (candidate.classifications & WallKinds) != 0;
        }

        static bool IsFurniture(ARPlane candidate)
        {
            if (IsWall(candidate))
                return false;
            return (candidate.classifications & FurnitureKinds) != 0;
        }

        static ARRaycastHit HitOnPlane(ARPlane wall, Vector3 worldPoint, float distance)
        {
            Transform space = wall.transform.parent != null ? wall.transform.parent : wall.transform;
            XRRaycastHit nativeHit = new XRRaycastHit(
                wall.trackableId,
                new Pose(space.InverseTransformPoint(worldPoint), Quaternion.Inverse(space.rotation) * wall.transform.rotation),
                distance,
                TrackableType.PlaneWithinInfinity);
            return new ARRaycastHit(nativeHit, distance, space, wall);
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
            if (wallHeld)
                ShowPieceOnPhoto(artwork);
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

        Texture2D ImageFor(Artwork artwork)
        {
            if (artwork != null && artwork.Id == BuyerPieceId && buyerTexture != null)
                return buyerTexture;
            Texture2D picture = PictureFor(artwork);
            if (picture != null)
                return picture;
            return SampleTexture(FillFor(artwork));
        }

        static Texture2D PictureFor(Artwork artwork)
        {
            if (artwork == null || string.IsNullOrEmpty(artwork.ImageResource))
                return null;
            return Resources.Load<Texture2D>(artwork.ImageResource);
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
            tagsPanel = CreatePanel(column.transform, "Tags");
            Cover(tagsPanel, new Color(0.86f, 0.93f, 1f, 1f));
            quizPrompt = CreateLabel(tagsPanel.transform, uiFont, TagsMessage, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -340f), 52);
            quizPrompt.color = new Color(0.07f, 0.18f, 0.42f, 1f);
            quizPrompt.rectTransform.sizeDelta = new Vector2(960f, 220f);
            for (int i = 0; i < quizButtons.Length; i++)
            {
                Button button = CreateButton(
                    tagsPanel.transform,
                    uiFont,
                    "Answer",
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f),
                    new Vector2(24f, 80f),
                    new Vector2(1032f, 140f),
                    () => { },
                    36);
                quizButtons[i] = button;
                quizImages[i] = button.GetComponent<Image>();
                quizLabels[i] = button.GetComponentInChildren<Text>();
            }

            ownPiecePanel = CreatePanel(tagsPanel.transform, "Own piece");
            Cover(ownPiecePanel, new Color(0.86f, 0.93f, 1f, 1f));
            pictureLabel = CreateButton(ownPiecePanel.transform, uiFont, "Upload a clear picture", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 1300f), new Vector2(1032f, 110f), UploadPainting, 32).GetComponentInChildren<Text>();
            widthValue = CreateMeasureBox(ownPiecePanel.transform, "Width cm", new Vector2(24f, 1140f), out widthBox);
            heightValue = CreateMeasureBox(ownPiecePanel.transform, "Height cm", new Vector2(24f, 990f), out heightBox);
            WireMeasure(widthValue, widthBox);
            WireMeasure(heightValue, heightBox);
            CreateButton(ownPiecePanel.transform, uiFont, "No frame", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 860f), new Vector2(500f, 100f), ChooseBarePiece, 32);
            CreateButton(ownPiecePanel.transform, uiFont, "Add a frame", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(556f, 860f), new Vector2(500f, 100f), ChooseFramedPiece, 32);
            frameDetails = CreatePanel(ownPiecePanel.transform, "Frame details");
            mouldingValue = CreateMeasureBox(frameDetails.transform, "Frame width cm", new Vector2(24f, 720f), out mouldingBox);
            WireMeasure(mouldingValue, mouldingBox);
            CreateButton(frameDetails.transform, uiFont, "Black", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 610f), new Vector2(240f, 90f), ChooseBlackFrame, 28);
            CreateButton(frameDetails.transform, uiFont, "White", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(280f, 610f), new Vector2(240f, 90f), ChooseWhiteFrame, 28);
            CreateButton(frameDetails.transform, uiFont, "Oak", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(536f, 610f), new Vector2(240f, 90f), ChooseOakFrame, 28);
            CreateButton(frameDetails.transform, uiFont, "Gold", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(792f, 610f), new Vector2(240f, 90f), ChooseGoldFrame, 28);
            frameDetails.SetActive(false);
            CreateButton(ownPiecePanel.transform, uiFont, "Try this piece", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 480f), new Vector2(1032f, 110f), TryOwnPiece, 36);
            AddMeasureKey("1", 0, 3, () => AppendMeasure("1"));
            AddMeasureKey("2", 1, 3, () => AppendMeasure("2"));
            AddMeasureKey("3", 2, 3, () => AppendMeasure("3"));
            AddMeasureKey("4", 0, 2, () => AppendMeasure("4"));
            AddMeasureKey("5", 1, 2, () => AppendMeasure("5"));
            AddMeasureKey("6", 2, 2, () => AppendMeasure("6"));
            AddMeasureKey("7", 0, 1, () => AppendMeasure("7"));
            AddMeasureKey("8", 1, 1, () => AppendMeasure("8"));
            AddMeasureKey("9", 2, 1, () => AppendMeasure("9"));
            AddMeasureKey(".", 0, 0, () => AppendMeasure("."));
            AddMeasureKey("0", 1, 0, () => AppendMeasure("0"));
            AddMeasureKey("Delete", 2, 0, BackspaceMeasure);
            ownPiecePanel.SetActive(false);
            ShowQuestion();
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
            Cover(worksPanel, new Color(0.86f, 0.93f, 1f, 1f));
            Cover(recommendPanel, new Color(0.94f, 0.96f, 1f, 1f));
            GameObject workViewport = CreatePanel(worksPanel.transform, "Work scroll");
            RectTransform viewRect = workViewport.GetComponent<RectTransform>();
            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.pivot = new Vector2(0.5f, 0.5f);
            viewRect.offsetMin = new Vector2(28f, 260f);
            viewRect.offsetMax = new Vector2(-28f, -160f);
            workViewRect = viewRect;
            Image viewImage = workViewport.AddComponent<Image>();
            viewImage.color = new Color(1f, 1f, 1f, 0.01f);
            workViewport.AddComponent<RectMask2D>();
            workScroll = workViewport.AddComponent<ScrollRect>();
            workScroll.horizontal = false;
            workScroll.vertical = true;
            workScroll.movementType = ScrollRect.MovementType.Clamped;
            workScroll.scrollSensitivity = 40f;
            workScroll.viewport = viewRect;
            workList = CreatePanel(workViewport.transform, "Work list").GetComponent<RectTransform>();
            workScroll.content = workList;
            catalogueHeading = CreateLabel(worksPanel.transform, uiFont, "Top picks", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), 48);
            catalogueHeading.color = PageInk;
            catalogueHeading.rectTransform.sizeDelta = new Vector2(980f, 64f);
            catalogueSubtitle = CreateLabel(worksPanel.transform, uiFont, string.Empty, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -82f), 26);
            catalogueSubtitle.color = new Color(0.35f, 0.48f, 0.64f, 1f);
            catalogueSubtitle.rectTransform.sizeDelta = new Vector2(980f, 44f);

            browseButton = CreateButton(worksPanel.transform, uiFont, "Browse for more", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 148f), new Vector2(1032f, 84f), ShowAll, 28).gameObject;
            topPicksButton = CreateButton(worksPanel.transform, uiFont, "Top picks", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 148f), new Vector2(1032f, 84f), ShowTopPicks, 28).gameObject;
            PaintQuiet(browseButton);
            PaintQuiet(topPicksButton);
            browseFilters = CreatePanel(worksPanel.transform, "Browse filters");
            PaintQuiet(CreateButton(browseFilters.transform, uiFont, "All", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 248f), new Vector2(246f, 80f), ShowAll, 26).gameObject);
            PaintQuiet(CreateButton(browseFilters.transform, uiFont, "Blue", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(286f, 248f), new Vector2(246f, 80f), ShowBlue, 26).gameObject);
            PaintQuiet(CreateButton(browseFilters.transform, uiFont, "Calm", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(548f, 248f), new Vector2(246f, 80f), ShowCalm, 26).gameObject);
            PaintQuiet(CreateButton(browseFilters.transform, uiFont, "Fits 50", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(810f, 248f), new Vector2(246f, 80f), ShowFitsFifty, 26).gameObject);
            PaintPrimary(CreateButton(worksPanel.transform, uiFont, "Try this piece", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 24f), new Vector2(1032f, 108f), BeginPlacement, 34).gameObject);
            RefreshWorkList();

            notesButton = CreateButton(wallPanel.transform, uiFont, "Notes", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 460f), new Vector2(500f, 100f), ShowNotes, 36).gameObject;
            skipButton = CreateButton(wallPanel.transform, uiFont, "Skip", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 28f), new Vector2(500f, 110f), SkipQuestion, 32).gameObject;
            concernField = CreateInput(wallPanel.transform, uiFont);
            sendButton = CreateButton(wallPanel.transform, uiFont, "Send", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(556f, 28f), new Vector2(500f, 110f), Send, 32).gameObject;
            PaintQuiet(skipButton);
            PaintPrimary(sendButton);
            placeButton = CreateButton(wallPanel.transform, uiFont, "Place", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 40f), new Vector2(320f, 140f), Place);
            swapButton = CreateButton(wallPanel.transform, uiFont, "Swap", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(320f, 140f), Swap).gameObject;
            removeButton = CreateButton(wallPanel.transform, uiFont, "Remove", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 40f), new Vector2(320f, 140f), Remove).gameObject;

            Text recommendTitle = CreateLabel(recommendPanel.transform, uiFont, "What do you want to do?", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), 42);
            recommendTitle.color = PageInk;
            recommendTitle.rectTransform.sizeDelta = new Vector2(980f, 80f);
            CreateButton(recommendPanel.transform, uiFont, "Keep", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 320f), new Vector2(320f, 120f), Keep);
            CreateButton(recommendPanel.transform, uiFont, "Save", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 320f), new Vector2(320f, 120f), Save);
            CreateButton(recommendPanel.transform, uiFont, "Dismiss", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 320f), new Vector2(320f, 120f), Dismiss);
            CreateButton(recommendPanel.transform, uiFont, "Notes 1", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 580f), new Vector2(328f, 80f), ShowFirstNotes, 28);
            CreateButton(recommendPanel.transform, uiFont, "Notes 2", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(376f, 580f), new Vector2(328f, 80f), ShowSecondNotes, 28);
            CreateButton(recommendPanel.transform, uiFont, "Notes 3", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(728f, 580f), new Vector2(328f, 80f), ShowThirdNotes, 28);
            CreateButton(recommendPanel.transform, uiFont, "Alternatives", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 460f), new Vector2(1032f, 100f), AskForAlternatives, 32);
            CreateButton(recommendPanel.transform, uiFont, "Use 1", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 180f), new Vector2(320f, 120f), ChooseFirst);
            CreateButton(recommendPanel.transform, uiFont, "Use 2", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 180f), new Vector2(320f, 120f), ChooseSecond);
            CreateButton(recommendPanel.transform, uiFont, "Use 3", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 180f), new Vector2(320f, 120f), ChooseThird);
            CreateButton(recommendPanel.transform, uiFont, "Change tags", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 40f), new Vector2(500f, 120f), ChangeTags, 32);
            CreateButton(recommendPanel.transform, uiFont, "End", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 40f), new Vector2(500f, 120f), EndSession, 32);
            if (statusShade != null)
                statusShade.SetAsLastSibling();
            if (status != null)
                status.rectTransform.SetAsLastSibling();
            if (stepLine != null)
                stepLine.rectTransform.SetAsLastSibling();
            if (aimMark != null)
                aimMark.rectTransform.SetAsLastSibling();
            ApplyStepVisibility();

            if (SkipOpeningForPreview)
            {
                StartPreview();
                canvasObject.SetActive(true);
            }
            else
            {
                canvasObject.SetActive(false);
                OpeningSequence.Play(uiFont, (id, code) =>
                {
                    BuyerSession started;
                    if (BuyerSession.TryStart(code, out started))
                        session = started;
                    ChooseOpeningWork(id);
                    if (canvasObject != null)
                        canvasObject.SetActive(true);
                    StartBuyerPath();
                });
            }
        }

        static void Cover(GameObject panel, Color color)
        {
            if (panel == null)
                return;

            GameObject shade = new GameObject("Cover", typeof(RectTransform));
            shade.transform.SetParent(panel.transform, false);
            shade.transform.SetAsFirstSibling();
            Image image = shade.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            RectTransform rect = shade.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
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

        Text CreateMeasureBox(Transform parent, string placeholder, Vector2 position, out Image box)
        {
            GameObject fieldObject = new GameObject(placeholder);
            fieldObject.transform.SetParent(parent, false);
            Image image = fieldObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.96f);
            Button button = fieldObject.AddComponent<Button>();
            button.targetGraphic = image;
            RectTransform rect = fieldObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.sizeDelta = new Vector2(1032f, 120f);
            rect.anchoredPosition = position;

            Text value = CreateFieldText(fieldObject.transform, uiFont, string.Empty, new Color(0.07f, 0.18f, 0.42f, 1f), FontStyle.Bold, 48);
            value.alignment = TextAnchor.MiddleLeft;
            value.raycastTarget = false;
            Text hint = CreateFieldText(fieldObject.transform, uiFont, placeholder, new Color(0.35f, 0.42f, 0.52f, 0.9f), FontStyle.Italic, 36);
            hint.alignment = TextAnchor.MiddleLeft;
            hint.raycastTarget = false;
            box = image;
            return value;
        }

        void WireMeasure(Text value, Image box)
        {
            Button button = value.transform.parent.GetComponent<Button>();
            button.onClick.AddListener(() => SelectMeasure(value, box));
        }

        void AddMeasureKey(string label, int column, int rowFromBottom, UnityEngine.Events.UnityAction action)
        {
            CreateButton(
                ownPiecePanel.transform,
                uiFont,
                label,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(24f + (column * 344f), 24f + (rowFromBottom * 112f)),
                new Vector2(328f, 100f),
                action,
                36);
        }

        void SelectMeasure(Text value, Image box)
        {
            selectedValue = value;
            PaintMeasure(widthBox, box);
            PaintMeasure(heightBox, box);
            PaintMeasure(mouldingBox, box);
        }

        static void PaintMeasure(Image box, Image selected)
        {
            if (box == null)
                return;

            box.color = box == selected
                ? new Color(0.75f, 0.86f, 1f, 1f)
                : new Color(1f, 1f, 1f, 0.96f);
        }

        void AppendMeasure(string digit)
        {
            if (selectedValue == null)
                SelectMeasure(heightValue, heightBox);
            if (selectedValue == null)
                return;

            string current = selectedValue.text;
            if (digit == "." && current.IndexOf('.') >= 0)
                return;
            if (current.Length >= 6)
                return;

            selectedValue.text = current + digit;
            ShowMeasureHint(selectedValue, false);
        }

        void BackspaceMeasure()
        {
            if (selectedValue == null || selectedValue.text.Length == 0)
                return;

            selectedValue.text = selectedValue.text.Substring(0, selectedValue.text.Length - 1);
            ShowMeasureHint(selectedValue, selectedValue.text.Length == 0);
        }

        static void ShowMeasureHint(Text value, bool showHint)
        {
            Transform hint = value.transform.parent.Find("Placeholder");
            if (hint != null)
                hint.gameObject.SetActive(showHint);
        }

        void TypeIntoSelectedMeasure()
        {
            if (!enteringOwnPiece)
                return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.backspaceKey.wasPressedThisFrame)
                BackspaceMeasure();
            if (keyboard.periodKey.wasPressedThisFrame || keyboard.numpadPeriodKey.wasPressedThisFrame || keyboard.commaKey.wasPressedThisFrame)
                AppendMeasure(".");

            Key[] numberKeys =
            {
                Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4,
                Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9
            };
            Key[] padKeys =
            {
                Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4,
                Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9
            };
            for (int digit = 0; digit < numberKeys.Length; digit++)
            {
                if (keyboard[numberKeys[digit]].wasPressedThisFrame || keyboard[padKeys[digit]].wasPressedThisFrame)
                    AppendMeasure(digit.ToString());
            }
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
