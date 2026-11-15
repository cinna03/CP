using System.Collections;
using System.Collections.Generic;
using Capstone.App.AR;
using Capstone.App.Data;
using Capstone.App.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Capstone.App.UI
{
    /// <summary>
    /// Binds the wall questions, the three decisions, and the recommendation tray.
    /// The written lines and the ranking stay in the query service and the recommendation engine.
    /// </summary>
    public sealed class WallDecisionHUDController : MonoBehaviour
    {
        static readonly Color Card = new Color32(255, 255, 255, 255);
        static readonly Color Accent = new Color32(233, 168, 188, 255);
        static readonly Color Blush = new Color32(249, 235, 240, 255);
        static readonly Color Ink = new Color32(42, 34, 37, 255);
        static readonly Color Stone = new Color32(125, 110, 115, 255);

        readonly ArtworkQueryService replies = new ArtworkQueryService();
        readonly RecommendationEngine ranking = new RecommendationEngine();
        readonly IWallSnapshotService photos = new WallSnapshotService();
        readonly IDecisionStudyLogger study = new DecisionStudyLogger();
        public event System.Action VisitFinished;
        public event System.Action VisitCancelled;

        ARArtworkPresenter wall;
        ArtworkSessionState session;
        IReadOnlyList<ArtworkData> catalogue;
        IReadOnlyList<FrameOption> frames;
        RectTransform history;
        RectTransform historyContent;
        GameObject historyPill;
        ScrollRect historyScroll;
        RectTransform tray;
        RectTransform trayRow;
        TMP_Text trayHeading;
        GameObject suggestionPill;
        TMP_Text keptLabel;
        GameObject compareButton;
        RectTransform compareList;
        RectTransform inspect;
        TMP_Text inspectBody;
        RectTransform inspectSizeRow;
        RectTransform inspectFrameRow;
        ArtworkData inspectWork;
        int inspectSize;
        FrameOption inspectFrame;
        RectTransform compareCard;
        RectTransform checkIn;
        string checkDriver = "";
        TMP_Text compareBody;
        int keptCursor;
        TMP_InputField askField;
        Sprite rounded;
        Coroutine toast;
        Coroutine scrollRoutine;
        Coroutine capture;
        Coroutine exitRoutine;
        bool closing;

        public void Open(ARArtworkPresenter presenter, ArtworkSessionState visit, IReadOnlyList<ArtworkData> works, IReadOnlyList<FrameOption> frameOptions)
        {
            wall = presenter;
            session = visit;
            catalogue = works;
            frames = frameOptions;
            if (session != null && session.SelectedArtwork != null)
                session.NoteTried(session.SelectedArtwork, session.SizeIndex, session.SelectedFrame, "Viewing");
            if (session != null)
                session.Changed += RefreshSelectors;
            Build();
            RefreshSelectors();
        }

        void OnDestroy()
        {
            if (session != null)
                session.Changed -= RefreshSelectors;
            ranking.Clear();
        }

        void AskFit()
        {
            Ask("Fit & Size");
        }

        void AskLight()
        {
            Ask("Lighting & Colour");
        }

        void AskStory()
        {
            Ask("Story & Craft");
        }

        void AskPlace()
        {
            Ask("Where to Hang");
        }

        void Ask(string question)
        {
            ArtworkData work = Current();
            if (work == null)
            {
                Reply(question, ArtworkQueryService.FixedLine, null);
                return;
            }

            ConsultReply result = replies.Consult(work, SizeIndex(), session != null ? session.SelectedFrame : null, catalogue, session, question);
            Reply(question, result.Text, result.Options, result.Actions);
        }

        void Keep()
        {
            ArtworkData work = Current();
            if (work != null && session != null)
            {
                session.RememberKept(work, session.SizeIndex, session.SelectedFrame);
                session.Taste.NoteDecision(work.Style, work.Mood, work.Colour, 2f);
                session.NoteTried(work, session.SizeIndex, session.SelectedFrame, "Kept");
            }
            RefreshSelectors();
            ShowToast("Added to Kept Works");
            Decide(VisitDecision.Keep);
        }

        void Save()
        {
            ArtworkData work = Current();
            if (work == null)
                return;

            if (session != null)
            {
                session.SetShortlisted(work, true);
                session.Taste.NoteDecision(work.Style, work.Mood, work.Colour, 1f);
                session.NoteTried(work, session.SizeIndex, session.SelectedFrame, "Saved");
            }
            Decide(VisitDecision.Save);
            ShowToast("Saved to Favorites");
        }

        void DismissWork()
        {
            ArtworkData work = Current();
            if (work != null && session != null)
            {
                session.NoteDismissed(work);
                session.Taste.NoteDecision(work.Style, work.Mood, work.Colour, -1f);
                session.NoteTried(work, session.SizeIndex, session.SelectedFrame, "Dismissed");
            }
            Decide(VisitDecision.Dismiss);
        }

        void Decide(VisitDecision decision)
        {
            ArtworkData work = Current();
            if (work == null || catalogue == null)
                return;

            if (session != null)
                session.RecordDecision(decision);
            ranking.Apply(work, decision);
            if (trayHeading != null)
                trayHeading.text = decision == VisitDecision.Keep ? "Similar Works You Might Like (3)" : ranking.Heading(decision);
            IReadOnlyList<string> dismissed = session != null ? session.DismissedIdentifiers : null;
            ShowRecommendations(ranking.Three(catalogue, work, decision, dismissed, session != null ? session.Taste : null));
        }

        void Back()
        {
            if (closing)
                return;
            if (VisitCancelled != null)
                VisitCancelled();
        }

        void FinishVisit()
        {
            if (closing)
                return;
            if (checkIn != null)
                Destroy(checkIn.gameObject);

            checkDriver = "";
            int tried = session != null && session.Tried != null ? session.Tried.Count : 0;
            int kept = session != null && session.Kept != null ? session.Kept.Count : 0;
            int questions = session != null && session.Answers != null ? session.Answers.Count : 0;
            string room = "Unspecified";
            if (session != null && session.Taste != null && session.Taste.TargetRoom.Length > 0)
                room = session.Taste.TargetRoom;

            string pieceTitle = "No work selected";
            string dimensions = "";
            string frame = "Unframed";
            if (session != null && session.Kept != null && session.Kept.Count > 0 && session.Kept[session.Kept.Count - 1] != null && session.Kept[session.Kept.Count - 1].Work != null)
            {
                KeptChoice choice = session.Kept[session.Kept.Count - 1];
                pieceTitle = choice.Work.Title;
                dimensions = choice.Work.FormatSizeLabel(choice.SizeIndex);
                frame = choice.Frame != null && choice.Frame.FrameName.Length > 0 ? choice.Frame.FrameName : "Unframed";
            }
            else if (Current() != null)
            {
                ArtworkData work = Current();
                pieceTitle = work.Title;
                dimensions = work.FormatSizeLabel(SizeIndex());
                frame = session != null && session.SelectedFrame != null && session.SelectedFrame.FrameName.Length > 0 ? session.SelectedFrame.FrameName : "Unframed";
            }

            string pieceLine = dimensions.Length > 0 ? pieceTitle + ", " + dimensions + " · " + frame : pieceTitle;
            checkIn = CardAt("Finish", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(920f, 1180f), Vector2.zero);
            checkIn.pivot = new Vector2(0.5f, 0.5f);
            TMP_Text recap = Label(checkIn, "You tested " + tried + " works on your wall, kept " + kept + " finalists, and asked " + questions + " questions.\n\nTop piece: " + pieceLine, 26f, Ink, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            recap.rectTransform.anchorMin = new Vector2(0f, 1f);
            recap.rectTransform.anchorMax = new Vector2(1f, 1f);
            recap.rectTransform.pivot = new Vector2(0.5f, 1f);
            recap.rectTransform.sizeDelta = new Vector2(-56f, 160f);
            recap.rectTransform.anchoredPosition = new Vector2(0f, -24f);
            recap.enableWordWrapping = true;
            TMP_Text prompt = Label(checkIn, "How confident do you feel about choosing art for your space now?", 28f, Ink, FontStyles.Bold, TextAlignmentOptions.TopLeft);
            prompt.rectTransform.anchorMin = new Vector2(0f, 1f);
            prompt.rectTransform.anchorMax = new Vector2(1f, 1f);
            prompt.rectTransform.pivot = new Vector2(0.5f, 1f);
            prompt.rectTransform.sizeDelta = new Vector2(-56f, 90f);
            prompt.rectTransform.anchoredPosition = new Vector2(0f, -200f);
            prompt.enableWordWrapping = true;

            Pill(checkIn, "1 · Unsure", new Vector2(0.5f, 1f), new Vector2(820f, 56f), new Vector2(0f, -310f), Accent, delegate { CloseVisit(tried, kept, pieceTitle, dimensions, frame, questions, room, 1); });
            Pill(checkIn, "2", new Vector2(0.5f, 1f), new Vector2(820f, 56f), new Vector2(0f, -376f), Accent, delegate { CloseVisit(tried, kept, pieceTitle, dimensions, frame, questions, room, 2); });
            Pill(checkIn, "3", new Vector2(0.5f, 1f), new Vector2(820f, 56f), new Vector2(0f, -442f), Accent, delegate { CloseVisit(tried, kept, pieceTitle, dimensions, frame, questions, room, 3); });
            Pill(checkIn, "4", new Vector2(0.5f, 1f), new Vector2(820f, 56f), new Vector2(0f, -508f), Accent, delegate { CloseVisit(tried, kept, pieceTitle, dimensions, frame, questions, room, 4); });
            Pill(checkIn, "5 · Very Confident", new Vector2(0.5f, 1f), new Vector2(820f, 56f), new Vector2(0f, -574f), Accent, delegate { CloseVisit(tried, kept, pieceTitle, dimensions, frame, questions, room, 5); });
            Pill(checkIn, "Seeing real size helped", new Vector2(0.5f, 1f), new Vector2(820f, 52f), new Vector2(0f, -660f), Blush, delegate { checkDriver = "Seeing real size helped"; });
            Pill(checkIn, "Advisor tips helped", new Vector2(0.5f, 1f), new Vector2(820f, 52f), new Vector2(0f, -722f), Blush, delegate { checkDriver = "Advisor tips helped"; });
            Pill(checkIn, "Easy to compare", new Vector2(0.5f, 1f), new Vector2(820f, 52f), new Vector2(0f, -784f), Blush, delegate { checkDriver = "Easy to compare"; });
            Pill(checkIn, "Skip & Exit", new Vector2(0.5f, 1f), new Vector2(280f, 52f), new Vector2(0f, -870f), Blush, delegate { CloseVisit(tried, kept, pieceTitle, dimensions, frame, questions, room, 0); });
            checkIn.SetAsLastSibling();
        }

        void CloseVisit(int tried, int kept, string title, string dimensions, string frame, int questions, string room, int score)
        {
            if (closing)
                return;
            closing = true;
            string driver = score == 0 ? "Skipped" : checkDriver;
            if (string.IsNullOrEmpty(driver))
                driver = "Unspecified";

            study.LogConfidenceDecision(new DecisionConfidenceEntry
            {
                SessionId = study.SessionId,
                TotalWorksTried = tried,
                KeptWorksCount = kept,
                FinalSelectedArtworkTitle = title ?? "",
                FinalChosenDimensionsCm = dimensions ?? "",
                FinalChosenFrame = string.IsNullOrEmpty(frame) ? "Unframed" : frame,
                QuestionsAskedInSession = questions,
                LearnedRoomContext = string.IsNullOrEmpty(room) ? "Unspecified" : room,
                ConfidenceScore = score,
                PrimaryConfidenceDriver = driver
            });

            if (session != null)
                session.ClearVisit();
            if (checkIn != null)
                Destroy(checkIn.gameObject);
            checkIn = null;
            ShowToast("Session saved — thank you!");
            if (exitRoutine != null)
                StopCoroutine(exitRoutine);
            exitRoutine = StartCoroutine(ReturnToMenu());
        }

        IEnumerator ReturnToMenu()
        {
            yield return new WaitForSecondsRealtime(1.1f);
            if (VisitFinished != null)
                VisitFinished();
        }

        void ShowAnswer(string reply)
        {
            Reply("Note", reply, null);
        }

        void Reply(string question, string reply, string[] options)
        {
            Reply(question, reply, options, null);
        }

        void Reply(string question, string reply, string[] options, ConsultAction[] actions)
        {
            string title = Current() != null ? Current().Title : "";
            if (session != null)
                session.RememberAnswer(title, question, reply);
            AddHistory(title, question, reply, options, actions);
            ShowHistory();
            if (scrollRoutine != null)
                StopCoroutine(scrollRoutine);
            scrollRoutine = StartCoroutine(RevealLatest());
        }

        void AcceptOption(string option)
        {
            if (session != null && session.Taste != null)
                session.Taste.Accept(option);
            Ask(option);
        }

        void ApplyAction(ConsultAction action)
        {
            if (action == null || wall == null || session == null)
                return;

            if (action.Work != null)
            {
                int index = action.SizeIndex >= 0 ? action.SizeIndex : 0;
                wall.LoadArtworkIntoWall(action.Work, index, session.SelectedFrame);
                return;
            }

            if (action.SizeIndex >= 0 && session.SelectedArtwork != null)
                wall.LoadArtworkIntoWall(session.SelectedArtwork, action.SizeIndex, session.SelectedFrame);
        }

        void ShowRecommendations(IReadOnlyList<ArtworkData> works)
        {
            if (trayRow == null)
                return;

            for (int i = trayRow.childCount - 1; i >= 0; i--)
                Destroy(trayRow.GetChild(i).gameObject);

            if (works != null)
            {
                for (int i = 0; i < works.Count; i++)
                {
                    if (works[i] != null)
                        AddRecommendation(works[i]);
                }
            }

            if (suggestionPill != null)
                suggestionPill.SetActive(false);
            if (tray != null)
                tray.gameObject.SetActive(true);
        }

        void ShowSuggestions()
        {
            if (trayRow == null || trayRow.childCount == 0)
                return;
            if (suggestionPill != null)
                suggestionPill.SetActive(false);
            if (tray != null)
                tray.gameObject.SetActive(true);
        }

        void HideSuggestions()
        {
            if (tray != null)
                tray.gameObject.SetActive(false);
            if (suggestionPill != null)
                suggestionPill.SetActive(trayRow != null && trayRow.childCount > 0);
        }

        void ChooseRecommendation(ArtworkData work)
        {
            Place(work, 0, session != null ? session.SelectedFrame : null);
        }

        void Place(ArtworkData work, int sizeIndex, FrameOption frame)
        {
            HideHistory();
            HideInspect();
            HideSuggestions();
            if (wall != null)
                wall.LoadArtworkIntoWall(work, sizeIndex, frame);
            RefreshSelectors();
        }

        ArtworkData Current()
        {
            return session != null ? session.SelectedArtwork : null;
        }

        int SizeIndex()
        {
            return session != null ? session.SizeIndex : 0;
        }

        readonly List<Canvas> photoCanvases = new List<Canvas>();

        public void RememberForPhoto(Canvas canvas)
        {
            if (canvas != null && !photoCanvases.Contains(canvas))
                photoCanvases.Add(canvas);
        }

        static void HideForPhoto(Canvas canvas, List<Canvas> hidden)
        {
            if (canvas == null || !canvas.enabled)
                return;
            canvas.enabled = false;
            hidden.Add(canvas);
        }

        void TakePhoto()
        {
            if (capture != null)
                return;
            capture = StartCoroutine(CaptureWall());
        }

        IEnumerator CaptureWall()
        {
            ArtworkData work = Current();
            var hidden = new List<Canvas>();
            HideForPhoto(GetComponent<Canvas>(), hidden);
            for (int i = 0; i < photoCanvases.Count; i++)
                HideForPhoto(photoCanvases[i], hidden);

            yield return null;
            yield return new WaitForEndOfFrame();
            Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();
            for (int i = 0; i < hidden.Count; i++)
            {
                if (hidden[i] != null)
                    hidden[i].enabled = true;
            }

            if (work == null)
            {
                if (shot != null)
                    Destroy(shot);
                ShowToast("Choose a work before taking a photo.");
            }
            else
            {
                string frame = session != null && session.SelectedFrame != null ? session.SelectedFrame.FrameName : "Unframed";
                photos.SaveSnapshot(shot, work, SizeIndex(), frame);
                if (shot != null)
                    Destroy(shot);
                ShowToast("Saved photo to Gallery");
            }

            capture = null;
        }

        void ShowToast(string message)
        {
            if (toast != null)
                StopCoroutine(toast);
            toast = StartCoroutine(Toast(message));
        }

        IEnumerator Toast(string message)
        {
            var banner = new GameObject("Saved", typeof(RectTransform), typeof(Image));
            banner.transform.SetParent(transform, false);
            var rect = banner.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(520f, 72f);
            rect.anchoredPosition = new Vector2(0f, 1180f);
            Image image = banner.GetComponent<Image>();
            image.sprite = Rounded();
            image.type = Image.Type.Sliced;
            image.color = Accent;
            image.raycastTarget = false;
            Label(banner.transform, message, 26f, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
            yield return new WaitForSecondsRealtime(1.6f);
            if (banner != null)
                Destroy(banner);
        }

        void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 760;
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1
                | AdditionalCanvasShaderChannels.Normal
                | AdditionalCanvasShaderChannels.Tangent;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0f;
            gameObject.AddComponent<GraphicRaycaster>();
            gameObject.AddComponent<SafeAreaFitter>();

            Button kept = Pill(transform, "Compare Tried (0)", new Vector2(1f, 1f), new Vector2(420f, 72f), new Vector2(-250f, -164f), Accent, ToggleCompare);
            keptLabel = kept.GetComponentInChildren<TMP_Text>();
            compareButton = kept.gameObject;
            compareButton.SetActive(false);
            Pill(transform, "← Back", new Vector2(0f, 1f), new Vector2(220f, 72f), new Vector2(146f, -80f), Blush, Back);
            Pill(transform, "Take Photo", new Vector2(0.5f, 1f), new Vector2(280f, 72f), new Vector2(0f, -80f), Accent, TakePhoto);
            Pill(transform, "Finish Visit", new Vector2(1f, 1f), new Vector2(280f, 72f), new Vector2(-180f, -80f), Accent, FinishVisit);
            BuildHistory();

            tray = CardAt("Recommendations", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1000f, 460f), new Vector2(0f, 420f));
            trayHeading = Label(tray, "Similar Works You Might Like", 22f, Ink, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            trayHeading.rectTransform.anchorMin = new Vector2(0f, 1f);
            trayHeading.rectTransform.anchorMax = new Vector2(1f, 1f);
            trayHeading.rectTransform.pivot = new Vector2(0f, 1f);
            trayHeading.rectTransform.sizeDelta = new Vector2(-280f, 44f);
            trayHeading.rectTransform.anchoredPosition = new Vector2(24f, -8f);
            Pill(tray, "Hide", new Vector2(1f, 1f), new Vector2(140f, 48f), new Vector2(-24f, -36f), Blush, HideSuggestions);
            var row = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(tray, false);
            trayRow = row.GetComponent<RectTransform>();
            trayRow.anchorMin = new Vector2(0f, 0f);
            trayRow.anchorMax = new Vector2(1f, 1f);
            trayRow.offsetMin = new Vector2(12f, 12f);
            trayRow.offsetMax = new Vector2(-12f, -64f);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            tray.gameObject.SetActive(false);
            suggestionPill = Pill(transform, "Show Suggestions", new Vector2(0.5f, 0f), new Vector2(420f, 64f), new Vector2(0f, 250f), Accent, ShowSuggestions).gameObject;
            suggestionPill.SetActive(false);

            var decisions = new GameObject("Decisions", typeof(RectTransform));
            decisions.transform.SetParent(transform, false);
            var decisionRect = decisions.GetComponent<RectTransform>();
            decisionRect.anchorMin = decisionRect.anchorMax = new Vector2(0.5f, 0f);
            decisionRect.pivot = new Vector2(0.5f, 0f);
            decisionRect.sizeDelta = new Vector2(980f, 72f);
            decisionRect.anchoredPosition = new Vector2(0f, 320f);
            Pill(decisionRect, "Keep", new Vector2(0f, 0.5f), new Vector2(300f, 64f), new Vector2(158f, 0f), Accent, Keep);
            Pill(decisionRect, "Save", new Vector2(0.5f, 0.5f), new Vector2(300f, 64f), Vector2.zero, Blush, Save);
            Pill(decisionRect, "Dismiss", new Vector2(1f, 0.5f), new Vector2(300f, 64f), new Vector2(-158f, 0f), Card, DismissWork);

            BuildInspect();
            BuildCompare();
        }

        void AddRecommendation(ArtworkData work)
        {
            var card = new GameObject(work.Title, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            card.transform.SetParent(trayRow, false);
            Image image = card.GetComponent<Image>();
            image.sprite = Rounded();
            image.type = Image.Type.Sliced;
            image.color = Blush;
            card.GetComponent<LayoutElement>().preferredWidth = 300f;
            ArtworkData chosen = work;

            if (work.ArtworkSprite != null)
            {
                var picture = new GameObject("Picture", typeof(RectTransform), typeof(Image));
                picture.transform.SetParent(card.transform, false);
                var pictureRect = picture.GetComponent<RectTransform>();
                pictureRect.anchorMin = new Vector2(0f, 0.52f);
                pictureRect.anchorMax = new Vector2(1f, 1f);
                pictureRect.offsetMin = new Vector2(12f, 4f);
                pictureRect.offsetMax = new Vector2(-12f, -8f);
                Image sprite = picture.GetComponent<Image>();
                sprite.sprite = work.ArtworkSprite;
                sprite.preserveAspect = true;
                sprite.raycastTarget = false;
            }

            TMP_Text title = Label(card.transform, work.Title, 20f, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
            title.rectTransform.anchorMin = new Vector2(0f, 0.38f);
            title.rectTransform.anchorMax = new Vector2(1f, 0.52f);
            title.rectTransform.offsetMin = title.rectTransform.offsetMax = Vector2.zero;
            TMP_Text size = Label(card.transform, work.FormatSizeLabel(0), 16f, Stone, FontStyles.Normal, TextAlignmentOptions.Center);
            size.rectTransform.anchorMin = new Vector2(0f, 0.26f);
            size.rectTransform.anchorMax = new Vector2(1f, 0.38f);
            size.rectTransform.offsetMin = size.rectTransform.offsetMax = Vector2.zero;

            Pill(card.transform, "Details", new Vector2(0.27f, 0.12f), new Vector2(130f, 48f), Vector2.zero, Card, delegate { OpenDetails(chosen); });
            Pill(card.transform, "Try on Wall", new Vector2(0.73f, 0.12f), new Vector2(150f, 48f), Vector2.zero, Accent, delegate { ChooseRecommendation(chosen); });
        }

        RectTransform Row(string name, float y)
        {
            var host = new GameObject(name, typeof(RectTransform));
            host.transform.SetParent(transform, false);
            var rect = host.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(1000f, 60f);
            rect.anchoredPosition = new Vector2(0f, y);
            return rect;
        }

        void BuildAsk()
        {
            var bar = new GameObject("Ask", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(history != null ? history : transform, false);
            var rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(940f, 56f);
            rect.anchoredPosition = new Vector2(0f, 12f);
            Image image = bar.GetComponent<Image>();
            image.sprite = Rounded();
            image.type = Image.Type.Sliced;
            image.color = Card;

            var fieldObject = new GameObject("Input", typeof(RectTransform), typeof(TMP_InputField));
            fieldObject.transform.SetParent(bar.transform, false);
            var fieldRect = fieldObject.GetComponent<RectTransform>();
            fieldRect.anchorMin = Vector2.zero;
            fieldRect.anchorMax = Vector2.one;
            fieldRect.offsetMin = new Vector2(20f, 8f);
            fieldRect.offsetMax = new Vector2(-150f, -8f);
            var textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(fieldObject.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            TMP_Text value = textObject.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
                value.font = TMP_Settings.defaultFontAsset;
            value.fontSize = 22f;
            value.color = Ink;
            value.alignment = TextAlignmentOptions.MidlineLeft;
            var placeholderObject = new GameObject("Placeholder", typeof(RectTransform));
            placeholderObject.transform.SetParent(fieldObject.transform, false);
            var placeholderRect = placeholderObject.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = placeholderRect.offsetMax = Vector2.zero;
            TMP_Text placeholder = placeholderObject.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
                placeholder.font = TMP_Settings.defaultFontAsset;
            placeholder.text = "Ask a question about this work...";
            placeholder.fontSize = 20f;
            placeholder.fontStyle = FontStyles.Italic;
            placeholder.color = Stone;
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            askField = fieldObject.GetComponent<TMP_InputField>();
            askField.textViewport = fieldRect;
            askField.textComponent = value;
            askField.placeholder = placeholder;
            askField.fontAsset = value.font;
            askField.pointSize = 22f;
            askField.lineType = TMP_InputField.LineType.SingleLine;
            askField.onSubmit.AddListener(AskTyped);
            Pill(bar.transform, "Ask", new Vector2(1f, 0.5f), new Vector2(120f, 48f), new Vector2(-16f, 0f), Accent, SubmitAsk);
        }

        void SubmitAsk()
        {
            AskTyped(askField != null ? askField.text : "");
        }

        void AskTyped(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
                return;
            Ask(question);
        }

        void BuildHistory()
        {
            history = CardAt("Questions", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(960f, 860f), Vector2.zero);
            history.pivot = new Vector2(0.5f, 0.5f);
            TMP_Text title = Label(history, "Questions", 24f, Ink, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0f, 1f);
            title.rectTransform.sizeDelta = new Vector2(-220f, 44f);
            title.rectTransform.anchoredPosition = new Vector2(24f, -8f);
            Pill(history, "Hide", new Vector2(1f, 1f), new Vector2(120f, 48f), new Vector2(-20f, -32f), Blush, HideHistory);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            viewport.transform.SetParent(history, false);
            var viewRect = viewport.GetComponent<RectTransform>();
            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.offsetMin = new Vector2(16f, 210f);
            viewRect.offsetMax = new Vector2(-16f, -56f);
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            historyContent = content.GetComponent<RectTransform>();
            historyContent.anchorMin = new Vector2(0f, 1f);
            historyContent.anchorMax = new Vector2(1f, 1f);
            historyContent.pivot = new Vector2(0.5f, 1f);
            historyContent.offsetMin = historyContent.offsetMax = Vector2.zero;
            var stack = content.GetComponent<VerticalLayoutGroup>();
            stack.spacing = 12f;
            stack.padding = new RectOffset(8, 8, 8, 8);
            stack.childAlignment = TextAnchor.UpperLeft;
            stack.childControlWidth = true;
            stack.childControlHeight = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;
            var fit = content.GetComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            historyScroll = viewport.AddComponent<ScrollRect>();
            historyScroll.viewport = viewRect;
            historyScroll.content = historyContent;
            historyScroll.horizontal = false;
            historyScroll.vertical = true;
            historyScroll.movementType = ScrollRect.MovementType.Clamped;

            var chips = new GameObject("Question chips", typeof(RectTransform));
            chips.transform.SetParent(history, false);
            var chipRect = chips.GetComponent<RectTransform>();
            chipRect.anchorMin = chipRect.anchorMax = new Vector2(0.5f, 0f);
            chipRect.pivot = new Vector2(0.5f, 0f);
            chipRect.sizeDelta = new Vector2(940f, 120f);
            chipRect.anchoredPosition = new Vector2(0f, 72f);
            Pill(chipRect, "Fit & Size", new Vector2(0f, 1f), new Vector2(450f, 52f), new Vector2(235f, -28f), Card, AskFit);
            Pill(chipRect, "Lighting & Colour", new Vector2(1f, 1f), new Vector2(450f, 52f), new Vector2(-235f, -28f), Card, AskLight);
            Pill(chipRect, "Where to Hang", new Vector2(0f, 0f), new Vector2(450f, 52f), new Vector2(235f, 28f), Card, AskPlace);
            Pill(chipRect, "Story & Craft", new Vector2(1f, 0f), new Vector2(450f, 52f), new Vector2(-235f, 28f), Card, AskStory);
            BuildAsk();
            history.gameObject.SetActive(true);

            historyPill = Pill(transform, "Show questions", new Vector2(0f, 1f), new Vector2(280f, 64f), new Vector2(180f, -80f), Accent, ShowHistory).gameObject;
            historyPill.SetActive(false);
        }

        void AddHistory(string title, string question, string reply, string[] options, ConsultAction[] actions)
        {
            if (historyContent == null)
                return;

            string line = "[" + (string.IsNullOrEmpty(title) ? "This work" : title) + "] Q: " + question + " -> A: " + reply;
            var block = new GameObject("Reply", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            block.transform.SetParent(historyContent, false);
            Image plate = block.GetComponent<Image>();
            plate.sprite = Rounded();
            plate.type = Image.Type.Sliced;
            plate.color = Card;
            plate.raycastTarget = false;
            var element = block.GetComponent<LayoutElement>();
            element.minHeight = 88f;
            TMP_Text text = Label(block.transform, line, 22f, Ink, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;
            text.margin = new Vector4(12f, 8f, 12f, 8f);
            text.color = Ink;
            element.preferredHeight = Mathf.Max(88f, 36f + (line.Length / 42f) * 28f);

            if (options != null && options.Length > 0)
            {
                bool stacked = false;
                for (int i = 0; i < options.Length; i++)
                {
                    if (options[i] != null && options[i].Length > 16)
                        stacked = true;
                }

                var choices = new GameObject("Choices", typeof(RectTransform), typeof(LayoutElement));
                choices.transform.SetParent(historyContent, false);
                var choiceElement = choices.GetComponent<LayoutElement>();
                if (stacked)
                {
                    choiceElement.preferredHeight = options.Length * 60f;
                    choiceElement.minHeight = options.Length * 60f;
                    for (int i = 0; i < options.Length; i++)
                    {
                        string option = options[i];
                        Pill(choices.transform, option, new Vector2(0.5f, 1f), new Vector2(860f, 52f), new Vector2(0f, -30f - i * 60f), Accent, delegate { AcceptOption(option); });
                    }
                }
                else
                {
                    choiceElement.preferredHeight = 64f;
                    choiceElement.minHeight = 64f;
                    float width = Mathf.Min(210f, 860f / options.Length - 8f);
                    for (int i = 0; i < options.Length; i++)
                    {
                        string option = options[i];
                        float x = (i - (options.Length - 1) * 0.5f) * (width + 8f);
                        Pill(choices.transform, option, new Vector2(0.5f, 0.5f), new Vector2(width, 52f), new Vector2(x, 0f), Accent, delegate { AcceptOption(option); });
                    }
                }
            }

            if (actions != null && actions.Length > 0)
            {
                var choices = new GameObject("Actions", typeof(RectTransform), typeof(LayoutElement));
                choices.transform.SetParent(historyContent, false);
                var choiceElement = choices.GetComponent<LayoutElement>();
                choiceElement.preferredHeight = actions.Length * 60f;
                choiceElement.minHeight = actions.Length * 60f;
                for (int i = 0; i < actions.Length; i++)
                {
                    ConsultAction action = actions[i];
                    if (action == null || string.IsNullOrEmpty(action.Label))
                        continue;
                    Pill(choices.transform, action.Label, new Vector2(0.5f, 1f), new Vector2(860f, 52f), new Vector2(0f, -30f - i * 60f), Accent, delegate { ApplyAction(action); });
                }
            }
        }

        IEnumerator RevealLatest()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            if (historyContent != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(historyContent);
            if (historyScroll != null)
                historyScroll.verticalNormalizedPosition = 0f;
            if (history != null)
            {
                history.gameObject.SetActive(true);
                if (compareCard == null || !compareCard.gameObject.activeSelf)
                    history.SetAsLastSibling();
            }
        }

        void ShowHistory()
        {
            if (history != null)
            {
                history.gameObject.SetActive(true);
                if (compareCard == null || !compareCard.gameObject.activeSelf)
                    history.SetAsLastSibling();
            }
            if (historyPill != null)
                historyPill.SetActive(false);
        }

        void HideHistory()
        {
            if (history != null)
                history.gameObject.SetActive(false);
            if (historyPill != null)
                historyPill.SetActive(historyContent != null && historyContent.childCount > 0);
        }

        void BuildInspect()
        {
            inspect = CardAt("Inspect", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(920f, 980f), Vector2.zero);
            inspect.pivot = new Vector2(0.5f, 0.5f);
            inspectBody = Label(inspect, "", 26f, Ink, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            inspectBody.rectTransform.anchorMin = new Vector2(0f, 0.42f);
            inspectBody.rectTransform.anchorMax = new Vector2(1f, 1f);
            inspectBody.rectTransform.offsetMin = new Vector2(28f, 8f);
            inspectBody.rectTransform.offsetMax = new Vector2(-88f, -24f);
            Pill(inspect, "X", new Vector2(1f, 1f), new Vector2(64f, 64f), new Vector2(-24f, -48f), Blush, HideInspect);
            inspectSizeRow = new GameObject("Inspect sizes", typeof(RectTransform)).GetComponent<RectTransform>();
            inspectSizeRow.SetParent(inspect, false);
            inspectSizeRow.anchorMin = inspectSizeRow.anchorMax = new Vector2(0.5f, 0f);
            inspectSizeRow.pivot = new Vector2(0.5f, 0f);
            inspectSizeRow.sizeDelta = new Vector2(880f, 64f);
            inspectSizeRow.anchoredPosition = new Vector2(0f, 180f);
            inspectFrameRow = new GameObject("Inspect frames", typeof(RectTransform)).GetComponent<RectTransform>();
            inspectFrameRow.SetParent(inspect, false);
            inspectFrameRow.anchorMin = inspectFrameRow.anchorMax = new Vector2(0.5f, 0f);
            inspectFrameRow.pivot = new Vector2(0.5f, 0f);
            inspectFrameRow.sizeDelta = new Vector2(880f, 64f);
            inspectFrameRow.anchoredPosition = new Vector2(0f, 108f);
            Pill(inspect, "Try on Wall", new Vector2(0.5f, 0f), new Vector2(360f, 68f), new Vector2(0f, 28f), Accent, TryInspect);
            inspect.gameObject.SetActive(false);
        }

        void BuildCompare()
        {
            compareCard = CardAt("Compare", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(980f, 1100f), Vector2.zero);
            compareCard.pivot = new Vector2(0.5f, 0.5f);
            TMP_Text title = Label(compareCard, "Works tried", 30f, Ink, FontStyles.Bold, TextAlignmentOptions.TopLeft);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.sizeDelta = new Vector2(-140f, 56f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -12f);
            Pill(compareCard, "X", new Vector2(1f, 1f), new Vector2(64f, 64f), new Vector2(-24f, -44f), Blush, HideCompare);

            var viewport = new GameObject("Tried view", typeof(RectTransform), typeof(RectMask2D), typeof(Image), typeof(ScrollRect));
            viewport.transform.SetParent(compareCard, false);
            var viewRect = viewport.GetComponent<RectTransform>();
            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.offsetMin = new Vector2(16f, 20f);
            viewRect.offsetMax = new Vector2(-16f, -72f);
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            var content = new GameObject("Tried list", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            compareList = content.GetComponent<RectTransform>();
            compareList.anchorMin = new Vector2(0f, 1f);
            compareList.anchorMax = new Vector2(1f, 1f);
            compareList.pivot = new Vector2(0.5f, 1f);
            var stack = content.GetComponent<VerticalLayoutGroup>();
            stack.spacing = 12f;
            stack.childControlWidth = true;
            stack.childControlHeight = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ScrollRect scroll = viewport.GetComponent<ScrollRect>();
            scroll.viewport = viewRect;
            scroll.content = compareList;
            scroll.horizontal = false;
            scroll.vertical = true;
            compareCard.gameObject.SetActive(false);
        }

        void RefreshSelectors()
        {
            int count = session != null && session.Tried != null ? session.Tried.Count : 0;
            if (keptLabel != null)
                keptLabel.text = "Compare Tried (" + count + ")";
            if (compareButton != null)
                compareButton.SetActive(count >= 2);
        }

        void FillSizes(RectTransform row, ArtworkData work, int selected, bool onWall)
        {
            if (row == null)
                return;

            int count = work != null && work.AvailableSizes != null ? work.AvailableSizes.Count : 0;
            string key = (onWall ? "wall-size:" : "inspect-size:") + (work != null ? work.Identifier : "") + ":" + count;
            if (row.gameObject.name == key && row.childCount == count)
            {
                Recolour(row, selected);
                return;
            }

            row.gameObject.name = key;
            Clear(row);
            if (work == null || count == 0)
                return;

            float width = Mathf.Min(280f, 960f / count - 12f);
            for (int i = 0; i < count; i++)
            {
                int index = i;
                float x = (i - (count - 1) * 0.5f) * (width + 12f);
                string label = work.FormatSizeLabel(i);
                Pill(row, label, new Vector2(0.5f, 0.5f), new Vector2(width, 52f), new Vector2(x, 0f), i == selected ? Accent : Card, delegate
                {
                    ChooseInspectSize(index);
                });
            }
        }

        void FillFrames(RectTransform row, FrameOption selected, bool onWall)
        {
            if (row == null)
                return;

            int count = frames != null ? frames.Count : 0;
            string key = (onWall ? "wall-frame:" : "inspect-frame:") + count;
            if (row.gameObject.name == key && row.childCount == count)
            {
                RecolourFrame(row, selected);
                return;
            }

            row.gameObject.name = key;
            Clear(row);
            if (frames == null)
                return;

            float width = count > 0 ? Mathf.Min(230f, 960f / count - 12f) : 200f;
            for (int i = 0; i < count; i++)
            {
                FrameOption option = frames[i];
                if (option == null)
                    continue;
                float x = (i - (count - 1) * 0.5f) * (width + 12f);
                bool active = selected != null && option.FrameName == selected.FrameName;
                FrameOption captured = option;
                Pill(row, option.FrameName, new Vector2(0.5f, 0.5f), new Vector2(width, 52f), new Vector2(x, 0f), active ? Accent : Card, delegate
                {
                    ChooseInspectFrame(captured);
                });
            }
        }

        static void Recolour(RectTransform row, int selected)
        {
            for (int i = 0; i < row.childCount; i++)
            {
                Image image = row.GetChild(i).GetComponent<Image>();
                if (image != null)
                    image.color = i == selected ? Accent : Card;
            }
        }

        static void RecolourFrame(RectTransform row, FrameOption selected)
        {
            for (int i = 0; i < row.childCount; i++)
            {
                Image image = row.GetChild(i).GetComponent<Image>();
                if (image == null)
                    continue;
                image.color = selected != null && row.GetChild(i).name == selected.FrameName ? Accent : Card;
            }
        }

        static void Clear(RectTransform row)
        {
            for (int i = row.childCount - 1; i >= 0; i--)
                DestroyImmediate(row.GetChild(i).gameObject);
        }

        void OpenDetails(ArtworkData work)
        {
            if (work == null || inspect == null)
                return;

            inspectWork = work;
            inspectSize = 0;
            inspectFrame = session != null ? session.SelectedFrame : null;
            inspectBody.text = replies.DescribeTitle(work) + "\n" + replies.DescribeArtist(work) + "\n" + replies.DescribeYear(work) + "\n" + replies.DescribeMedium(work) + "\n" + replies.DescribeColour(work) + "\n" + replies.DescribeStory(work);
            FillSizes(inspectSizeRow, work, inspectSize, false);
            FillFrames(inspectFrameRow, inspectFrame, false);
            inspect.gameObject.SetActive(true);
            inspect.SetAsLastSibling();
        }

        void ChooseInspectSize(int index)
        {
            inspectSize = index;
            FillSizes(inspectSizeRow, inspectWork, inspectSize, false);
        }

        void ChooseInspectFrame(FrameOption frame)
        {
            inspectFrame = frame;
            FillFrames(inspectFrameRow, inspectFrame, false);
        }

        void TryInspect()
        {
            if (inspectWork == null)
                return;
            Place(inspectWork, inspectSize, inspectFrame);
        }

        void HideInspect()
        {
            if (inspect != null)
                inspect.gameObject.SetActive(false);
        }

        void ToggleCompare()
        {
            if (compareCard != null && compareCard.gameObject.activeSelf)
            {
                HideCompare();
                return;
            }

            if (session == null || session.Tried == null || session.Tried.Count < 2)
            {
                ShowToast("Try a second work before comparing.");
                return;
            }

            FillTried();
            compareCard.gameObject.SetActive(true);
            compareCard.SetAsLastSibling();
        }

        void FillTried()
        {
            if (compareList == null || session == null || session.Tried == null)
                return;

            for (int i = compareList.childCount - 1; i >= 0; i--)
                Destroy(compareList.GetChild(i).gameObject);

            for (int i = 0; i < session.Tried.Count; i++)
            {
                TriedPiece piece = session.Tried[i];
                if (piece != null && piece.Work != null)
                    AddTried(piece);
            }
        }

        void AddTried(TriedPiece piece)
        {
            var row = new GameObject(piece.Work.Title, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            row.transform.SetParent(compareList, false);
            Image plate = row.GetComponent<Image>();
            plate.sprite = Rounded();
            plate.type = Image.Type.Sliced;
            plate.color = Blush;
            row.GetComponent<LayoutElement>().preferredHeight = 168f;
            TriedPiece chosen = piece;

            if (piece.Work.ArtworkSprite != null)
            {
                var picture = new GameObject("Picture", typeof(RectTransform), typeof(Image));
                picture.transform.SetParent(row.transform, false);
                var pictureRect = picture.GetComponent<RectTransform>();
                pictureRect.anchorMin = new Vector2(0f, 0.5f);
                pictureRect.anchorMax = new Vector2(0f, 0.5f);
                pictureRect.pivot = new Vector2(0f, 0.5f);
                pictureRect.sizeDelta = new Vector2(120f, 140f);
                pictureRect.anchoredPosition = new Vector2(12f, 0f);
                Image sprite = picture.GetComponent<Image>();
                sprite.sprite = piece.Work.ArtworkSprite;
                sprite.preserveAspect = true;
                sprite.raycastTarget = false;
            }

            string frame = piece.Frame != null && piece.Frame.FrameName.Length > 0 ? piece.Frame.FrameName : "Unframed";
            string line = piece.Work.Title + "\n" + piece.Work.FormatSizeLabel(piece.SizeIndex) + "\n" + frame + "\n" + piece.Work.Colour + ", " + piece.Work.Mood + "\n" + piece.Status;
            TMP_Text text = Label(row.transform, line, 20f, Ink, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            text.rectTransform.offsetMin = new Vector2(148f, 56f);
            text.rectTransform.offsetMax = new Vector2(-16f, -8f);
            Pill(row.transform, "Put Back on Wall", new Vector2(1f, 0f), new Vector2(260f, 44f), new Vector2(-16f, 28f), Accent, delegate
            {
                if (wall != null)
                    wall.LoadArtworkIntoWall(chosen.Work, chosen.SizeIndex, chosen.Frame);
            });
        }

        void HideCompare()
        {
            if (compareCard != null)
                compareCard.gameObject.SetActive(false);
        }

        void NextKept()
        {
            if (session == null || session.Kept == null || session.Kept.Count == 0)
                return;

            keptCursor = (keptCursor + 1) % session.Kept.Count;
            KeptChoice choice = session.Kept[keptCursor];
            if (choice == null || choice.Work == null)
                return;

            Place(choice.Work, choice.SizeIndex, choice.Frame);
            if (compareBody != null)
                compareBody.text = CompareText();
            if (compareCard != null)
            {
                compareCard.gameObject.SetActive(true);
                compareCard.SetAsLastSibling();
            }
        }

        string CompareText()
        {
            if (session == null || session.Kept == null)
                return "";

            string text = "";
            for (int i = 0; i < session.Kept.Count; i++)
            {
                KeptChoice choice = session.Kept[i];
                if (choice == null || choice.Work == null)
                    continue;
                string frame = choice.Frame != null && !string.IsNullOrEmpty(choice.Frame.FrameName) ? choice.Frame.FrameName : "Unframed";
                string mark = i == keptCursor ? "On the wall\n" : "";
                text += mark + choice.Work.Title + "\n" + choice.Work.FormatSizeLabel(choice.SizeIndex) + "\n" + frame + "\n" + choice.Work.Colour + "\n" + choice.Work.Style + "\n\n";
            }

            return text;
        }

        RectTransform CardAt(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 position)
        {
            var card = new GameObject(name, typeof(RectTransform), typeof(Image));
            card.transform.SetParent(transform, false);
            var rect = card.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            Image image = card.GetComponent<Image>();
            image.sprite = Rounded();
            image.type = Image.Type.Sliced;
            image.color = Card;
            return rect;
        }

        Button Pill(Transform parent, string caption, Vector2 anchor, Vector2 size, Vector2 position, Color fill, UnityEngine.Events.UnityAction tap)
        {
            var pill = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button));
            pill.transform.SetParent(parent, false);
            var rect = pill.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            Image image = pill.GetComponent<Image>();
            image.sprite = Rounded();
            image.type = Image.Type.Sliced;
            image.color = fill;
            Label(pill.transform, caption, caption.Length > 12 ? 22f : 26f, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
            Button button = pill.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(tap);
            return button;
        }

        TMP_Text Label(Transform parent, string caption, float size, Color color, FontStyles style, TextAlignmentOptions alignment)
        {
            var host = new GameObject("Label", typeof(RectTransform));
            host.transform.SetParent(parent, false);
            var rect = host.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = host.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
                text.font = TMP_Settings.defaultFontAsset;
            text.text = caption;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        Sprite Rounded()
        {
            if (rounded != null)
                return rounded;

            const int edge = 96;
            var texture = new Texture2D(edge, edge, TextureFormat.RGBA32, false);
            var pixels = new Color32[edge * edge];
            float radius = 28f;
            var clear = new Color32(255, 255, 255, 0);
            var solid = new Color32(255, 255, 255, 255);
            for (int y = 0; y < edge; y++)
            {
                for (int x = 0; x < edge; x++)
                {
                    float dx = 0f;
                    float dy = 0f;
                    if (x < radius)
                        dx = radius - x;
                    else if (x > edge - 1 - radius)
                        dx = x - (edge - 1 - radius);
                    if (y < radius)
                        dy = radius - y;
                    else if (y > edge - 1 - radius)
                        dy = y - (edge - 1 - radius);
                    pixels[y * edge + x] = dx * dx + dy * dy <= radius * radius ? solid : clear;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            rounded = Sprite.Create(texture, new Rect(0f, 0f, edge, edge), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(32f, 32f, 32f, 32f));
            return rounded;
        }
    }
}
