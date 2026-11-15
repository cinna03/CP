using System;
using System.Collections.Generic;
using Capstone.App.Data;
using Capstone.App.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Capstone.App.UI
{
    /// <summary>
    /// The saved try-on photos. It reads the snapshot service and asks the menu to reopen the wall.
    /// </summary>
    public sealed class GalleryViewController : MonoBehaviour
    {
        static readonly Color Card = new Color32(255, 255, 255, 255);
        static readonly Color Accent = new Color32(233, 168, 188, 255);
        static readonly Color Blush = new Color32(249, 235, 240, 255);
        static readonly Color Ink = new Color32(42, 34, 37, 255);
        static readonly Color Stone = new Color32(125, 110, 115, 255);

        readonly IWallSnapshotService photos = new WallSnapshotService();
        readonly List<Texture2D> loaded = new List<Texture2D>();
        Action openWall;
        RectTransform list;
        Sprite rounded;

        public static void Fill(RectTransform page, Action openWall)
        {
            if (page == null)
                return;

            GalleryViewController view = page.GetComponent<GalleryViewController>();
            if (view == null)
                view = page.gameObject.AddComponent<GalleryViewController>();
            view.openWall = openWall;
            view.Refresh();
        }

        void OnDestroy()
        {
            for (int i = 0; i < loaded.Count; i++)
            {
                if (loaded[i] != null)
                    Destroy(loaded[i]);
            }
        }

        void Refresh()
        {
            EnsureList();
            for (int i = list.childCount - 1; i >= 0; i--)
                Destroy(list.GetChild(i).gameObject);
            for (int i = 0; i < loaded.Count; i++)
            {
                if (loaded[i] != null)
                    Destroy(loaded[i]);
            }
            loaded.Clear();

            IReadOnlyList<WallSnapshotRecord> records = photos.GetAllSnapshots();
            if (records == null || records.Count == 0)
            {
                CardMessage("No wall photos saved yet. Tap the camera button during a wall visit to capture how an artwork looks in your space.");
                return;
            }

            for (int i = 0; i < records.Count; i++)
            {
                if (records[i] != null)
                    AddCard(records[i]);
            }
        }

        void EnsureList()
        {
            if (list != null)
                return;

            var viewport = new GameObject("Gallery scroll", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            viewport.transform.SetParent(transform, false);
            var viewRect = viewport.GetComponent<RectTransform>();
            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.offsetMin = new Vector2(36f, 36f);
            viewRect.offsetMax = new Vector2(-36f, -240f);
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);

            var content = new GameObject("Photos", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            list = content.GetComponent<RectTransform>();
            list.anchorMin = new Vector2(0f, 1f);
            list.anchorMax = new Vector2(1f, 1f);
            list.pivot = new Vector2(0.5f, 1f);
            var stack = content.GetComponent<VerticalLayoutGroup>();
            stack.spacing = 20f;
            stack.padding = new RectOffset(8, 8, 8, 24);
            stack.childControlWidth = true;
            stack.childControlHeight = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = viewport.GetComponent<ScrollRect>();
            scroll.viewport = viewRect;
            scroll.content = list;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
        }

        void AddCard(WallSnapshotRecord record)
        {
            Texture2D texture = photos.LoadSnapshotTexture(record);
            if (texture != null)
                loaded.Add(texture);

            var card = new GameObject(record.ArtworkTitle, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            card.transform.SetParent(list, false);
            Image plate = card.GetComponent<Image>();
            plate.sprite = Rounded();
            plate.type = Image.Type.Sliced;
            plate.color = Card;
            float photoHeight = PhotoHeight(texture);
            card.GetComponent<LayoutElement>().preferredHeight = photoHeight + 132f;
            WallSnapshotRecord chosen = record;
            card.GetComponent<Button>().onClick.AddListener(delegate { OpenPreview(chosen); });

            var picture = new GameObject("Photo", typeof(RectTransform), typeof(Image));
            picture.transform.SetParent(card.transform, false);
            var pictureRect = picture.GetComponent<RectTransform>();
            pictureRect.anchorMin = new Vector2(0f, 1f);
            pictureRect.anchorMax = new Vector2(1f, 1f);
            pictureRect.pivot = new Vector2(0.5f, 1f);
            pictureRect.sizeDelta = new Vector2(-32f, photoHeight);
            pictureRect.anchoredPosition = new Vector2(0f, -16f);
            Image sprite = picture.GetComponent<Image>();
            sprite.raycastTarget = false;
            sprite.preserveAspect = true;
            if (texture != null)
            {
                sprite.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                sprite.color = Color.white;
            }
            else
            {
                sprite.color = Blush;
            }

            string frame = string.IsNullOrWhiteSpace(record.FrameName) ? "Unframed" : record.FrameName;
            string size = string.IsNullOrWhiteSpace(record.DimensionsLabel) ? "Size not stored" : record.DimensionsLabel;
            TMP_Text title = Text(card.transform, string.IsNullOrWhiteSpace(record.ArtworkTitle) ? "Untitled" : record.ArtworkTitle, 28f, Ink, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            title.rectTransform.anchorMin = new Vector2(0f, 0f);
            title.rectTransform.anchorMax = new Vector2(1f, 0f);
            title.rectTransform.pivot = new Vector2(0.5f, 0f);
            title.rectTransform.sizeDelta = new Vector2(-40f, 40f);
            title.rectTransform.anchoredPosition = new Vector2(0f, 56f);
            TMP_Text detail = Text(card.transform, size + " · " + frame, 22f, Stone, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            detail.rectTransform.anchorMin = new Vector2(0f, 0f);
            detail.rectTransform.anchorMax = new Vector2(1f, 0f);
            detail.rectTransform.pivot = new Vector2(0.5f, 0f);
            detail.rectTransform.sizeDelta = new Vector2(-40f, 32f);
            detail.rectTransform.anchoredPosition = new Vector2(0f, 20f);
        }

        void CardMessage(string message)
        {
            var card = new GameObject("Empty", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            card.transform.SetParent(list, false);
            Image plate = card.GetComponent<Image>();
            plate.sprite = Rounded();
            plate.type = Image.Type.Sliced;
            plate.color = Card;
            card.GetComponent<LayoutElement>().preferredHeight = 280f;
            TMP_Text body = Text(card.transform, message, 26f, Ink, FontStyles.Normal, TextAlignmentOptions.Center);
            body.rectTransform.offsetMin = new Vector2(36f, 28f);
            body.rectTransform.offsetMax = new Vector2(-36f, -28f);
            body.enableWordWrapping = true;
        }

        void OpenPreview(WallSnapshotRecord record)
        {
            var modal = new GameObject("Preview", typeof(RectTransform), typeof(Image));
            modal.transform.SetParent(transform, false);
            var rect = modal.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            modal.GetComponent<Image>().color = new Color32(251, 249, 250, 255);
            modal.transform.SetAsLastSibling();

            Texture2D texture = photos.LoadSnapshotTexture(record);
            if (texture != null)
                loaded.Add(texture);

            var picture = new GameObject("Photo", typeof(RectTransform), typeof(Image));
            picture.transform.SetParent(modal.transform, false);
            var pictureRect = picture.GetComponent<RectTransform>();
            pictureRect.anchorMin = new Vector2(0.5f, 0.5f);
            pictureRect.anchorMax = new Vector2(0.5f, 0.5f);
            pictureRect.sizeDelta = new Vector2(960f, 1100f);
            pictureRect.anchoredPosition = new Vector2(0f, 80f);
            Image sprite = picture.GetComponent<Image>();
            sprite.preserveAspect = true;
            sprite.raycastTarget = false;
            if (texture != null)
                sprite.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            else
                sprite.color = Blush;

            string title = string.IsNullOrWhiteSpace(record.ArtworkTitle) ? "Untitled" : record.ArtworkTitle;
            TMP_Text heading = Text(modal.transform, title, 32f, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
            heading.rectTransform.anchorMin = heading.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            heading.rectTransform.sizeDelta = new Vector2(920f, 48f);
            heading.rectTransform.anchoredPosition = new Vector2(0f, 280f);

            Button again = Button(modal.transform, "Try Again on Wall", new Vector2(0f, 188f), Accent);
            Button remove = Button(modal.transform, "Delete", new Vector2(0f, 108f), Blush);
            again.onClick.AddListener(delegate { TryAgain(record, modal); });
            remove.onClick.AddListener(delegate
            {
                photos.DeleteSnapshot(record.SnapshotId);
                Destroy(modal);
                Refresh();
            });
        }

        void TryAgain(WallSnapshotRecord record, GameObject modal)
        {
            CatalogueShelf shelf = Resources.Load<CatalogueShelf>("CatalogueShelf");
            ArtworkData work = FindWork(shelf, record.ArtworkIdentifier);
            if (work == null || shelf == null || shelf.Session == null)
                return;

            shelf.Session.Select(work, record.SizeIndex, FindFrame(shelf.Frames, record.FrameName));
            if (modal != null)
                Destroy(modal);
            if (openWall != null)
                openWall();
        }

        static ArtworkData FindWork(CatalogueShelf shelf, string identifier)
        {
            if (shelf == null || shelf.Works == null || string.IsNullOrEmpty(identifier))
                return null;

            for (int i = 0; i < shelf.Works.Count; i++)
            {
                ArtworkData work = shelf.Works[i];
                if (work != null && work.Identifier == identifier)
                    return work;
            }

            return null;
        }

        static FrameOption FindFrame(IReadOnlyList<FrameOption> frames, string frameName)
        {
            if (frames == null || string.IsNullOrWhiteSpace(frameName) || frameName == "Unframed")
                return null;

            for (int i = 0; i < frames.Count; i++)
            {
                FrameOption frame = frames[i];
                if (frame != null && string.Equals(frame.FrameName, frameName, StringComparison.OrdinalIgnoreCase))
                    return frame;
            }

            return null;
        }

        static float PhotoHeight(Texture2D texture)
        {
            if (texture == null || texture.width <= 0)
                return 360f;
            float height = 960f * texture.height / texture.width;
            return Mathf.Clamp(height, 280f, 640f);
        }

        Button Button(Transform parent, string caption, Vector2 position, Color fill)
        {
            var host = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button));
            host.transform.SetParent(parent, false);
            var rect = host.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(860f, 68f);
            rect.anchoredPosition = position;
            Image image = host.GetComponent<Image>();
            image.sprite = Rounded();
            image.type = Image.Type.Sliced;
            image.color = fill;
            Text(host.transform, caption, 26f, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
            Button button = host.GetComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        TMP_Text Text(Transform parent, string caption, float size, Color color, FontStyles style, TextAlignmentOptions alignment)
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
            text.enableWordWrapping = true;
            return text;
        }

        Sprite Rounded()
        {
            if (rounded != null)
                return rounded;

            const int edge = 96;
            const float radius = 28f;
            var texture = new Texture2D(edge, edge, TextureFormat.RGBA32, false);
            var pixels = new Color32[edge * edge];
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
            rounded = Sprite.Create(texture, new Rect(0f, 0f, edge, edge), new Vector2(0.5f, 0.5f), edge, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            return rounded;
        }
    }
}
