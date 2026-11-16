using System;
using Capstone.App.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Capstone.App.UI
{
    /// <summary>
    /// One "similar work" card in the wall tray. The card is built once and then rebound to a
    /// different artwork each time the ranking changes, so the tray reuses three cards for the
    /// whole visit instead of destroying and rebuilding them after every decision.
    /// </summary>
    public sealed class RecommendationCardView : MonoBehaviour
    {
        Image picture;
        TMP_Text title;
        TMP_Text size;
        ArtworkData work;
        Action<ArtworkData> details;
        Action<ArtworkData> tryOnWall;

        /// <summary>The artwork this card currently shows.</summary>
        public ArtworkData Work => work;

        /// <summary>Called once by the tray when the card is first built.</summary>
        public void Wire(Image pictureImage, TMP_Text titleText, TMP_Text sizeText)
        {
            picture = pictureImage;
            title = titleText;
            size = sizeText;
        }

        public void Bind(ArtworkData artwork, Action<ArtworkData> onDetails, Action<ArtworkData> onTryOnWall)
        {
            work = artwork;
            details = onDetails;
            tryOnWall = onTryOnWall;
            gameObject.name = artwork != null ? artwork.Title : "Card";
            if (title != null)
                title.text = artwork != null ? artwork.Title : "";
            if (size != null)
                size.text = artwork != null ? artwork.FormatSizeLabel(0) : "";
            if (picture != null)
            {
                Sprite sprite = artwork != null ? artwork.ArtworkSprite : null;
                picture.sprite = sprite;
                picture.enabled = sprite != null;
            }
        }

        public void ShowDetails()
        {
            if (work != null)
                details?.Invoke(work);
        }

        public void TryOnWall()
        {
            if (work != null)
                tryOnWall?.Invoke(work);
        }
    }
}
