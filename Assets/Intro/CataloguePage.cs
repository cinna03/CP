using Capstone.App.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The catalogue opened from the menu. It lists the works stored in the app and opens one record at a time.
/// </summary>
public static class CataloguePage
{
    static readonly Color Ink = new Color32(42, 34, 37, 255);
    static readonly Color Stone = new Color32(125, 110, 115, 255);
    static readonly Color Card = new Color32(255, 255, 255, 255);
    static readonly Color Rose = new Color32(242, 220, 227, 255);
    static readonly Color Paper = new Color32(251, 249, 250, 255);

    public static void Fill(RectTransform root)
    {
        CatalogueShelf shelf = Resources.Load<CatalogueShelf>("CatalogueShelf");
        RectTransform scroll = Scroll(root);
        RectTransform content = scroll.Find("Viewport/Content") as RectTransform;
        if (shelf == null || shelf.Works == null || shelf.Works.Count == 0)
        {
            Note(content, "No works are stored in the catalogue.");
            return;
        }

        for (int i = 0; i < shelf.Works.Count; i++)
        {
            ArtworkData work = shelf.Works[i];
            if (work == null)
                continue;
            AddWork(content, root, work);
        }
    }

    static RectTransform Scroll(RectTransform root)
    {
        var host = new GameObject("Works", typeof(RectTransform), typeof(ScrollRect));
        host.transform.SetParent(root, false);
        var area = host.GetComponent<RectTransform>();
        area.anchorMin = Vector2.zero;
        area.anchorMax = Vector2.one;
        area.offsetMin = new Vector2(36f, 48f);
        area.offsetMax = new Vector2(-36f, -250f);

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
        viewport.transform.SetParent(host.transform, false);
        var viewRect = viewport.GetComponent<RectTransform>();
        viewRect.anchorMin = Vector2.zero;
        viewRect.anchorMax = Vector2.one;
        viewRect.offsetMin = viewRect.offsetMax = Vector2.zero;
        var mask = viewport.GetComponent<Image>();
        mask.color = new Color(1f, 1f, 1f, 0.01f);

        var contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(viewport.transform, false);
        var content = contentObject.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = content.offsetMax = Vector2.zero;

        var layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 18f;
        layout.padding = new RectOffset(4, 4, 4, 24);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fit = contentObject.GetComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = host.GetComponent<ScrollRect>();
        scroll.viewport = viewRect;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;
        return area;
    }

    static void AddWork(RectTransform content, RectTransform root, ArtworkData work)
    {
        var card = new GameObject(work.Identifier, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(Outline));
        card.transform.SetParent(content, false);
        var image = card.GetComponent<Image>();
        image.color = Card;
        var outline = card.GetComponent<Outline>();
        outline.effectColor = Rose;
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        var layout = card.GetComponent<LayoutElement>();
        layout.minHeight = 210f;
        layout.preferredHeight = 210f;

        var swatch = new GameObject("Swatch", typeof(RectTransform), typeof(Image));
        swatch.transform.SetParent(card.transform, false);
        var swatchRect = swatch.GetComponent<RectTransform>();
        swatchRect.anchorMin = swatchRect.anchorMax = new Vector2(0f, 0.5f);
        swatchRect.pivot = new Vector2(0f, 0.5f);
        swatchRect.sizeDelta = new Vector2(150f, 150f);
        swatchRect.anchoredPosition = new Vector2(24f, 0f);
        Image picture = swatch.GetComponent<Image>();
        picture.raycastTarget = false;
        Paint(picture, work);

        string artist = string.IsNullOrWhiteSpace(work.Artist) ? "" : work.Artist;
        string story = string.IsNullOrWhiteSpace(work.StoryNote) ? "No story note is stored for this artwork." : work.StoryNote;
        Text(card.transform, work.Title, 30f, Ink, FontStyles.Bold, new Vector2(196f, 52f), new Vector2(760f, 40f));
        Text(card.transform, work.FormatSizeLabel(0), 24f, Stone, FontStyles.Normal, new Vector2(196f, 12f), new Vector2(760f, 32f));
        Text(card.transform, artist, 22f, Stone, FontStyles.Normal, new Vector2(196f, -24f), new Vector2(760f, 32f));
        Text(card.transform, story, 22f, Ink, FontStyles.Normal, new Vector2(196f, -64f), new Vector2(760f, 40f));

        ArtworkData chosen = work;
        card.GetComponent<Button>().onClick.AddListener(() => Open(root, chosen));
    }

    static void Open(RectTransform root, ArtworkData work)
    {
        Transform existing = root.Find("Work detail");
        if (existing != null)
            Object.Destroy(existing.gameObject);

        var panel = new GameObject("Work detail", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(root, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = Paper;

        var back = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
        back.transform.SetParent(panel.transform, false);
        var backRect = back.GetComponent<RectTransform>();
        backRect.anchorMin = backRect.anchorMax = new Vector2(0f, 1f);
        backRect.pivot = new Vector2(0f, 1f);
        backRect.sizeDelta = new Vector2(180f, 72f);
        backRect.anchoredPosition = new Vector2(36f, -48f);
        back.GetComponent<Image>().color = Card;
        back.GetComponent<Button>().onClick.AddListener(() => Object.Destroy(panel));
        Text(back.transform, "Back", 32f, Ink, FontStyles.Bold, Vector2.zero, Vector2.zero, true, TextAlignmentOptions.Center);

        var swatch = new GameObject("Swatch", typeof(RectTransform), typeof(Image));
        swatch.transform.SetParent(panel.transform, false);
        var swatchRect = swatch.GetComponent<RectTransform>();
        swatchRect.anchorMin = swatchRect.anchorMax = new Vector2(0.5f, 1f);
        swatchRect.pivot = new Vector2(0.5f, 1f);
        swatchRect.sizeDelta = new Vector2(860f, 720f);
        swatchRect.anchoredPosition = new Vector2(0f, -160f);
        Image picture = swatch.GetComponent<Image>();
        picture.raycastTarget = false;
        Paint(picture, work);

        float y = -920f;
        y = Line(panel.transform, work.Title, 40f, Ink, FontStyles.Bold, y, 64f);
        if (!string.IsNullOrWhiteSpace(work.Artist))
            y = Line(panel.transform, work.Artist, 28f, Stone, FontStyles.Normal, y, 44f);
        if (work.Year != 0)
            y = Line(panel.transform, work.Year.ToString(), 28f, Stone, FontStyles.Normal, y, 44f);
        if (!string.IsNullOrWhiteSpace(work.Medium))
            y = Line(panel.transform, work.Medium, 28f, Stone, FontStyles.Normal, y, 44f);
        y = Line(panel.transform, work.FormatSizeLabel(0), 28f, Ink, FontStyles.Normal, y, 48f);
        y = Line(panel.transform, work.Colour, 28f, Ink, FontStyles.Normal, y, 44f);
        string story = string.IsNullOrWhiteSpace(work.StoryNote) ? "No story note is stored for this artwork." : work.StoryNote;
        Line(panel.transform, story, 28f, Ink, FontStyles.Normal, y, 160f);
    }

    static float Line(Transform parent, string value, float size, Color color, FontStyles style, float y, float height)
    {
        Text(parent, value, size, color, style, new Vector2(0f, y), new Vector2(900f, height), false, TextAlignmentOptions.TopLeft, new Vector2(0.5f, 1f));
        return y - height - 8f;
    }

    static void Note(RectTransform content, string value)
    {
        var row = new GameObject("Empty", typeof(RectTransform), typeof(LayoutElement));
        row.transform.SetParent(content, false);
        row.GetComponent<LayoutElement>().preferredHeight = 120f;
        Text(row.transform, value, 28f, Stone, FontStyles.Normal, Vector2.zero, new Vector2(0f, 120f), true);
    }

    static void Text(Transform parent, string value, float size, Color color, FontStyles style, Vector2 position, Vector2 delta, bool fill = false, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft, Vector2? anchor = null)
    {
        var label = new GameObject("Label", typeof(RectTransform));
        label.transform.SetParent(parent, false);
        var rect = label.GetComponent<RectTransform>();
        if (fill)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        else
        {
            Vector2 point = anchor ?? new Vector2(0f, 0.5f);
            rect.anchorMin = rect.anchorMax = point;
            rect.pivot = new Vector2(point.x, point.y == 1f ? 1f : 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = delta;
        }

        var text = label.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;
        text.text = value ?? "";
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
    }

    static void Paint(Image image, ArtworkData work)
    {
        if (work.ArtworkSprite != null)
        {
            image.sprite = work.ArtworkSprite;
            image.preserveAspect = true;
            image.color = Color.white;
            return;
        }

        image.sprite = null;
        image.color = Swatch(work.Colour);
    }

    static Color Swatch(string colour)
    {
        switch ((colour ?? "").Trim().ToLowerInvariant())
        {
            case "blue":
                return new Color32(186, 206, 220, 255);
            case "red":
                return new Color32(214, 154, 150, 255);
            case "green":
                return new Color32(184, 204, 176, 255);
            case "pink":
                return new Color32(233, 168, 188, 255);
            default:
                return Rose;
        }
    }
}
