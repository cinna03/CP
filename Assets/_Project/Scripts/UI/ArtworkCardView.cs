using System;
using Capstone.App.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Capstone.App.UI
{
    /// <summary>
    /// One catalogue card. The picture keeps its own proportions. The card reports a tap and a shortlist change upward.
    /// </summary>
    public sealed class ArtworkCardView : MonoBehaviour
    {
        [SerializeField] Image artworkImage;
        [SerializeField] AspectRatioFitter aspectRatioFitter;
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] TMP_Text dimensionsLabel;
        [SerializeField] Button openButton;
        [SerializeField] Button shortlistButton;
        [SerializeField] TMP_Text shortlistLabel;
        [SerializeField] Image shortlistImage;
        [SerializeField] UIThemeData theme;
        [SerializeField] ArtworkEvent opened = new ArtworkEvent();

        ArtworkData artwork;
        bool shortlisted;
        Action onOpen;
        Action onToggleShortlist;

        public ArtworkData Artwork => artwork;
        public UnityEvent<ArtworkData> Opened => opened;

        void Awake()
        {
            if (openButton != null)
                openButton.onClick.AddListener(Open);
            if (shortlistButton != null)
                shortlistButton.onClick.AddListener(ToggleShortlist);
        }

        void OnDestroy()
        {
            if (openButton != null)
                openButton.onClick.RemoveListener(Open);
            if (shortlistButton != null)
                shortlistButton.onClick.RemoveListener(ToggleShortlist);
        }

        public void Bind(ArtworkData next, bool saved, Action open, Action toggleShortlist)
        {
            artwork = next;
            shortlisted = saved;
            onOpen = open;
            onToggleShortlist = toggleShortlist;
            Refresh();
        }

        public void Open()
        {
            if (artwork == null)
                return;

            onOpen?.Invoke();
            opened.Invoke(artwork);
        }

        public void ToggleShortlist()
        {
            if (artwork == null)
                return;

            shortlisted = !shortlisted;
            RefreshShortlist();
            onToggleShortlist?.Invoke();
        }

        void Refresh()
        {
            if (artwork == null)
                return;

            if (titleLabel != null)
                titleLabel.text = artwork.Title;
            if (dimensionsLabel != null)
                dimensionsLabel.text = artwork.FormatSizeLabel(0);
            if (artworkImage != null)
            {
                artworkImage.sprite = artwork.ArtworkSprite;
                artworkImage.preserveAspect = true;
                artworkImage.color = Color.white;
                artworkImage.enabled = artwork.ArtworkSprite != null;
            }

            if (aspectRatioFitter != null)
            {
                aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                aspectRatioFitter.aspectRatio = Aspect(artwork);
            }

            RefreshShortlist();
        }

        void RefreshShortlist()
        {
            if (shortlistLabel != null)
                shortlistLabel.text = shortlisted ? "Saved" : "Save";
            if (shortlistImage != null && theme != null)
                shortlistImage.color = shortlisted ? theme.PrimaryAccent : theme.SecondaryAccent;
        }

        static float Aspect(ArtworkData work)
        {
            if (work.ArtworkSprite != null)
            {
                Rect rect = work.ArtworkSprite.rect;
                if (rect.height > 0.01f)
                    return rect.width / rect.height;
            }

            if (work.TryGetSizeCentimetres(0, out Vector2 size) && size.y > 0f)
                return size.x / size.y;

            return 1f;
        }
    }

    [Serializable]
    public sealed class ArtworkEvent : UnityEvent<ArtworkData>
    {
    }
}
