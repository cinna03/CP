using System;
using Capstone.App.Data;
using Capstone.App.Services;
using Capstone.App.UI;
using Capstone.Session;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Off-white entrance. Five slots are ready at once.
// A letter or a digit fills the next slot. The fifth character is checked immediately.
[AddComponentMenu("Intro/Access Gate")]
public class AccessGate : MonoBehaviour
{
    public static bool Granted { get; private set; }

    const int Length = 5;
    // Editorial direction: off-white paper, ink type, dusty-pink wordmark.
    static readonly Color Paper = new Color32(0xFC, 0xFB, 0xFA, 0xFF);
    static readonly Color Ink = new Color32(0x2B, 0x21, 0x24, 0xFF);
    static readonly Color Quiet = new Color32(0xD9, 0xC3, 0xC9, 0xFF);
    static readonly Color Stone = new Color32(0x6B, 0x53, 0x59, 0xFF);
    static readonly Color Rose = new Color32(0xB8, 0x5C, 0x74, 0xFF);
    static readonly Color Blush = new Color32(0xE4, 0xA9, 0xB5, 0xFF);
    const float Margin = 78f;
    const float SlotsY = -275f;

    readonly Image[] borders = new Image[Length];
    readonly Image[] fills = new Image[Length];
    readonly Image[] carets = new Image[Length];
    readonly TMP_Text[] glyphs = new TMP_Text[Length];
    readonly Image[] lines = new Image[Length];
    readonly float[] born = new float[Length];

    RectTransform titleRect;
    RectTransform entry;
    CanvasGroup entryGroup;
    CanvasGroup successGroup;
    RectTransform checkCircle;
    TMP_Text thanksName;
    RectTransform slotsRect;
    [SerializeField] Image topBowImage;
    [SerializeField] TMP_FontAsset nightyFont;
    CanvasGroup bowGroup;
    int shown;
    float revealAt = -1f;
    float shakeTime = -1f;
    Action admitted;
    TouchScreenKeyboard board;
    string entered = "";
    float hold = -1f;
    bool focused;
    bool refused;
    bool thanks;
    float seen;
    float bowAt = -1f;
    public static void Reopen()
    {
        Granted = false;
        Present(null, null);
    }

    public static void Present(Action onAdmitted, TMP_FontAsset nighty)
    {
        if (Granted)
        {
            onAdmitted?.Invoke();
            return;
        }

        pendingFont = NightyFont();
        if (pendingFont == null)
            pendingFont = nighty;
        var host = new GameObject("Entrance", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(AccessGate));
        var canvas = host.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;
        canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1
            | AdditionalCanvasShaderChannels.Normal
            | AdditionalCanvasShaderChannels.Tangent;

        var scaler = host.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0f;

            host.GetComponent<AccessGate>().admitted = onAdmitted;
            UserProfileFlush.Ensure();
    }

    void Awake()
    {
        gameObject.AddComponent<Capstone.App.UI.SafeAreaFitter>();
        var paper = gameObject.AddComponent<Image>();
        paper.color = Paper;
        paper.raycastTarget = true;
        var root = (RectTransform)transform;
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;

        for (int i = 0; i < Length; i++)
            born[i] = -1f;

        TMP_FontAsset font = Capstone.App.UI.AppType.BodyFont;
        TMP_FontAsset nighty = nightyFont != null ? nightyFont : pendingFont != null ? pendingFont : NightyFont();
        if (nighty == null)
            nighty = ThemeNighty();
        pendingFont = null;
        TMP_FontAsset words = nighty != null ? nighty : font;

        BuildBow();

        TMP_FontAsset heavy = Capstone.App.UI.AppType.HeavyFont;
        TMP_FontAsset label = Capstone.App.UI.AppType.LabelFont;

        TMP_Text prompt = NewText("VISIT CODE", label, 32f, Stone, FontStyles.Normal);
        prompt.characterSpacing = 12f;
        prompt.alignment = TextAlignmentOptions.Left;
        titleRect = prompt.rectTransform;
        PinLeft(titleRect, new Vector2(900f, 48f), SlotsY + 130f);

        TMP_Text tagline = NewText("See it at its real size,\non your own wall.", label, 60f, Ink, FontStyles.Normal);
        tagline.alignment = TextAlignmentOptions.TopLeft;
        tagline.lineSpacing = -8f;
        PinLeft(tagline.rectTransform, new Vector2(900f, 160f), SlotsY + 300f);

        TMP_Text place = NewText("ART \u00B7 KIGALI", label, 34f, Ink, FontStyles.Normal);
        place.characterSpacing = 8f;
        place.alignment = TextAlignmentOptions.Left;
        RectTransform placeRect = place.rectTransform;
        placeRect.anchorMin = placeRect.anchorMax = new Vector2(0f, 1f);
        placeRect.pivot = new Vector2(0f, 1f);
        placeRect.sizeDelta = new Vector2(500f, 48f);
        placeRect.anchoredPosition = new Vector2(Margin, -150f);

        TMP_Text issue = NewText("N\u00BA 01", label, 34f, Ink, FontStyles.Normal);
        issue.alignment = TextAlignmentOptions.Right;
        RectTransform issueRect = issue.rectTransform;
        issueRect.anchorMin = issueRect.anchorMax = new Vector2(1f, 1f);
        issueRect.pivot = new Vector2(1f, 1f);
        issueRect.sizeDelta = new Vector2(300f, 48f);
        issueRect.anchoredPosition = new Vector2(-Margin, -150f);

        TMP_Text wordmark = NewText("Cinnarios", heavy, 330f, Blush, FontStyles.Bold);
        wordmark.characterSpacing = -6f;
        wordmark.alignment = TextAlignmentOptions.BottomLeft;
        wordmark.overflowMode = TextOverflowModes.Overflow;
        RectTransform markRect = wordmark.rectTransform;
        markRect.anchorMin = markRect.anchorMax = new Vector2(0f, 0f);
        markRect.pivot = new Vector2(0f, 0f);
        markRect.sizeDelta = new Vector2(1400f, 380f);
        markRect.anchoredPosition = new Vector2(-14f, -96f);
        Capstone.App.UI.UIMotion.Appear(wordmark.gameObject, 0.25f, 0.7f);

        var row = new GameObject("Slots", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(transform, false);
        slotsRect = row.GetComponent<RectTransform>();
        slotsRect.anchorMin = slotsRect.anchorMax = new Vector2(0f, 0.5f);
        slotsRect.pivot = new Vector2(0f, 0.5f);
        slotsRect.sizeDelta = new Vector2(728f, 150f);
        slotsRect.anchoredPosition = new Vector2(Margin, SlotsY);
        var layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = 22f;

        for (int i = 0; i < Length; i++)
        {
            var slot = new GameObject("Slot", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            slot.transform.SetParent(row.transform, false);
            var slotRect = slot.GetComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(128f, 144f);
            slotRect.localScale = new Vector3(0.85f, 0.85f, 1f);
            borders[i] = slot.GetComponent<Image>();
            borders[i].sprite = Rounded(12);
            borders[i].type = Image.Type.Sliced;
            borders[i].color = Quiet;
            borders[i].raycastTarget = false;
            slot.GetComponent<CanvasGroup>().alpha = 0f;

            var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(slot.transform, false);
            var fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(1.5f, 1.5f);
            fillRect.offsetMax = new Vector2(-1.5f, -1.5f);
            fills[i] = fillObject.GetComponent<Image>();
            fills[i].sprite = Rounded(12);
            fills[i].type = Image.Type.Sliced;
            fills[i].color = Paper;
            fills[i].raycastTarget = false;

            var lineObject = new GameObject("Line", typeof(RectTransform), typeof(Image));
            lineObject.transform.SetParent(slot.transform, false);
            var lineRect = lineObject.GetComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0f, 0f);
            lineRect.anchorMax = new Vector2(1f, 0f);
            lineRect.pivot = new Vector2(0.5f, 0f);
            lineRect.sizeDelta = new Vector2(0f, 5f);
            lineRect.anchoredPosition = Vector2.zero;
            lines[i] = lineObject.GetComponent<Image>();
            lines[i].color = Quiet;
            lines[i].raycastTarget = false;

            TMP_Text glyph = NewText("", heavy, 72f, Ink, FontStyles.Bold);
            glyph.characterSpacing = -1f;
            glyph.transform.SetParent(slot.transform, false);
            glyph.rectTransform.anchorMin = Vector2.zero;
            glyph.rectTransform.anchorMax = Vector2.one;
            glyph.rectTransform.offsetMin = glyph.rectTransform.offsetMax = Vector2.zero;
            glyphs[i] = glyph;

            var caret = new GameObject("Caret", typeof(RectTransform), typeof(Image));
            caret.transform.SetParent(slot.transform, false);
            var caretRect = caret.GetComponent<RectTransform>();
            caretRect.anchorMin = caretRect.anchorMax = new Vector2(0.5f, 0.5f);
            caretRect.sizeDelta = new Vector2(4f, 72f);
            carets[i] = caret.GetComponent<Image>();
            carets[i].color = Ink;
            carets[i].raycastTarget = false;
            caret.SetActive(false);
        }

        var entryObject = new GameObject("Entry", typeof(RectTransform), typeof(CanvasGroup));
        entryObject.transform.SetParent(transform, false);
        entry = entryObject.GetComponent<RectTransform>();
        entry.anchorMin = Vector2.zero;
        entry.anchorMax = Vector2.one;
        entry.offsetMin = entry.offsetMax = Vector2.zero;
        entryGroup = entryObject.GetComponent<CanvasGroup>();
        titleRect.SetParent(entry, true);
        tagline.rectTransform.SetParent(entry, true);
        placeRect.SetParent(entry, true);
        issueRect.SetParent(entry, true);
        if (topBowImage != null)
            topBowImage.rectTransform.SetParent(entry, true);
        slotsRect.SetParent(entry, true);
        BuildThanks(heavy, label);
    }

    void OnEnable()
    {
        if (Keyboard.current != null)
            Keyboard.current.onTextInput += OnTyped;
    }

    void OnDisable()
    {
        if (Keyboard.current != null)
            Keyboard.current.onTextInput -= OnTyped;
    }

    void Start()
    {
        bowAt = Time.unscaledTime;
        if (bowGroup != null)
        {
            bowGroup.alpha = 0f;
            topBowImage.rectTransform.localScale = Vector3.one * 0.92f;
        }
        Focus();
    }

    void Update()
    {
        float step = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        if (bowAt >= 0f && bowGroup != null)
        {
            float u = Mathf.Clamp01((Time.unscaledTime - bowAt) / 0.35f);
            float ease = 1f - (1f - u) * (1f - u);
            bowGroup.alpha = ease;
            float scale = Mathf.Lerp(0.92f, 1f, ease);
            topBowImage.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }
        float wave = 0f;
        if (shakeTime >= 0f)
        {
            shakeTime += step;
            wave = Mathf.Sin(shakeTime * 45f) * Mathf.Exp(-shakeTime * 8f) * 18f;
            if (shakeTime >= 0.40f)
                shakeTime = -1f;
        }
        if (slotsRect != null)
            slotsRect.anchoredPosition = new Vector2(Margin + wave, SlotsY);

        PaintBoxes();

        if (thanks)
        {
            entryGroup.alpha = Mathf.MoveTowards(entryGroup.alpha, 0f, step / 0.28f);
            successGroup.alpha = Mathf.MoveTowards(successGroup.alpha, 1f, step / 0.4f);
            float pop = Ease(successGroup.alpha);
            checkCircle.localScale = Vector3.one * Mathf.Lerp(0.84f, 1f, pop);
            seen += step;
            if (seen > 1.2f)
            {
                Action done = admitted;
                admitted = null;
                if (done != null)
                    done.Invoke();
                else
                    MainMenu.TakeOver();
                Destroy(gameObject);
            }
        }

        if (focused && !thanks)
            ReadEntry();

        BlinkCaret();

        if (hold < 0f || thanks)
            return;

        hold -= step;
        if (hold > 0f)
            return;

        hold = -1f;
        if (refused)
        {
            entered = "";
            refused = false;
            for (int i = 0; i < Length; i++)
                born[i] = -1f;
            if (TouchScreenKeyboard.isSupported)
                board = OpenKeyboard("", TouchScreenKeyboardType.ASCIICapable, false, false, false, false, "", Length);
            Refresh();
        }
    }

    /// <summary>
    /// Opens the system keyboard without its own text box, so the letters only appear in the
    /// underlined slots of the entrance screen (iOS otherwise adds a full-width input bar).
    /// </summary>
    static TouchScreenKeyboard OpenKeyboard(string text, TouchScreenKeyboardType type, bool autocorrect, bool multiline, bool secure, bool alert, string placeholder, int limit)
    {
        TouchScreenKeyboard.hideInput = true;
        return TouchScreenKeyboard.Open(text, type, autocorrect, multiline, secure, alert, placeholder, limit);
    }

    static TMP_FontAsset pendingFont;

    static TMP_FontAsset NightyFont()
    {
        TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        for (int i = 0; i < fonts.Length; i++)
        {
            if (fonts[i] != null && fonts[i].name.IndexOf("Nighty", StringComparison.OrdinalIgnoreCase) >= 0)
                return fonts[i];
        }

        return null;
    }

    void BuildBow()
    {
        if (topBowImage == null)
        {
            var bowObject = new GameObject("TopBowAccentImage", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            bowObject.transform.SetParent(transform, false);
            topBowImage = bowObject.GetComponent<Image>();
        }

        RectTransform bowRect = topBowImage.rectTransform;
        bowRect.anchorMin = bowRect.anchorMax = new Vector2(0.5f, 1f);
        bowRect.pivot = new Vector2(0.5f, 1f);
        bowRect.sizeDelta = new Vector2(460f, 460f);
        bowRect.anchoredPosition = new Vector2(0f, -36f);
        topBowImage.preserveAspect = true;
        topBowImage.raycastTarget = false;
        if (topBowImage.sprite == null)
            topBowImage.sprite = Resources.Load<Sprite>("TopBowAccent");
        if (topBowImage.sprite == null)
            topBowImage.sprite = Resources.Load<Sprite>("Bow Pin");
        bowGroup = topBowImage.GetComponent<CanvasGroup>();
        if (bowGroup == null)
            bowGroup = topBowImage.gameObject.AddComponent<CanvasGroup>();
        bowGroup.alpha = 0f;
        bowGroup.blocksRaycasts = false;
    }

    static TMP_FontAsset ThemeNighty()
    {
        UIThemeData[] themes = Resources.FindObjectsOfTypeAll<UIThemeData>();
        for (int i = 0; i < themes.Length; i++)
        {
            if (themes[i] != null && themes[i].NightyFont != null)
                return themes[i].NightyFont;
        }

        return null;
    }

    void OnTyped(char c)
    {
        if (board != null || !focused || thanks || refused)
            return;
        if (char.IsLetterOrDigit(c))
            Apply(entered + c);
    }

    void Focus()
    {
        if (focused || thanks || refused)
            return;

        focused = true;
        revealAt = Time.unscaledTime;
        if (TouchScreenKeyboard.isSupported)
            board = OpenKeyboard(entered, TouchScreenKeyboardType.ASCIICapable, false, false, false, false, "", Length);
    }

    void ReadEntry()
    {
        if (board != null)
        {
            Apply(board.text);
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || refused || thanks)
            return;

        if (keyboard.backspaceKey.wasPressedThisFrame || keyboard.deleteKey.wasPressedThisFrame)
        {
            if (entered.Length > 0)
                Apply(entered.Substring(0, entered.Length - 1));
            return;
        }

    }

    void Apply(string raw)
    {
        if (thanks || refused)
            return;

        string next = Sanitize(raw);
        if (next == entered)
            return;

        entered = next;
        Refresh();
        if (entered.Length < Length)
            return;

        Admit(entered);
    }

    void Admit(string raw)
    {
        string code = UserProfileTracker.Normalize(raw);
        if (code.Length < 2)
            return;

        if (!EntranceCodes.Accepts(code))
        {
            refused = true;
            shakeTime = 0f;
            hold = 0.40f;
#if UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
            return;
        }

        focused = false;
        thanks = true;
        Granted = true;
        new UserProfileTracker().Register(code);
        UserProfileFlush.Ensure();
        CatalogueShelf shelf = Resources.Load<CatalogueShelf>("CatalogueShelf");
        if (shelf != null && shelf.Session != null)
            shelf.Session.RestoreSaved(shelf.Works);
        thanksName.text = "Thanks " + Pretty(code) + "!";
        if (board != null)
            board.active = false;
    }

    void OnApplicationPause(bool pause)
    {
        if (pause)
            UserProfileTracker.Flush();
    }

    void OnApplicationQuit()
    {
        UserProfileTracker.Flush();
    }

    void Refresh()
    {
        if (entered.Length > shown)
        {
            for (int i = shown; i < entered.Length; i++)
                born[i] = Time.unscaledTime;
        }
        else if (entered.Length < shown)
        {
            for (int i = entered.Length; i < shown; i++)
                born[i] = -1f;
        }

        shown = entered.Length;
        for (int i = 0; i < Length; i++)
            glyphs[i].text = i < entered.Length ? entered[i].ToString() : "";
    }

    void PaintBoxes()
    {
        float pulse = Mathf.PingPong(Time.unscaledTime, 0.5f) / 0.5f;
        for (int i = 0; i < Length; i++)
        {
            if (borders[i] == null)
                return;

            bool active = focused && !refused && !thanks && i == entered.Length;
            float reveal = Reveal(i);
            float wave = thanks ? SuccessWave(i) : 1f;
            float pop = Pop(i);
            float scale = Mathf.LerpUnclamped(0.85f, 1f, reveal) * pop * wave;
            borders[i].rectTransform.localScale = new Vector3(scale, scale, 1f);
            CanvasGroup fade = borders[i].GetComponent<CanvasGroup>();
            if (fade != null)
                fade.alpha = focused || thanks ? Mathf.Clamp01((Time.unscaledTime - revealAt - i * 0.03f) / 0.16f) : 0f;

            fills[i].color = Color.clear;
            borders[i].color = Color.clear;
            if (lines[i] != null)
                lines[i].color = refused ? Rose : active ? Rose : i < entered.Length ? Ink : Quiet;

            bool showCaret = active;
            carets[i].gameObject.SetActive(showCaret);
            if (showCaret)
                carets[i].color = new Color(Ink.r, Ink.g, Ink.b, pulse);
        }
    }

    float Reveal(int index)
    {
        if ((!focused && !thanks) || revealAt < 0f)
            return 0f;
        float t = (Time.unscaledTime - revealAt - index * 0.03f) / 0.28f;
        if (t <= 0f)
            return 0f;
        return EaseOutBack(t);
    }

    float Pop(int index)
    {
        if (born[index] < 0f)
            return 1f;
        float u = Mathf.Clamp01((Time.unscaledTime - born[index]) / 0.14f);
        if (u < 0.45f)
            return Mathf.Lerp(1f, 1.12f, EaseOut(u / 0.45f));
        return Mathf.Lerp(1.12f, 1f, EaseOut((u - 0.45f) / 0.55f));
    }

    float SuccessWave(int index)
    {
        float t = seen - index * 0.04f;
        if (t <= 0f)
            return 1f;
        float u = Mathf.Clamp01(t / 0.16f);
        if (u < 0.5f)
            return Mathf.Lerp(1f, 1.08f, u * 2f);
        return Mathf.Lerp(1.08f, 1f, (u - 0.5f) * 2f);
    }

    void BlinkCaret()
    {
    }

    static string Sanitize(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return "";

        var kept = new System.Text.StringBuilder(Length);
        string upper = raw.ToUpperInvariant();
        for (int i = 0; i < upper.Length && kept.Length < Length; i++)
        {
            char c = upper[i];
            if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                kept.Append(c);
        }

        return kept.ToString();
    }

    static void PinLeft(RectTransform rect, Vector2 size, float y)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = new Vector2(Margin, y);
    }

    TMP_Text NewText(string value, TMP_FontAsset font, float size, Color color, FontStyles style)
    {
        var item = new GameObject("Text", typeof(RectTransform));
        item.transform.SetParent(transform, false);
        var text = item.AddComponent<TextMeshProUGUI>();
        if (font != null)
            text.font = font;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        var rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        return text;
    }

    void BuildThanks(TMP_FontAsset nighty, TMP_FontAsset plain)
    {
        var host = new GameObject("Thanks", typeof(RectTransform), typeof(CanvasGroup));
        host.transform.SetParent(transform, false);
        var hostRect = host.GetComponent<RectTransform>();
        hostRect.anchorMin = Vector2.zero;
        hostRect.anchorMax = Vector2.one;
        hostRect.offsetMin = hostRect.offsetMax = Vector2.zero;
        successGroup = host.GetComponent<CanvasGroup>();
        successGroup.alpha = 0f;
        successGroup.blocksRaycasts = false;

        var circle = new GameObject("Mark", typeof(RectTransform), typeof(Image));
        circle.transform.SetParent(host.transform, false);
        checkCircle = circle.GetComponent<RectTransform>();
        checkCircle.anchorMin = checkCircle.anchorMax = new Vector2(0.5f, 0.5f);
        checkCircle.pivot = new Vector2(0.5f, 0.5f);
        checkCircle.sizeDelta = new Vector2(132f, 132f);
        checkCircle.anchoredPosition = new Vector2(0f, 92f);
        var circleImage = circle.GetComponent<Image>();
        circleImage.sprite = Resources.Load<Sprite>("PinkTick");
        circleImage.preserveAspect = true;
        circleImage.color = Color.white;
        circleImage.raycastTarget = false;

        thanksName = NewText("Thanks", nighty, 96f, Ink, FontStyles.Bold);
        thanksName.overflowMode = TextOverflowModes.Overflow;
        thanksName.transform.SetParent(host.transform, false);
        thanksName.rectTransform.sizeDelta = new Vector2(920f, 130f);
        thanksName.rectTransform.anchoredPosition = new Vector2(0f, -36f);

        var note = NewText("Your code has been received.\nYou can continue.", plain, 28f, Stone, FontStyles.Normal);
        note.transform.SetParent(host.transform, false);
        note.rectTransform.sizeDelta = new Vector2(780f, 96f);
        note.rectTransform.anchoredPosition = new Vector2(0f, -118f);
    }

    static string Pretty(string code)
    {
        if (string.IsNullOrEmpty(code))
            return "";
        return char.ToUpperInvariant(code[0]) + code.Substring(1).ToLowerInvariant();
    }

    static Sprite rounded;

    static Sprite Rounded(int radius)
    {
        if (rounded != null)
            return rounded;

        const int edge = 48;
        var texture = new Texture2D(edge, edge, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        var pixels = new Color32[edge * edge];
        for (int y = 0; y < edge; y++)
        {
            for (int x = 0; x < edge; x++)
            {
                float dx = 0f;
                float dy = 0f;
                if (x < radius)
                    dx = radius - x;
                else if (x >= edge - radius)
                    dx = x - (edge - radius - 1);
                if (y < radius)
                    dy = radius - y;
                else if (y >= edge - radius)
                    dy = y - (edge - radius - 1);
                float outside = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                byte alpha = (byte)(Mathf.Clamp01(1f - outside) * 255f);
                pixels[y * edge + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        rounded = Sprite.Create(texture, new Rect(0f, 0f, edge, edge), new Vector2(0.5f, 0.5f), edge, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        return rounded;
    }

    static float EaseOut(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - (1f - t) * (1f - t);
    }

    static float EaseOutBack(float t)
    {
        t = Mathf.Clamp01(t);
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    static float Ease(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }
}
