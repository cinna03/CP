using System.Collections.Generic;
using Capstone.App.Data;
using Capstone.App.Services;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Capstone.App.UI
{
    /// <summary>
    /// The opened work. The picture fills the view. Blank artist, year, and medium rows stay hidden.
    /// Size pills and frame swatches preview a choice. Try on My Wall writes that choice into the session.
    /// </summary>
    public sealed class ArtworkDetailViewController : MonoBehaviour
    {
        public const string TryOnWallLabel = "Try on My Wall";

        [SerializeField] ArtworkSessionState session;
        [SerializeField] UIThemeData theme;
        [SerializeField] FrameOption[] frames = new FrameOption[0];
        [SerializeField] Image artworkImage;
        [SerializeField] Image framePreview;
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] TMP_Text dimensionsLabel;
        [SerializeField] TMP_Text colourLabel;
        [SerializeField] TMP_Text storyLabel;
        [SerializeField] TMP_Text artistLabel;
        [SerializeField] GameObject artistRow;
        [SerializeField] TMP_Text yearLabel;
        [SerializeField] GameObject yearRow;
        [SerializeField] TMP_Text mediumLabel;
        [SerializeField] GameObject mediumRow;
        [SerializeField] RectTransform sizePillRoot;
        [SerializeField] RectTransform frameSwatchRoot;
        [SerializeField] Button tryOnWallButton;
        [SerializeField] TMP_Text tryOnWallLabel;
        [SerializeField] UnityEvent onTryOnWall = new UnityEvent();
        [SerializeReference] IArtworkQueryService query;

        readonly List<GameObject> spawned = new List<GameObject>();
        ArtworkData artwork;
        int sizeIndex;
        FrameOption selectedFrame;

        public UnityEvent OnTryOnWall => onTryOnWall;

        void Awake()
        {
            if (query == null)
                query = new ArtworkQueryService();
            if (tryOnWallButton != null)
                tryOnWallButton.onClick.AddListener(TryOnWall);
            if (tryOnWallLabel != null)
                tryOnWallLabel.text = TryOnWallLabel;
        }

        void OnDestroy()
        {
            if (tryOnWallButton != null)
                tryOnWallButton.onClick.RemoveListener(TryOnWall);
            ClearSpawned();
        }

        public void Show(ArtworkData next)
        {
            artwork = next;
            sizeIndex = 0;
            selectedFrame = null;
            if (session != null && next != null && session.SelectedArtwork == next)
            {
                sizeIndex = session.SizeIndex;
                selectedFrame = session.SelectedFrame;
            }

            Refresh();
        }

        public void TryOnWall()
        {
            if (artwork == null)
                return;

            if (session != null)
                session.Select(artwork, sizeIndex, selectedFrame);

            onTryOnWall.Invoke();
        }

        void Refresh()
        {
            ClearSpawned();
            if (artwork == null)
                return;

            if (artworkImage != null)
            {
                artworkImage.sprite = artwork.ArtworkSprite;
                artworkImage.preserveAspect = true;
                artworkImage.color = Color.white;
                artworkImage.enabled = artwork.ArtworkSprite != null;
            }

            if (titleLabel != null)
                titleLabel.text = artwork.Title;
            if (dimensionsLabel != null)
                dimensionsLabel.text = artwork.FormatSizeLabel(sizeIndex);
            if (colourLabel != null)
                colourLabel.text = artwork.Colour;
            if (storyLabel != null)
                storyLabel.text = query == null ? artwork.StoryNote : query.DescribeStory(artwork);

            ShowOptional(artistRow, artistLabel, artwork.Artist, !string.IsNullOrWhiteSpace(artwork.Artist));
            ShowOptional(yearRow, yearLabel, artwork.Year.ToString(), artwork.Year != 0);
            ShowOptional(mediumRow, mediumLabel, artwork.Medium, !string.IsNullOrWhiteSpace(artwork.Medium));
            BuildSizePills();
            BuildFrameSwatches();
            PaintFramePreview();
        }

        void ShowOptional(GameObject row, TMP_Text label, string value, bool visible)
        {
            if (row != null)
                row.SetActive(visible);
            else if (label != null)
                label.gameObject.SetActive(visible);

            if (visible && label != null)
                label.text = value;
        }

        void BuildSizePills()
        {
            if (sizePillRoot == null)
                return;

            IReadOnlyList<Vector2> sizes = artwork.AvailableSizes;
            if (sizes == null)
                return;

            for (int i = 0; i < sizes.Count; i++)
            {
                int captured = i;
                Button pill = CreatePill(sizePillRoot, artwork.FormatSizeLabel(i), captured == sizeIndex);
                pill.onClick.AddListener(delegate { ChooseSize(captured); });
            }
        }

        void BuildFrameSwatches()
        {
            if (frameSwatchRoot == null || frames == null)
                return;

            for (int i = 0; i < frames.Length; i++)
            {
                FrameOption frame = frames[i];
                if (frame == null)
                    continue;

                Button swatch = CreatePill(frameSwatchRoot, frame.FrameName, frame == selectedFrame);
                Image image = swatch.GetComponent<Image>();
                if (image != null)
                    image.color = frame.UIHighlightColor;
                swatch.onClick.AddListener(delegate { ChooseFrame(frame); });
            }
        }

        void ChooseSize(int index)
        {
            sizeIndex = index;
            Refresh();
        }

        void ChooseFrame(FrameOption frame)
        {
            selectedFrame = frame;
            Refresh();
        }

        void PaintFramePreview()
        {
            if (framePreview == null)
                return;

            if (selectedFrame == null)
            {
                framePreview.color = theme != null ? theme.CardBorder : Color.white;
                return;
            }

            framePreview.color = selectedFrame.UIHighlightColor;
        }

        Button CreatePill(RectTransform parent, string label, bool selected)
        {
            GameObject pill = new GameObject("Pill", typeof(RectTransform), typeof(Image), typeof(Button));
            pill.transform.SetParent(parent, false);
            spawned.Add(pill);

            Image image = pill.GetComponent<Image>();
            if (theme != null)
                image.color = selected ? theme.PrimaryAccent : theme.SecondaryAccent;

            GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(pill.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 6f);
            textRect.offsetMax = new Vector2(-12f, -6f);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 18f;
            text.color = theme != null ? theme.PrimaryText : Color.black;
            if (theme != null && theme.Font != null)
                text.font = theme.Font;
            else if (TMP_Settings.defaultFontAsset != null)
                text.font = TMP_Settings.defaultFontAsset;

            LayoutElement layout = pill.AddComponent<LayoutElement>();
            layout.minHeight = 44f;
            layout.preferredHeight = 44f;
            return pill.GetComponent<Button>();
        }

        void ClearSpawned()
        {
            for (int i = spawned.Count - 1; i >= 0; i--)
            {
                if (spawned[i] != null)
                    Destroy(spawned[i]);
            }

            spawned.Clear();
        }
    }
}
