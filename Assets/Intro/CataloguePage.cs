using System.Collections.Generic;
using Capstone.App.Data;
using Capstone.App.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The catalogue opened from the menu. It reads the stored shelf, and the filter and the session live in the services.
/// </summary>
public static class CataloguePage
{
    static readonly CatalogueFilterService Filter = new CatalogueFilterService();
    static readonly ArtworkQueryService Replies = new ArtworkQueryService();

    static CatalogueShelf shelf;
    static UIThemeData theme;
    static ArtworkSessionState session;
    static RectTransform page;
    static RectTransform listContent;
    static string style = "";
    static string mood = "";
    static string colour = "";
    static string search = "";
    static string orientation = "";
    static string sizeTier = "";
    static bool favorites;
    static TMP_InputField searchField;
    static GameObject clearSearch;
    static Sprite rounded;
    static readonly List<FilterPill> pills = new List<FilterPill>();

    public static void Fill(RectTransform root)
    {
        OpenPage(root, false);
    }

    public static void FillFavorites(RectTransform root)
    {
        OpenPage(root, true);
    }

    static void OpenPage(RectTransform root, bool savedOnly)
    {
        shelf = Resources.Load<CatalogueShelf>("CatalogueShelf");
        theme = shelf != null ? shelf.Theme : null;
        session = shelf != null ? shelf.Session : null;
        if (session != null && shelf != null)
            session.RestoreSaved(shelf.Works);
        page = root;
        favorites = savedOnly;
        style = "";
        mood = "";
        colour = "";
        search = "";
        orientation = "";
        sizeTier = "";
        pills.Clear();

        Image paper = root.GetComponent<Image>();
        if (paper != null)
            paper.color = CanvasColor;

        TintHeading(root);
        BuildSearch(root);
        BuildFilters(root);
        listContent = Scroll(root);
        if (!favorites && (shelf == null || shelf.Works == null || shelf.Works.Count == 0))
        {
            Note(listContent, "No works are stored in the catalogue.");
            return;
        }

        ShowWorks();
    }

    static void ShowWorks()
    {
        for (int i = listContent.childCount - 1; i >= 0; i--)
            Object.Destroy(listContent.GetChild(i).gameObject);

        IReadOnlyList<ArtworkData> shown;
        bool pin = false;
        string arrival = "";
        if (!favorites && ArtFinderArrival.Pending)
        {
            pin = ArtFinderArrival.PinMatches;
            arrival = ArtFinderArrival.Message;
            ArtFinderArrival.Pending = false;
        }

        if (pin && session != null)
            shown = Filter.Suggested(shelf.Works, session.Taste);
        else
        {
            CatalogueQuery query = new CatalogueQuery
            {
                Style = style,
                Mood = mood,
                Colour = colour,
                Search = search,
                Orientation = WantedOrientation(),
                SizeTier = WantedSize()
            };
            IReadOnlyList<ArtworkData> source = favorites
                ? (session != null ? session.Shortlist : new List<ArtworkData>())
                : shelf.Works;
            shown = Filter.Matching(source, query);
        }

        if (arrival.Length > 0)
            Arrival(listContent, arrival);
        if (shown.Count == 0)
        {
            bool untouched = string.IsNullOrWhiteSpace(search) && style == "" && mood == "" && colour == "" && orientation == "" && sizeTier == "";
            if (favorites && untouched)
                Note(listContent, "No saved works yet. Tap the heart on a work to keep it here.");
            else
                EmptyMatch(listContent);
            PaintPills();
            return;
        }

        for (int i = 0; i < shown.Count; i++)
        {
            if (shown[i] != null)
                AddWork(listContent, shown[i]);
        }

        PaintPills();
    }

    static void BuildSearch(RectTransform root)
    {
        var bar = new GameObject("Search", typeof(RectTransform), typeof(Image));
        bar.transform.SetParent(root, false);
        var barRect = bar.GetComponent<RectTransform>();
        barRect.anchorMin = barRect.anchorMax = new Vector2(0.5f, 1f);
        barRect.pivot = new Vector2(0.5f, 1f);
        barRect.sizeDelta = new Vector2(1008f, 76f);
        barRect.anchoredPosition = new Vector2(0f, -232f);
        Image surface = bar.GetComponent<Image>();
        surface.sprite = RoundedSprite();
        surface.type = Image.Type.Sliced;
        surface.color = Border;

        var inner = new GameObject("Surface", typeof(RectTransform), typeof(Image));
        inner.transform.SetParent(bar.transform, false);
        var innerRect = inner.GetComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(2f, 2f);
        innerRect.offsetMax = new Vector2(-2f, -2f);
        Image innerImage = inner.GetComponent<Image>();
        innerImage.sprite = RoundedSprite();
        innerImage.type = Image.Type.Sliced;
        innerImage.color = Card;
        innerImage.raycastTarget = false;

        var fieldObject = new GameObject("Input", typeof(RectTransform), typeof(TMP_InputField));
        fieldObject.transform.SetParent(bar.transform, false);
        var fieldRect = fieldObject.GetComponent<RectTransform>();
        fieldRect.anchorMin = Vector2.zero;
        fieldRect.anchorMax = Vector2.one;
        fieldRect.offsetMin = new Vector2(24f, 8f);
        fieldRect.offsetMax = new Vector2(-88f, -8f);

        var textObject = new GameObject("Text", typeof(RectTransform));
        textObject.transform.SetParent(fieldObject.transform, false);
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        TMP_Text value = textObject.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            value.font = TMP_Settings.defaultFontAsset;
        value.fontSize = 26f;
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
        placeholder.text = "Search by mood, colour, style, or artist...";
        placeholder.fontSize = 24f;
        placeholder.fontStyle = FontStyles.Italic;
        placeholder.color = Stone;
        placeholder.alignment = TextAlignmentOptions.MidlineLeft;

        searchField = fieldObject.GetComponent<TMP_InputField>();
        searchField.textViewport = fieldRect;
        searchField.textComponent = value;
        searchField.placeholder = placeholder;
        searchField.fontAsset = value.font;
        searchField.pointSize = 26f;
        searchField.lineType = TMP_InputField.LineType.SingleLine;
        searchField.onValueChanged.AddListener(OnSearch);

        var clearObject = new GameObject("Clear", typeof(RectTransform), typeof(Image), typeof(Button));
        clearObject.transform.SetParent(bar.transform, false);
        var clearRect = clearObject.GetComponent<RectTransform>();
        clearRect.anchorMin = clearRect.anchorMax = new Vector2(1f, 0.5f);
        clearRect.pivot = new Vector2(1f, 0.5f);
        clearRect.sizeDelta = new Vector2(64f, 64f);
        clearRect.anchoredPosition = new Vector2(-8f, 0f);
        Image clearImage = clearObject.GetComponent<Image>();
        clearImage.sprite = RoundedSprite();
        clearImage.type = Image.Type.Sliced;
        clearImage.color = Blush;
        Text(clearObject.transform, "X", 26f, Ink, FontStyles.Bold, Vector2.zero, Vector2.zero, true, TextAlignmentOptions.Center);
        clearObject.GetComponent<Button>().onClick.AddListener(ResetSearch);
        clearObject.SetActive(false);
        clearSearch = clearObject;
    }

    static void OnSearch(string value)
    {
        search = value ?? "";
        if (clearSearch != null)
            clearSearch.SetActive(!string.IsNullOrEmpty(search));
        if (listContent != null)
            ShowWorks();
    }

    static void ResetSearch()
    {
        style = "";
        mood = "";
        colour = "";
        orientation = "";
        sizeTier = "";
        search = "";
        if (searchField != null)
            searchField.SetTextWithoutNotify("");
        if (clearSearch != null)
            clearSearch.SetActive(false);
        ShowWorks();
    }

    static void EmptyMatch(RectTransform content)
    {
        Note(content, "No artworks found matching your search.");
        var button = new GameObject("Reset", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        button.transform.SetParent(content, false);
        Image image = button.GetComponent<Image>();
        image.sprite = RoundedSprite();
        image.type = Image.Type.Sliced;
        image.color = Accent;
        var element = button.GetComponent<LayoutElement>();
        element.preferredHeight = 72f;
        element.preferredWidth = 460f;
        Text(button.transform, "Reset Search & Filters", 24f, Ink, FontStyles.Bold, Vector2.zero, Vector2.zero, true, TextAlignmentOptions.Center);
        button.GetComponent<Button>().onClick.AddListener(ResetSearch);
    }

    static void BuildFilters(RectTransform root)
    {
        var host = new GameObject("Filters", typeof(RectTransform), typeof(Image));
        host.transform.SetParent(root, false);
        var area = host.GetComponent<RectTransform>();
        area.anchorMin = new Vector2(0f, 1f);
        area.anchorMax = new Vector2(1f, 1f);
        area.pivot = new Vector2(0.5f, 1f);
        area.sizeDelta = new Vector2(-48f, 168f);
        area.anchoredPosition = new Vector2(0f, -324f);
        host.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);

        RectTransform tags = FilterRow(host.transform, 0f);
        RectTransform shape = FilterRow(host.transform, -80f);

        AddPill(tags, "All", "", "");
        AddDistinct(tags, "style");
        AddDistinct(tags, "mood");
        AddDistinct(tags, "colour");
        AddPill(shape, "Portrait", "orientation", "Portrait");
        AddPill(shape, "Landscape", "orientation", "Landscape");
        AddPill(shape, "Square", "orientation", "Square");
        AddPill(shape, "Small", "size", "Small");
        AddPill(shape, "Medium", "size", "Medium");
        AddPill(shape, "Large", "size", "Large");
    }

    static RectTransform FilterRow(Transform parent, float y)
    {
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
        viewport.transform.SetParent(parent, false);
        var viewRect = viewport.GetComponent<RectTransform>();
        viewRect.anchorMin = new Vector2(0f, 1f);
        viewRect.anchorMax = new Vector2(1f, 1f);
        viewRect.pivot = new Vector2(0.5f, 1f);
        viewRect.sizeDelta = new Vector2(0f, 80f);
        viewRect.anchoredPosition = new Vector2(0f, y);
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);

        var contentObject = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(viewport.transform, false);
        var content = contentObject.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 0f);
        content.anchorMax = new Vector2(0f, 1f);
        content.pivot = new Vector2(0f, 0.5f);
        content.offsetMin = content.offsetMax = Vector2.zero;
        var layout = contentObject.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.padding = new RectOffset(8, 24, 8, 8);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        var fit = contentObject.GetComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        var scrollObject = viewport.AddComponent<ScrollRect>();
        scrollObject.viewport = viewRect;
        scrollObject.content = content;
        scrollObject.horizontal = true;
        scrollObject.vertical = false;
        scrollObject.movementType = ScrollRect.MovementType.Clamped;
        return content;
    }

    static void AddDistinct(RectTransform content, string kind)
    {
        var seen = new List<string>();
        IReadOnlyList<ArtworkData> works = shelf != null ? shelf.Works : null;
        if (works == null)
            return;

        for (int i = 0; i < works.Count; i++)
        {
            ArtworkData work = works[i];
            if (work == null)
                continue;
            string value = kind == "style" ? work.Style : kind == "mood" ? work.Mood : work.Colour;
            if (string.IsNullOrWhiteSpace(value) || Contains(seen, value))
                continue;
            seen.Add(value.Trim());
            AddPill(content, value.Trim(), kind, value.Trim());
        }
    }

    static void AddPill(RectTransform content, string label, string kind, string value)
    {
        var pill = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        pill.transform.SetParent(content, false);
        Image image = pill.GetComponent<Image>();
        image.sprite = RoundedSprite();
        image.type = Image.Type.Sliced;
        var element = pill.GetComponent<LayoutElement>();
        element.minWidth = 120f;
        element.preferredWidth = Mathf.Max(120f, 48f + label.Length * 18f);
        element.preferredHeight = 64f;
        TMP_Text text = Text(pill.transform, label, 24f, Ink, FontStyles.Normal, Vector2.zero, Vector2.zero, true, TextAlignmentOptions.Center);
        string capturedKind = kind;
        string capturedValue = value;
        pill.GetComponent<Button>().onClick.AddListener(() => ChooseFilter(capturedKind, capturedValue));
        pills.Add(new FilterPill { Image = image, Label = text, Kind = kind, Value = value });
    }

    static void ChooseFilter(string kind, string value)
    {
        if (kind == "")
        {
            style = "";
            mood = "";
            colour = "";
            orientation = "";
            sizeTier = "";
        }
        else if (kind == "style")
            style = style == value ? "" : value;
        else if (kind == "mood")
            mood = mood == value ? "" : value;
        else if (kind == "colour")
            colour = colour == value ? "" : value;
        else if (kind == "orientation")
            orientation = orientation == value ? "" : value;
        else if (kind == "size")
            sizeTier = sizeTier == value ? "" : value;

        ShowWorks();
    }

    static void PaintPills()
    {
        for (int i = 0; i < pills.Count; i++)
        {
            FilterPill pill = pills[i];
            bool active = pill.Kind == ""
                ? style == "" && mood == "" && colour == "" && orientation == "" && sizeTier == ""
                : (pill.Kind == "style" && style == pill.Value)
                    || (pill.Kind == "mood" && mood == pill.Value)
                    || (pill.Kind == "colour" && colour == pill.Value)
                    || (pill.Kind == "orientation" && orientation == pill.Value)
                    || (pill.Kind == "size" && sizeTier == pill.Value);
            if (pill.Image != null)
                pill.Image.color = active ? Accent : Blush;
            if (pill.Label != null)
                pill.Label.color = Ink;
        }
    }

    static Orientation? WantedOrientation()
    {
        if (orientation == "Portrait")
            return Orientation.Portrait;
        if (orientation == "Landscape")
            return Orientation.Landscape;
        if (orientation == "Square")
            return Orientation.Square;
        return null;
    }

    static SizeTier? WantedSize()
    {
        if (sizeTier == "Small")
            return SizeTier.Small;
        if (sizeTier == "Medium")
            return SizeTier.Medium;
        if (sizeTier == "Large")
            return SizeTier.Large;
        return null;
    }

    static RectTransform Scroll(RectTransform root)
    {
        var host = new GameObject("Works", typeof(RectTransform), typeof(ScrollRect));
        host.transform.SetParent(root, false);
        var area = host.GetComponent<RectTransform>();
        area.anchorMin = Vector2.zero;
        area.anchorMax = Vector2.one;
        area.offsetMin = new Vector2(28f, 36f);
        area.offsetMax = new Vector2(-28f, -504f);

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
        viewport.transform.SetParent(host.transform, false);
        var viewRect = viewport.GetComponent<RectTransform>();
        viewRect.anchorMin = Vector2.zero;
        viewRect.anchorMax = Vector2.one;
        viewRect.offsetMin = viewRect.offsetMax = Vector2.zero;
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);

        var contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(viewport.transform, false);
        var content = contentObject.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = content.offsetMax = Vector2.zero;

        var layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 16f;
        layout.padding = new RectOffset(8, 8, 8, 28);
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
        return content;
    }

    static void AddWork(RectTransform content, ArtworkData work)
    {
        var card = new GameObject(work.Identifier, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        card.transform.SetParent(content, false);
        Image image = card.GetComponent<Image>();
        image.sprite = RoundedSprite();
        image.type = Image.Type.Sliced;
        image.color = Card;
        var layout = card.GetComponent<LayoutElement>();
        layout.minHeight = 188f;
        layout.preferredHeight = 188f;

        var swatch = new GameObject("Swatch", typeof(RectTransform), typeof(Image));
        swatch.transform.SetParent(card.transform, false);
        var swatchRect = swatch.GetComponent<RectTransform>();
        swatchRect.anchorMin = swatchRect.anchorMax = new Vector2(0f, 0.5f);
        swatchRect.pivot = new Vector2(0f, 0.5f);
        swatchRect.sizeDelta = new Vector2(140f, 140f);
        swatchRect.anchoredPosition = new Vector2(24f, 0f);
        Image picture = swatch.GetComponent<Image>();
        picture.raycastTarget = false;
        Paint(picture, work);

        Text(card.transform, work.Title, 30f, Ink, FontStyles.Bold, new Vector2(184f, 36f), new Vector2(620f, 40f));
        Text(card.transform, work.FormatSizeLabel(0), 24f, Stone, FontStyles.Normal, new Vector2(184f, -8f), new Vector2(620f, 32f));
        if (!string.IsNullOrWhiteSpace(work.Artist))
            Text(card.transform, work.Artist, 22f, Stone, FontStyles.Normal, new Vector2(184f, -44f), new Vector2(620f, 32f));

        bool saved = session != null && session.IsShortlisted(work);
        var heartObject = new GameObject("Save", typeof(RectTransform), typeof(Image), typeof(Button));
        heartObject.transform.SetParent(card.transform, false);
        var heartRect = heartObject.GetComponent<RectTransform>();
        heartRect.anchorMin = heartRect.anchorMax = new Vector2(1f, 0.5f);
        heartRect.pivot = new Vector2(1f, 0.5f);
        heartRect.sizeDelta = new Vector2(84f, 84f);
        heartRect.anchoredPosition = new Vector2(-16f, 0f);
        Image heartImage = heartObject.GetComponent<Image>();
        heartImage.sprite = RoundedSprite();
        heartImage.type = Image.Type.Sliced;
        heartImage.color = saved ? Accent : Blush;
        TMP_Text heart = Text(heartObject.transform, saved ? "♥" : "♡", 42f, saved ? Ink : Stone, FontStyles.Normal, Vector2.zero, Vector2.zero, true, TextAlignmentOptions.Center);
        heart.raycastTarget = false;
        ArtworkData chosen = work;
        heartObject.GetComponent<Button>().onClick.AddListener(() => ToggleSave(chosen, heart, heartImage));
        card.GetComponent<Button>().onClick.AddListener(() => Open(chosen));
    }

    static void ToggleSave(ArtworkData work, TMP_Text heart, Image heartImage)
    {
        if (session == null || work == null)
            return;

        bool include = !session.IsShortlisted(work);
        session.SetShortlisted(work, include);
        heart.text = include ? "♥" : "♡";
        heart.color = include ? Ink : Stone;
        if (heartImage != null)
            heartImage.color = include ? Accent : Blush;
        Notice(include ? work.Title + " saved" : work.Title + " removed");
        if (favorites)
            ShowWorks();
    }

    static void Open(ArtworkData work)
    {
        Transform existing = page.Find("Work detail");
        if (existing != null)
            Object.Destroy(existing.gameObject);

        int sizeIndex = 0;
        FrameOption frame = FirstFrame();
        if (session != null && session.SelectedArtwork == work)
        {
            sizeIndex = session.SizeIndex;
            if (session.SelectedFrame != null)
                frame = session.SelectedFrame;
        }

        var panel = new GameObject("Work detail", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(page, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = CanvasColor;

        Button back = CornerButton(panel.transform, "Back", new Vector2(36f, -48f), () =>
        {
            Object.Destroy(panel);
            ShowWorks();
        });

        var border = new GameObject("Frame", typeof(RectTransform), typeof(Image));
        border.transform.SetParent(panel.transform, false);
        var borderRect = border.GetComponent<RectTransform>();
        borderRect.anchorMin = borderRect.anchorMax = new Vector2(0.5f, 1f);
        borderRect.pivot = new Vector2(0.5f, 1f);
        borderRect.sizeDelta = new Vector2(900f, 760f);
        borderRect.anchoredPosition = new Vector2(0f, -150f);
        Image borderImage = border.GetComponent<Image>();
        borderImage.raycastTarget = false;

        var swatch = new GameObject("Preview", typeof(RectTransform), typeof(Image));
        swatch.transform.SetParent(border.transform, false);
        var swatchRect = swatch.GetComponent<RectTransform>();
        swatchRect.anchorMin = Vector2.zero;
        swatchRect.anchorMax = Vector2.one;
        swatchRect.offsetMin = new Vector2(18f, 18f);
        swatchRect.offsetMax = new Vector2(-18f, -18f);
        Image picture = swatch.GetComponent<Image>();
        picture.raycastTarget = false;
        Paint(picture, work);

        TMP_Text sizeLabel = Text(panel.transform, "", 28f, Ink, FontStyles.Normal, new Vector2(0f, -930f), new Vector2(900f, 40f), false, TextAlignmentOptions.Center, new Vector2(0.5f, 1f));
        RectTransform sizeRow = PillRow(panel.transform, -980f);
        RectTransform frameRow = PillRow(panel.transform, -1070f);
        var sizeButtons = new List<ChoiceButton>();
        var frameButtons = new List<ChoiceButton>();
        IReadOnlyList<Vector2> sizes = work.AvailableSizes;
        int count = sizes != null ? sizes.Count : 0;
        for (int i = 0; i < count; i++)
        {
            int captured = i;
            ChoiceButton choice = Choice(sizeRow, work.FormatSizeLabel(i), () => ChooseSize(work, captured, frame, sizeLabel, borderImage, swatchRect, sizeButtons, frameButtons));
            sizeButtons.Add(choice);
        }

        IReadOnlyList<FrameOption> frames = shelf != null ? shelf.Frames : null;
        if (frames != null)
        {
            for (int i = 0; i < frames.Count; i++)
            {
                FrameOption option = frames[i];
                if (option == null)
                    continue;
                FrameOption capturedFrame = option;
                ChoiceButton choice = Choice(frameRow, option.FrameName, () => ChooseFrame(work, sizeIndex, capturedFrame, sizeLabel, borderImage, swatchRect, sizeButtons, frameButtons));
                choice.Frame = option;
                frameButtons.Add(choice);
            }
        }

        float y = -1160f;
        y = Line(panel.transform, work.Title, 40f, Ink, FontStyles.Bold, y, 56f);
        if (!string.IsNullOrWhiteSpace(work.Artist))
            y = Line(panel.transform, work.Artist, 26f, Stone, FontStyles.Normal, y, 40f);
        if (work.Year != 0)
            y = Line(panel.transform, work.Year.ToString(), 26f, Stone, FontStyles.Normal, y, 40f);
        if (!string.IsNullOrWhiteSpace(work.Medium))
            y = Line(panel.transform, work.Medium, 26f, Stone, FontStyles.Normal, y, 40f);
        y = Line(panel.transform, work.Colour, 26f, Ink, FontStyles.Normal, y, 40f);
        Line(panel.transform, Replies.DescribeStory(work), 26f, Ink, FontStyles.Normal, y, 120f);

        bool saved = session != null && session.IsShortlisted(work);
        var bar = new GameObject("Actions", typeof(RectTransform));
        bar.transform.SetParent(panel.transform, false);
        var barRect = bar.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0f, 0f);
        barRect.anchorMax = new Vector2(1f, 0f);
        barRect.pivot = new Vector2(0.5f, 0f);
        barRect.sizeDelta = new Vector2(-64f, 96f);
        barRect.anchoredPosition = new Vector2(0f, 36f);

        ChoiceButton save = Choice(barRect, saved ? "Saved" : "Save Work", null);
        var saveRect = save.Root;
        saveRect.anchorMin = new Vector2(0f, 0f);
        saveRect.anchorMax = new Vector2(0f, 0f);
        saveRect.pivot = new Vector2(0f, 0f);
        saveRect.sizeDelta = new Vector2(280f, 88f);
        saveRect.anchoredPosition = Vector2.zero;
        ChoiceButton wall = Choice(barRect, "Try on My Wall", () => TryOnWall(work, sizeIndex, frame));
        var wallRect = wall.Root;
        wallRect.anchorMin = new Vector2(0f, 0f);
        wallRect.anchorMax = new Vector2(1f, 0f);
        wallRect.pivot = new Vector2(0f, 0f);
        wallRect.offsetMin = new Vector2(300f, 0f);
        wallRect.offsetMax = new Vector2(0f, 88f);
        wall.Image.color = Accent;

        DetailState state = new DetailState
        {
            Work = work,
            SizeIndex = Mathf.Clamp(sizeIndex, 0, Mathf.Max(0, count - 1)),
            Frame = frame,
            SizeLabel = sizeLabel,
            Border = borderImage,
            Preview = swatchRect,
            Sizes = sizeButtons,
            Frames = frameButtons,
            Save = save,
            Wall = wall
        };
        save.Button.onClick.AddListener(() => ToggleDetailSave(state));
        ApplyDetail(state);
        back.transform.SetAsLastSibling();
    }

    static void ChooseSize(ArtworkData work, int index, FrameOption ignored, TMP_Text sizeLabel, Image border, RectTransform preview, List<ChoiceButton> sizes, List<ChoiceButton> frames)
    {
        FrameOption frame = CurrentFrame(work, frames);
        if (session != null)
            session.Select(work, index, frame);
        RefreshOpen(work);
    }

    static void ChooseFrame(ArtworkData work, int ignoredIndex, FrameOption frame, TMP_Text sizeLabel, Image border, RectTransform preview, List<ChoiceButton> sizes, List<ChoiceButton> frames)
    {
        int index = session != null && session.SelectedArtwork == work ? session.SizeIndex : 0;
        if (session != null)
            session.Select(work, index, frame);
        RefreshOpen(work);
    }

    static void RefreshOpen(ArtworkData work)
    {
        Open(work);
    }

    static void ApplyDetail(DetailState state)
    {
        state.SizeLabel.text = state.Work.FormatSizeLabel(state.SizeIndex);
        Color frameColor = state.Frame != null ? state.Frame.UIHighlightColor : Border;
        bool unframed = state.Frame == null || state.Frame.FrameName == "Unframed";
        state.Border.color = unframed ? Border : frameColor;
        float inset = unframed ? 6f : 22f;
        state.Preview.offsetMin = new Vector2(inset, inset);
        state.Preview.offsetMax = new Vector2(-inset, -inset);

        for (int i = 0; i < state.Sizes.Count; i++)
            state.Sizes[i].Image.color = i == state.SizeIndex ? Accent : Blush;
        for (int i = 0; i < state.Frames.Count; i++)
        {
            bool chosen = state.Frames[i].Frame == state.Frame;
            state.Frames[i].Image.color = chosen ? Accent : Blush;
        }

        bool saved = session != null && session.IsShortlisted(state.Work);
        state.Save.Label.text = saved ? "Saved" : "Save Work";
        state.Save.Image.color = saved ? Accent : Blush;
        state.Wall.Image.color = Accent;
        if (session != null)
            session.Select(state.Work, state.SizeIndex, state.Frame);
    }

    static void ToggleDetailSave(DetailState state)
    {
        if (session == null)
            return;
        bool include = !session.IsShortlisted(state.Work);
        session.SetShortlisted(state.Work, include);
        ApplyDetail(state);
        Notice(include ? state.Work.Title + " saved" : state.Work.Title + " removed");
    }

    static void Notice(string message)
    {
        if (page == null)
            return;

        Transform existing = page.Find("Notice");
        if (existing != null)
            Object.Destroy(existing.gameObject);

        var banner = new GameObject("Notice", typeof(RectTransform), typeof(Image), typeof(NoticeLife));
        banner.transform.SetParent(page, false);
        banner.transform.SetAsLastSibling();
        var rect = banner.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(760f, 88f);
        rect.anchoredPosition = new Vector2(0f, 156f);
        Image image = banner.GetComponent<Image>();
        image.sprite = RoundedSprite();
        image.type = Image.Type.Sliced;
        image.color = Accent;
        image.raycastTarget = false;
        Text(banner.transform, message, 28f, Ink, FontStyles.Bold, Vector2.zero, Vector2.zero, true, TextAlignmentOptions.Center);
    }

    static void TryOnWall(ArtworkData work, int sizeIndex, FrameOption frame)
    {
        if (session != null)
            session.Select(work, sizeIndex, frame);
        TestDrive.Begin();
    }

    static FrameOption CurrentFrame(ArtworkData work, List<ChoiceButton> frames)
    {
        if (session != null && session.SelectedArtwork == work && session.SelectedFrame != null)
            return session.SelectedFrame;
        return FirstFrame();
    }

    static FrameOption FirstFrame()
    {
        if (shelf == null || shelf.Frames == null)
            return null;
        for (int i = 0; i < shelf.Frames.Count; i++)
        {
            if (shelf.Frames[i] != null)
                return shelf.Frames[i];
        }
        return null;
    }

    static RectTransform PillRow(Transform parent, float y)
    {
        var row = new GameObject("Choices", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(parent, false);
        var rect = row.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(960f, 72f);
        rect.anchoredPosition = new Vector2(0f, y);
        var layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        return rect;
    }

    static ChoiceButton Choice(RectTransform parent, string label, UnityEngine.Events.UnityAction action)
    {
        var root = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        root.transform.SetParent(parent, false);
        Image image = root.GetComponent<Image>();
        image.sprite = RoundedSprite();
        image.type = Image.Type.Sliced;
        image.color = Blush;
        var element = root.GetComponent<LayoutElement>();
        element.preferredHeight = 68f;
        element.minWidth = 140f;
        TMP_Text text = Text(root.transform, label, 22f, Ink, FontStyles.Bold, Vector2.zero, Vector2.zero, true, TextAlignmentOptions.Center);
        Button button = root.GetComponent<Button>();
        if (action != null)
            button.onClick.AddListener(action);
        return new ChoiceButton { Root = root.GetComponent<RectTransform>(), Image = image, Label = text, Button = button };
    }

    static Button CornerButton(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        var back = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        back.transform.SetParent(parent, false);
        var backRect = back.GetComponent<RectTransform>();
        backRect.anchorMin = backRect.anchorMax = new Vector2(0f, 1f);
        backRect.pivot = new Vector2(0f, 1f);
        backRect.sizeDelta = new Vector2(180f, 72f);
        backRect.anchoredPosition = position;
        Image image = back.GetComponent<Image>();
        image.sprite = RoundedSprite();
        image.type = Image.Type.Sliced;
        image.color = Card;
        back.GetComponent<Button>().onClick.AddListener(action);
        Text(back.transform, label, 30f, Ink, FontStyles.Bold, Vector2.zero, Vector2.zero, true, TextAlignmentOptions.Center);
        return back.GetComponent<Button>();
    }

    static void TintHeading(RectTransform root)
    {
        Transform title = root.Find("Title");
        if (title == null)
            return;
        TMP_Text text = title.GetComponent<TMP_Text>();
        if (text != null)
            text.color = Ink;
        Transform back = root.Find("Back");
        if (back == null)
            return;
        Image image = back.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = RoundedSprite();
            image.type = Image.Type.Sliced;
            image.color = Card;
        }
    }

    static float Line(Transform parent, string value, float size, Color color, FontStyles style, float y, float height)
    {
        Text(parent, value, size, color, style, new Vector2(0f, y), new Vector2(920f, height), false, TextAlignmentOptions.TopLeft, new Vector2(0.5f, 1f));
        return y - height - 6f;
    }

    static void Arrival(RectTransform content, string value)
    {
        var row = new GameObject("Arrival", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        row.transform.SetParent(content, false);
        row.GetComponent<LayoutElement>().preferredHeight = 140f;
        Image plate = row.GetComponent<Image>();
        plate.color = Blush;
        plate.raycastTarget = false;
        Text(row.transform, value, 26f, Ink, FontStyles.Bold, Vector2.zero, new Vector2(0f, 140f), true, TextAlignmentOptions.Center);
    }

    static void Note(RectTransform content, string value)
    {
        var row = new GameObject("Empty", typeof(RectTransform), typeof(LayoutElement));
        row.transform.SetParent(content, false);
        row.GetComponent<LayoutElement>().preferredHeight = 120f;
        Text(row.transform, value, 28f, Stone, FontStyles.Normal, Vector2.zero, new Vector2(0f, 120f), true);
    }

    static TMP_Text Text(Transform parent, string value, float size, Color color, FontStyles fontStyle, Vector2 position, Vector2 delta, bool fill = false, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft, Vector2? anchor = null)
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
        if (theme != null && theme.Font != null)
            text.font = theme.Font;
        else if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;
        text.text = value ?? "";
        text.fontSize = size;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
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
        image.preserveAspect = false;
        image.color = Blush;
    }

    static Sprite RoundedSprite()
    {
        if (rounded != null)
            return rounded;

        const int size = 96;
        const int radius = 28;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = Mathf.Min(x, size - 1 - x);
                int dy = Mathf.Min(y, size - 1 - y);
                float cover = 1f;
                if (dx < radius && dy < radius)
                {
                    float distance = Vector2.Distance(new Vector2(dx, dy), new Vector2(radius, radius));
                    cover = Mathf.Clamp01(radius - distance + 0.75f);
                }

                pixels[y * size + x] = new Color(1f, 1f, 1f, cover);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        rounded = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        return rounded;
    }

    static bool Contains(List<string> values, string value)
    {
        for (int i = 0; i < values.Count; i++)
        {
            if (string.Equals(values[i], value.Trim(), System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    static Color CanvasColor => theme != null ? theme.CanvasBackground : new Color32(251, 249, 250, 255);
    static Color Card => theme != null ? theme.CardSurface : Color.white;
    static Color Blush => theme != null ? theme.SecondaryAccent : new Color32(249, 235, 240, 255);
    static Color Accent => theme != null ? theme.PrimaryAccent : new Color32(233, 168, 188, 255);
    static Color Ink => theme != null ? theme.PrimaryText : new Color32(42, 34, 37, 255);
    static Color Stone => theme != null ? theme.SecondaryText : new Color32(125, 110, 115, 255);
    static Color Border => theme != null ? theme.CardBorder : new Color32(242, 220, 227, 255);

    sealed class FilterPill
    {
        public Image Image;
        public TMP_Text Label;
        public string Kind;
        public string Value;
    }

    sealed class ChoiceButton
    {
        public RectTransform Root;
        public Image Image;
        public TMP_Text Label;
        public Button Button;
        public FrameOption Frame;
    }

    sealed class DetailState
    {
        public ArtworkData Work;
        public int SizeIndex;
        public FrameOption Frame;
        public TMP_Text SizeLabel;
        public Image Border;
        public RectTransform Preview;
        public List<ChoiceButton> Sizes;
        public List<ChoiceButton> Frames;
        public ChoiceButton Save;
        public ChoiceButton Wall;
    }
}

sealed class NoticeLife : MonoBehaviour
{
    float left = 1.8f;

    void Update()
    {
        left -= Time.unscaledDeltaTime;
        if (left <= 0f)
            Destroy(gameObject);
    }
}
