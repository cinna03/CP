using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Cinnarios.Catalogue.Data;
using Cinnarios.Catalogue.Networking;
using Cinnarios.Catalogue.Shop;
using Cinnarios.Launch;
using Cinnarios.Painting.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cinnarios.Placement.UI
{
    /// <summary>
    /// One bottom sheet for the wall session. Before the painting is locked, the sheet
    /// only helps place it. After the lock, that same sheet asks whether the piece is
    /// right, then what is unsure, then offers paintings to try in the same spot.
    /// A long press reads the locked painting and uses the same sheet, so nothing stacks.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class PlacementHud : MonoBehaviour, IScreenHitTest
    {
        private enum Step
        {
            None,
            Satisfaction,
            Decline,
            Aspect,
            Direction,
            Suggestions,
            Save,
            Details,
            Waiting,
            Talk
        }

        [SerializeField] private WallPlacementController controller;

        private VisualElement _root;
        private VisualElement _sheet;
        private Label _hint;
        private Label _size;
        private VisualElement _decision;
        private VisualElement _placementRow;
        private Button _lockButton;
        private Button _shopButton;
        private ArtworkLaunchLoader _loader;
        private PlacementState _previous = PlacementState.Scanning;
        private Step _step = Step.None;
        private Step _stepUnderDetails = Step.Satisfaction;
        private WallAdvice.Aspect _aspect;
        private bool _saving;
        private bool _justArrived;
        private string _assistantLine;
        private TextField _say;
        private VisualElement _composer;
        private readonly TextureDownloader _pictures = new TextureDownloader();

        private void OnEnable()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            Build();
            BindLoader();
            if (controller != null)
            {
                controller.StateChanged += Render;
                controller.HeldOnPainting += ShowDetails;
                Render(controller.State);
            }
        }

        private void Start() => BindLoader();

        private void OnDisable()
        {
            if (_loader != null)
            {
                _loader.Changed -= OnPaintingChanged;
            }

            if (controller != null)
            {
                controller.StateChanged -= Render;
                controller.HeldOnPainting -= ShowDetails;
            }
        }

        public bool IsOverUi(Vector2 screenPosition)
        {
            if (_root?.panel == null)
            {
                return false;
            }

            Vector2 flipped = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
            Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(_root.panel, flipped);
            VisualElement picked = _root.panel.Pick(panelPosition);
            return picked != null && picked != _root;
        }

        private void Build()
        {
            _root.Clear();
            _root.pickingMode = PickingMode.Ignore;

            _sheet = new VisualElement();
            _sheet.style.position = Position.Absolute;
            _sheet.style.left = 16;
            _sheet.style.right = 16;
            _sheet.style.bottom = 28;
            _sheet.style.backgroundColor = new Color(0.11f, 0.1f, 0.08f, 0.92f);
            _sheet.style.alignItems = Align.Stretch;
            SetPadding(_sheet, 20, 20);
            SetRadius(_sheet, 28);

            _hint = new Label { pickingMode = PickingMode.Ignore };
            _hint.style.color = Color.white;
            _hint.style.fontSize = 28;
            _hint.style.whiteSpace = WhiteSpace.Normal;
            _hint.style.unityTextAlign = TextAnchor.MiddleCenter;
            _hint.style.marginBottom = 8;

            _size = new Label { pickingMode = PickingMode.Ignore };
            _size.style.color = new Color(1f, 0.96f, 0.9f, 0.9f);
            _size.style.fontSize = 20;
            _size.style.whiteSpace = WhiteSpace.Normal;
            _size.style.unityTextAlign = TextAnchor.MiddleCenter;
            _size.style.marginBottom = 12;
            _size.style.display = DisplayStyle.None;

            _decision = new VisualElement { pickingMode = PickingMode.Ignore };
            _decision.style.display = DisplayStyle.None;

            _placementRow = new VisualElement { pickingMode = PickingMode.Ignore };
            _placementRow.style.flexDirection = FlexDirection.Row;
            _placementRow.style.justifyContent = Justify.Center;
            _placementRow.style.marginTop = 8;
            _shopButton = CreateButton("Shop", ReturnToShop);
            _lockButton = CreateButton("Lock", () => controller.Lock());
            _placementRow.Add(_shopButton);
            _placementRow.Add(_lockButton);

            _composer = new VisualElement();
            _composer.style.flexDirection = FlexDirection.Row;
            _composer.style.alignItems = Align.Center;
            _composer.style.marginTop = 8;
            _composer.style.display = DisplayStyle.None;
            _say = new TextField();
            _say.style.flexGrow = 1;
            _say.style.fontSize = 24;
            _say.style.minHeight = 64;
            _say.style.color = Color.black;
            _say.style.backgroundColor = new Color(0.96f, 0.94f, 0.9f, 1f);
            SetRadius(_say, 16);
            _composer.Add(_say);
            Button send = CreateButton("Send", SendWhatTheySaid);
            send.style.marginLeft = 8;
            send.style.marginBottom = 0;
            _composer.Add(send);

            _sheet.Add(_hint);
            _sheet.Add(_size);
            _sheet.Add(_decision);
            _sheet.Add(_composer);
            _sheet.Add(_placementRow);
            _root.Add(_sheet);
        }

        private void Render(PlacementState state)
        {
            bool justLocked = state == PlacementState.Locked && _previous != PlacementState.Locked;
            _previous = state;
            if (state != PlacementState.Locked)
            {
                _step = Step.None;
                _assistantLine = null;
                ClearDecision();
            }
            else if (justLocked && _step == Step.None)
            {
                _step = Step.Talk;
            }

            switch (state)
            {
                case PlacementState.Scanning:
                    ShowPlacement(_loader != null && !_loader.PictureReady
                        ? "Aim at a corner while the painting loads. Move slowly."
                        : "Aim at a corner or the edge of the wall, and move slowly.", false);
                    break;
                case PlacementState.Aiming:
                    ShowPlacement("Tap the wall where it should hang.", false);
                    break;
                case PlacementState.Positioning:
                    ShowPlacement(
                        _loader != null && !_loader.PictureReady
                            ? "Drag it on this wall. The picture is still arriving."
                            : "Drag to move it on this wall. Lock when the spot is right.",
                        true);
                    break;
                case PlacementState.Locked:
                    ShowDecision();
                    break;
            }
        }

        private void ShowPlacement(string hint, bool lockVisible)
        {
            _hint.text = hint;
            _decision.style.display = DisplayStyle.None;
            if (_composer != null)
            {
                _composer.style.display = DisplayStyle.None;
            }
            SetVisible(_lockButton, lockVisible);
            _placementRow.style.display = DisplayStyle.Flex;
            ShowRecordedSize();
        }

        private void ShowDecision()
        {
            _decision.style.display = DisplayStyle.Flex;
            SetVisible(_lockButton, false);
            _placementRow.style.display = DisplayStyle.None;
            if (_composer != null)
            {
                _composer.style.display = DisplayStyle.None;
            }

            ClearDecision();
            ShowRecordedSize();

            switch (_step)
            {
                case Step.Waiting:
                    if (_loader != null && _loader.PictureFailed)
                    {
                        _hint.text = "That painting could not be loaded.";
                        AddChoice("Say something else", () => Go(Step.Talk));
                    }
                    else
                    {
                        string title = _loader?.CurrentArtwork?.Title;
                        _hint.text = string.IsNullOrEmpty(title)
                            ? "Bringing the next painting. One moment."
                            : $"Bringing {title}. One moment.";
                    }
                    break;
                case Step.Talk:
                    if (string.IsNullOrEmpty(_assistantLine))
                    {
                        _assistantLine = OpeningLine();
                    }

                    _hint.text = _assistantLine;
                    _composer.style.display = DisplayStyle.Flex;
                    AddChoice("This one is right", () => Hear("yes"));
                    AddChoice("It's too big", () => Hear("too big"));
                    AddChoice("It's too small", () => Hear("too small"));
                    AddChoice("The price", () => Hear("the price is high"));
                    AddChoice("Something different", () => Hear("something different"));
                    AddChoice("I don't want it", () => Hear("i don't want this"));
                    break;
                case Step.Satisfaction:
                    _hint.text = _justArrived
                        ? "The new painting is on the wall. Hold it to read it."
                        : "It stays on this wall while you walk. Hold the painting to read it.";
                    _justArrived = false;
                    _decision.Add(Prompt("Are you satisfied with this piece?"));
                    AddChoice("Yes", () => Go(Step.Save));
                    AddChoice("Not sure", () => Go(Step.Aspect));
                    AddChoice("No", () => Go(Step.Decline));
                    break;
                case Step.Decline:
                    _hint.text = "A no can end the session, or it can look for a better fit.";
                    _decision.Add(Prompt("What do you want to do?"));
                    AddChoice("Review it and find a better fit", () => Go(Step.Aspect));
                    AddChoice("I don't want this piece", LeaveWithoutIt);
                    break;
                case Step.Aspect:
                    _hint.text = "Name the part that is in the way. The next paintings follow that.";
                    _decision.Add(Prompt("What are you unsure about?"));
                    AddChoice("The size", () => AskDirection(WallAdvice.Aspect.Size));
                    AddChoice("The colour guide", () => AskDirection(WallAdvice.Aspect.Colour));
                    AddChoice("Whether it fits", () => AskDirection(WallAdvice.Aspect.Fit));
                    AddChoice("The piece itself", () => AskDirection(WallAdvice.Aspect.Piece));
                    AddChoice("The price", () => AskDirection(WallAdvice.Aspect.Price));
                    AddChoice("The light", () => AskDirection(WallAdvice.Aspect.Light));
                    break;
                case Step.Direction:
                    _hint.text = DirectionHint();
                    _decision.Add(Prompt(DirectionQuestion()));
                    foreach ((string label, WallAdvice.Lean lean) in DirectionChoices())
                    {
                        WallAdvice.Lean chosen = lean;
                        AddChoice(label, () => ShowSuggestions(chosen));
                    }
                    break;
                case Step.Suggestions:
                    _hint.text = string.IsNullOrEmpty(_assistantLine)
                        ? "These use their own size. After you tap one, slide it on this wall."
                        : _assistantLine;
                    _composer.style.display = DisplayStyle.Flex;
                    _decision.Add(Prompt("Tap one to hang it on this wall. You can slide it before you lock."));
                    break;
                case Step.Save:
                    _hint.text = "You can keep a picture of this view, then leave.";
                    _decision.Add(Prompt("Save this view for reference?"));
                    AddChoice("Save this view", SaveView);
                    AddChoice("Done", FinishSatisfied);
                    break;
                case Step.Details:
                    _hint.text = "From the museum record.";
                    _decision.Add(DetailLabel());
                    AddChoice("Close", () => Go(_stepUnderDetails));
                    break;
            }
        }

        private void SendWhatTheySaid()
        {
            string said = _say == null ? string.Empty : _say.value;
            if (_say != null)
            {
                _say.value = string.Empty;
            }

            Hear(said);
        }

        private void Hear(string said)
        {
            ArtworkRecord artwork = _loader?.CurrentArtwork;
            DecisionTalk.Turn turn = DecisionTalk.Hear(said, artwork);
            _assistantLine = turn.Reply;
            if (turn.Satisfied)
            {
                Go(Step.Save);
                return;
            }

            if (turn.Leave)
            {
                Go(Step.Decline);
                return;
            }

            if (turn.WantsNewSpot)
            {
                PlaceThisOneAgain();
                return;
            }

            if (!turn.WantsSuggestions)
            {
                Go(Step.Talk);
                return;
            }

            _aspect = turn.Aspect;
            WallVisitRecord.Remember("unsure", null);
            ShowSuggestions(turn.Lean);
        }

        private string OpeningLine()
        {
            ArtworkRecord artwork = _loader?.CurrentArtwork;
            if (artwork == null)
            {
                return "Tell me what is in the way, or say that this one is right.";
            }

            _justArrived = false;
            return DecisionTalk.Opening(artwork);
        }

        private void AskDirection(WallAdvice.Aspect aspect)
        {
            _aspect = aspect;
            Go(Step.Direction);
        }

        private string DirectionHint()
        {
            if (_aspect == WallAdvice.Aspect.Light)
            {
                return "The museum record does not describe lamp light. A lighter or deeper colour guide is only a shop stand-in.";
            }

            return "Keep what you like. Change what you do not.";
        }

        private string DirectionQuestion()
        {
            switch (_aspect)
            {
                case WallAdvice.Aspect.Size:
                case WallAdvice.Aspect.Fit:
                    return "Which way should the next one go?";
                case WallAdvice.Aspect.Price:
                    return "Which way should the price go?";
                case WallAdvice.Aspect.Light:
                    return "Which way should the colour guide go?";
                default:
                    return "Should the next one keep this, or leave it?";
            }
        }

        private IEnumerable<(string label, WallAdvice.Lean lean)> DirectionChoices()
        {
            switch (_aspect)
            {
                case WallAdvice.Aspect.Size:
                case WallAdvice.Aspect.Fit:
                    yield return ("Keep a similar size", WallAdvice.Lean.Keep);
                    yield return ("Too large", WallAdvice.Lean.Smaller);
                    yield return ("Too small", WallAdvice.Lean.Larger);
                    yield break;
                case WallAdvice.Aspect.Price:
                    yield return ("Keep a similar price", WallAdvice.Lean.Keep);
                    yield return ("A lower price", WallAdvice.Lean.Cheaper);
                    yield return ("A higher price", WallAdvice.Lean.Dearer);
                    yield break;
                case WallAdvice.Aspect.Light:
                    yield return ("A lighter guide", WallAdvice.Lean.Lighter);
                    yield return ("A deeper guide", WallAdvice.Lean.Deeper);
                    yield break;
                default:
                    yield return ("More like this", WallAdvice.Lean.Keep);
                    yield return ("Something different", WallAdvice.Lean.Different);
                    yield break;
            }
        }

        private void ShowSuggestions(WallAdvice.Lean lean)
        {
            _step = Step.Suggestions;
            Render(PlacementState.Locked);
            FillSuggestions(lean);
        }

        private async void FillSuggestions(WallAdvice.Lean lean)
        {
            if (_loader == null || _step != Step.Suggestions)
            {
                return;
            }

            IReadOnlyList<ShopArtworkData> catalogue;
            try
            {
                catalogue = await _loader.CatalogueAsync();
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"Suggestions did not load: {exception.Message}", this);
                catalogue = new List<ShopArtworkData>();
            }

            if (_step != Step.Suggestions)
            {
                return;
            }

            ArtworkRecord current = _loader.CurrentArtwork;
            ShopArtworkData currentData = null;
            if (catalogue != null && current != null)
            {
                foreach (ShopArtworkData item in catalogue)
                {
                    if (item.sourceId == current.SourceId)
                    {
                        currentData = item;
                        break;
                    }
                }
            }

            List<WallAdvice.Suggestion> suggestions = WallAdvice.Suggest(catalogue, currentData, _aspect, lean);
            if (suggestions.Count == 0)
            {
                _decision.Add(Prompt("The other paintings could not be loaded. Ask about another part, or keep this one."));
                AddChoice("Say something else", () => Go(Step.Talk));
                return;
            }

            _decision.Clear();
            _hint.text = _assistantLine;
            _decision.Add(Prompt("Tap one to hang it on this wall. You can slide it before you lock."));
            foreach (WallAdvice.Suggestion suggestion in suggestions)
            {
                string sourceId = suggestion.Artwork.sourceId;
                string label = $"{suggestion.Artwork.title}\n{suggestion.Artwork.widthCm:0.#} × {suggestion.Artwork.heightCm:0.#} cm\n{suggestion.Because}";
                AddPictureChoice(
                    label,
                    suggestion.Artwork.thumbnailUrl,
                    suggestion.Artwork.imageUrl,
                    suggestion.Artwork.title,
                    () => SwapTo(sourceId));
            }

            AddChoice("Say something else", () => Go(Step.Talk));
        }

        // The lock stays. A new spot means placing this same painting from the start.
        private void PlaceThisOneAgain()
        {
            if (controller == null || controller.State != PlacementState.Locked)
            {
                return;
            }

            _step = Step.None;
            _assistantLine = null;
            controller.ScanAgain();
        }

        private void SwapTo(string sourceId)
        {
            // The new painting stays on this wall. The person can slide it, then lock again.
            _justArrived = false;
            _step = Step.None;
            controller?.Move();
            _loader?.ShowSource(sourceId);
        }

        private void ShowDetails()
        {
            if (controller == null || controller.State != PlacementState.Locked || _loader?.CurrentArtwork == null)
            {
                return;
            }

            if (_step != Step.Details)
            {
                _stepUnderDetails = _step == Step.None ? Step.Satisfaction : _step;
            }

            _step = Step.Details;
            Render(PlacementState.Locked);
        }

        private Label DetailLabel()
        {
            ArtworkRecord artwork = _loader.CurrentArtwork;
            string date = string.IsNullOrWhiteSpace(artwork.Date) ? string.Empty : $", {artwork.Date}";
            long francs = (long)decimal.Round(artwork.Price * 1477.385m, 0, MidpointRounding.AwayFromZero);
            var label = new Label(
                $"{artwork.Title}\n{artwork.Artist}{date}\n{artwork.MediumDescription}\n{artwork.Dimensions.WidthCm:0.#} × {artwork.Dimensions.HeightCm:0.#} × {artwork.Dimensions.DepthCm:0.#} cm\nRWF {francs.ToString("N0", CultureInfo.InvariantCulture)}")
            {
                pickingMode = PickingMode.Ignore
            };
            label.style.color = Color.white;
            label.style.fontSize = 26;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.marginBottom = 12;
            return label;
        }

        private void SaveView()
        {
            if (_saving)
            {
                return;
            }

            StartCoroutine(CaptureView());
        }

        private IEnumerator CaptureView()
        {
            _saving = true;
            _sheet.style.display = DisplayStyle.None;
            yield return null;
            yield return new WaitForEndOfFrame();
            Texture2D image = ScreenCapture.CaptureScreenshotAsTexture();
            _sheet.style.display = DisplayStyle.Flex;
            string fileName = null;
            if (image != null)
            {
                byte[] png = image.EncodeToPNG();
                Destroy(image);
                string folder = Path.Combine(Application.persistentDataPath, "wall-tries");
                Directory.CreateDirectory(folder);
                string sourceId = _loader?.CurrentArtwork?.SourceId ?? "view";
                fileName = $"{sourceId}-{System.DateTime.UtcNow:yyyyMMddHHmmss}.png";
                File.WriteAllBytes(Path.Combine(folder, fileName), png);
            }

            _saving = false;
            WallVisitRecord.Remember("satisfied", fileName);
            _hint.text = fileName == null ? "The view could not be saved." : "Saved on this phone.";
            ClearDecision();
            _decision.Add(Prompt(fileName == null ? "You can still leave this session." : "The picture stays with this try."));
            AddChoice("Done", FinishSatisfied);
        }

        private void ReturnToShop()
        {
            FindAnyObjectByType<ShopStage>()?.EndTheSession();
        }

        private void FinishSatisfied()
        {
            WallVisitRecord.Remember("satisfied", null);
            FindAnyObjectByType<ShopStage>()?.EndTheSession();
        }

        private void LeaveWithoutIt()
        {
            WallVisitRecord.Remember("declined", null);
            FindAnyObjectByType<ShopStage>()?.EndTheSession();
        }

        private void Go(Step step)
        {
            if (step == Step.Aspect)
            {
                WallVisitRecord.Remember("unsure", null);
            }

            _step = step;
            if (controller != null && controller.State == PlacementState.Locked)
            {
                Render(PlacementState.Locked);
            }
        }

        private void OnPaintingChanged()
        {
            if (controller == null || controller.State != PlacementState.Locked)
            {
                if (controller != null)
                {
                    Render(controller.State);
                }

                return;
            }

            if (_step == Step.Waiting && _loader != null && _loader.PictureReady)
            {
                _justArrived = true;
                _assistantLine = null;
                _step = Step.Talk;
            }

            Render(PlacementState.Locked);
        }

        private void BindLoader()
        {
            ArtworkLaunchLoader found = FindAnyObjectByType<ArtworkLaunchLoader>();
            if (found == null || found == _loader)
            {
                return;
            }

            if (_loader != null)
            {
                _loader.Changed -= OnPaintingChanged;
            }

            _loader = found;
            _loader.Changed += OnPaintingChanged;
        }

        private void ShowRecordedSize()
        {
            PaintingDimensions dimensions = _loader?.CurrentArtwork?.Dimensions;
            if (_size == null || dimensions == null)
            {
                if (_size != null)
                {
                    _size.style.display = DisplayStyle.None;
                }

                return;
            }

            _size.text = $"{Centimetres(dimensions.WidthCm)} × {Centimetres(dimensions.HeightCm)} cm, the size the shop recorded.";
            _size.style.display = DisplayStyle.Flex;
        }

        private static string Centimetres(float value) =>
            value.ToString("0.##", CultureInfo.InvariantCulture);

        private void ClearDecision()
        {
            _decision.Clear();
        }

        private static Label Prompt(string text)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.style.color = Color.white;
            label.style.fontSize = 30;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.marginBottom = 12;
            return label;
        }

        private void AddChoice(string text, System.Action onClick)
        {
            Button button = CreateButton(text, onClick);
            button.style.marginBottom = 8;
            button.style.marginLeft = 0;
            button.style.marginRight = 0;
            button.style.whiteSpace = WhiteSpace.Normal;
            button.style.height = StyleKeyword.Auto;
            button.style.minHeight = 80;
            _decision.Add(button);
        }

        private void AddPictureChoice(string text, string imageUrl, string otherUrl, string workTitle, System.Action onClick)
        {
            var button = new Button(onClick) { text = string.Empty };
            button.style.flexDirection = FlexDirection.Row;
            button.style.alignItems = Align.Center;
            button.style.height = StyleKeyword.Auto;
            button.style.minHeight = 132;
            button.style.marginBottom = 8;
            button.style.backgroundColor = new Color(1f, 1f, 1f, 0.12f);
            button.style.borderTopWidth = 0;
            button.style.borderBottomWidth = 0;
            button.style.borderLeftWidth = 0;
            button.style.borderRightWidth = 0;
            SetRadius(button, 24);
            SetPadding(button, 10, 12);

            var image = new Image { scaleMode = ScaleMode.ScaleToFit };
            image.style.width = 108;
            image.style.height = 108;
            image.style.marginRight = 12;
            image.style.backgroundColor = new Color(1f, 1f, 1f, 0.08f);
            button.Add(image);

            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.style.color = Color.white;
            label.style.fontSize = 22;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.flexGrow = 1;
            button.Add(label);
            _decision.Add(button);

            if (!string.IsNullOrEmpty(imageUrl) || !string.IsNullOrEmpty(otherUrl))
            {
                StartCoroutine(ShowThumbnailPair(imageUrl, otherUrl, workTitle, image));
            }
        }

        private IEnumerator ShowThumbnailPair(string url, string otherUrl, string workTitle, Image target)
        {
            yield return ShowThumbnail(url, workTitle, target);
            if (target != null && target.panel != null && target.image == null && !string.IsNullOrEmpty(otherUrl))
            {
                yield return ShowThumbnail(otherUrl, workTitle, target);
            }
        }

        private IEnumerator ShowThumbnail(string url, string workTitle, Image target)
        {
            if (string.IsNullOrEmpty(url) || target == null)
            {
                yield break;
            }

            for (int attempt = 0; attempt < 2; attempt++)
            {
                Task<Texture2D> download = _pictures.GetAsync(url, 360, workTitle);
                while (!download.IsCompleted)
                {
                    yield return null;
                }

                if (target.panel == null)
                {
                    yield break;
                }

                if (!download.IsFaulted && download.Result != null)
                {
                    target.scaleMode = ScaleMode.ScaleToFit;
                    target.image = download.Result;
                    target.style.backgroundImage = Background.FromTexture2D(download.Result);
                    target.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                    yield break;
                }

                if (download.IsFaulted)
                {
                    Debug.LogWarning($"Suggestion picture failed: {download.Exception?.GetBaseException().Message}");
                }

                yield return new WaitForSeconds(0.75f);
            }
        }

        private static Button CreateButton(string text, System.Action onClick)
        {
            var button = new Button(onClick) { text = text };
            button.style.height = 88;
            button.style.minWidth = 160;
            button.style.marginLeft = 8;
            button.style.marginRight = 8;
            button.style.fontSize = 28;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.color = Color.white;
            button.style.backgroundColor = new Color(1f, 1f, 1f, 0.12f);
            button.style.borderTopWidth = 0;
            button.style.borderBottomWidth = 0;
            button.style.borderLeftWidth = 0;
            button.style.borderRightWidth = 0;
            SetRadius(button, 44);
            SetPadding(button, 12, 24);
            return button;
        }

        private static void SetVisible(VisualElement element, bool visible) =>
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        private static void SetPadding(VisualElement element, float vertical, float horizontal)
        {
            element.style.paddingTop = vertical;
            element.style.paddingBottom = vertical;
            element.style.paddingLeft = horizontal;
            element.style.paddingRight = horizontal;
        }

        private static void SetRadius(VisualElement element, float radius)
        {
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
        }
    }
}
