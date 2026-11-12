using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Off-white entrance. One dash until it is tapped, then five slots.
// The fifth character is checked immediately.
[AddComponentMenu("Intro/Access Gate")]
public class AccessGate : MonoBehaviour
{
    public static bool Granted { get; private set; }

    static readonly string[] Codes =
    {
        "AZURE", "LINEN", "OCHRE", "IVORY", "CEDAR",
        "MAPLE", "TERRA", "PEARL", "SLATE", "CORAL",
        "AMBER", "ATLAS", "QUILL", "MURAL", "BLUSH",
        "CREAM", "EBONY", "FRAME", "GLAZE", "HONEY",
        "LILAC", "OLIVE", "ROUGE", "SEPIA", "UMBER"
    };

    const int Length = 5;
    static readonly Color Paper = new Color32(247, 246, 243, 255);
    static readonly Color LabelInk = new Color32(155, 155, 155, 255);
    static readonly Color LineInk = new Color32(198, 198, 198, 255);
    static readonly Color MarkInk = new Color32(28, 28, 30, 255);

    readonly HashSet<string> valid = new HashSet<string>(Codes);
    readonly Image[] lines = new Image[Length];
    readonly TMP_Text[] glyphs = new TMP_Text[Length];

    CanvasGroup group;
    RectTransform titleRect;
    RectTransform entry;
    CanvasGroup entryGroup;
    CanvasGroup successGroup;
    RectTransform checkCircle;
    TMP_Text thanksName;
    HorizontalLayoutGroup slots;
    Image idleLine;
    TMP_Text notice;
    Action admitted;
    TouchScreenKeyboard board;
    string entered = "";
    float open;
    float openTarget;
    float shake;
    float hold = -1f;
    bool focused;
    bool refused;
    bool thanks;
    float seen;
    bool armed;
    bool wasDown;

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
    }

    void Awake()
    {
        group = gameObject.AddComponent<CanvasGroup>();
        var paper = gameObject.AddComponent<Image>();
        paper.color = Paper;
        paper.raycastTarget = true;
        var root = (RectTransform)transform;
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;

        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        TMP_FontAsset nighty = pendingFont != null ? pendingFont : NightyFont();
        pendingFont = null;
        var pink = new Color32(255, 183, 208, 255);

        var title = NewText("Entrance code", nighty != null ? nighty : font, 68f, pink, FontStyles.Normal);
        title.overflowMode = TextOverflowModes.Overflow;
        titleRect = title.rectTransform;
        titleRect.sizeDelta = new Vector2(900f, 140f);
        titleRect.anchoredPosition = new Vector2(0f, 64f);

        var rule = new GameObject("Dash", typeof(RectTransform), typeof(Image));
        rule.transform.SetParent(transform, false);
        var ruleRect = rule.GetComponent<RectTransform>();
        ruleRect.anchorMin = ruleRect.anchorMax = new Vector2(0.5f, 0.5f);
        ruleRect.pivot = new Vector2(0.5f, 0.5f);
        ruleRect.sizeDelta = new Vector2(460f, 3f);
        ruleRect.anchoredPosition = new Vector2(0f, -28f);
        idleLine = rule.GetComponent<Image>();
        idleLine.color = LineInk;
        idleLine.raycastTarget = false;

        var row = new GameObject("Slots", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(transform, false);
        var rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = rowRect.anchorMax = new Vector2(0.5f, 0.5f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.sizeDelta = new Vector2(900f, 160f);
        rowRect.anchoredPosition = new Vector2(0f, -28f);
        slots = row.GetComponent<HorizontalLayoutGroup>();
        slots.childAlignment = TextAnchor.MiddleCenter;
        slots.childControlWidth = false;
        slots.childControlHeight = false;
        slots.childForceExpandWidth = false;
        slots.childForceExpandHeight = false;
        slots.spacing = -84f;

        for (int i = 0; i < Length; i++)
        {
            var slot = new GameObject("Slot", typeof(RectTransform));
            slot.transform.SetParent(row.transform, false);
            var slotRect = slot.GetComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(84f, 150f);

            TMP_Text glyph = NewText("", font, 68f, MarkInk, FontStyles.Bold);
            glyph.transform.SetParent(slot.transform, false);
            glyph.rectTransform.anchorMin = new Vector2(0f, 0.28f);
            glyph.rectTransform.anchorMax = new Vector2(1f, 1f);
            glyph.rectTransform.offsetMin = glyph.rectTransform.offsetMax = Vector2.zero;
            glyphs[i] = glyph;

            var line = new GameObject("Line", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(slot.transform, false);
            var lineRect = line.GetComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0f, 0.5f);
            lineRect.anchorMax = new Vector2(1f, 0.5f);
            lineRect.pivot = new Vector2(0.5f, 0.5f);
            lineRect.sizeDelta = new Vector2(16f, 3f);
            lineRect.anchoredPosition = Vector2.zero;
            lines[i] = line.GetComponent<Image>();
            lines[i].color = new Color(LineInk.r, LineInk.g, LineInk.b, 0f);
            lines[i].raycastTarget = false;
        }

        notice = NewText("", font, 32f, LabelInk, FontStyles.Normal);
        notice.rectTransform.sizeDelta = new Vector2(860f, 70f);
        notice.rectTransform.anchoredPosition = new Vector2(0f, -150f);
        notice.color = new Color(LabelInk.r, LabelInk.g, LabelInk.b, 0f);

        var entryObject = new GameObject("Entry", typeof(RectTransform), typeof(CanvasGroup));
        entryObject.transform.SetParent(transform, false);
        entry = entryObject.GetComponent<RectTransform>();
        entry.anchorMin = Vector2.zero;
        entry.anchorMax = Vector2.one;
        entry.offsetMin = entry.offsetMax = Vector2.zero;
        entryGroup = entryObject.GetComponent<CanvasGroup>();
        titleRect.SetParent(entry, true);
        idleLine.rectTransform.SetParent(entry, true);
        slots.transform.SetParent(entry, true);
        notice.rectTransform.SetParent(entry, true);

        BuildThanks(nighty != null ? nighty : font, font);
    }

    void Update()
    {
        float step = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        open = Mathf.MoveTowards(open, openTarget, step / 0.42f);
        float eased = Ease(open);
        slots.spacing = Mathf.Lerp(-84f, 38f, eased);
        float lineWidth = Mathf.Lerp(16f, -6f, eased);
        Color segment = LineInk;
        segment.a = eased;
        for (int i = 0; i < Length; i++)
        {
            lines[i].rectTransform.sizeDelta = new Vector2(lineWidth, 3f);
            lines[i].color = segment;
        }
        Color single = LineInk;
        single.a = 1f - eased;
        idleLine.color = single;
        titleRect.anchoredPosition = new Vector2(0f, Mathf.Lerp(64f, 156f, eased));

        float wave = 0f;
        if (shake > 0f)
        {
            shake = Mathf.Max(0f, shake - step);
            wave = Mathf.Sin(shake * 52f) * shake * 36f;
        }
        entry.anchoredPosition = new Vector2(wave, 0f);

        if (thanks)
        {
            entryGroup.alpha = Mathf.MoveTowards(entryGroup.alpha, 0f, step / 0.28f);
            successGroup.alpha = Mathf.MoveTowards(successGroup.alpha, 1f, step / 0.4f);
            float pop = Ease(successGroup.alpha);
            checkCircle.localScale = Vector3.one * Mathf.Lerp(0.84f, 1f, pop);
            seen += step;
            if (seen > 1.2f)
            {
                MainMenu.TakeOver();
                Destroy(gameObject);
            }
        }

        bool down = Pressed();
        if (!armed)
            armed = !down;
        else if (down && !wasDown && !focused && !thanks && OverDash())
            Focus();
        wasDown = down;

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
            if (TouchScreenKeyboard.isSupported)
                board = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.ASCIICapable, false, false, false, false, "", Length);
            Refresh();
        }
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

    bool OverDash()
    {
        Vector2 screen = PointerScreen();
        if (screen == Vector2.zero && !Pressed())
            return false;

        RectTransform area = idleLine.rectTransform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screen, null, out Vector2 local))
            return false;

        Rect hit = area.rect;
        hit.xMin -= 28f;
        hit.xMax += 28f;
        hit.yMin -= 42f;
        hit.yMax += 42f;
        return hit.Contains(local);
    }

    static Vector2 PointerScreen()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            return Touchscreen.current.primaryTouch.position.ReadValue();
        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();
        Pointer pointer = Pointer.current;
        return pointer != null ? pointer.position.ReadValue() : Vector2.zero;
    }

    void Focus()
    {
        focused = true;
        openTarget = 1f;
        if (TouchScreenKeyboard.isSupported)
            board = TouchScreenKeyboard.Open(entered, TouchScreenKeyboardType.ASCIICapable, false, false, false, false, "", Length);
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

        for (Key key = Key.A; key <= Key.Z; key++)
        {
            if (!keyboard[key].wasPressedThisFrame)
                continue;
            Apply(entered + (char)('A' + (key - Key.A)));
            return;
        }

        for (Key key = Key.Digit0; key <= Key.Digit9; key++)
        {
            if (!keyboard[key].wasPressedThisFrame)
                continue;
            Apply(entered + (char)('0' + (key - Key.Digit0)));
            return;
        }

        for (Key key = Key.Numpad0; key <= Key.Numpad9; key++)
        {
            if (!keyboard[key].wasPressedThisFrame)
                continue;
            Apply(entered + (char)('0' + (key - Key.Numpad0)));
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
        notice.color = new Color(LabelInk.r, LabelInk.g, LabelInk.b, 0f);
        Refresh();
        if (entered.Length < Length)
            return;

        if (valid.Contains(entered))
        {
            focused = false;
            thanks = true;
            Granted = true;
            thanksName.text = "Thanks " + Pretty(entered) + "!";
            if (board != null)
                board.active = false;
            if (admitted != null)
                admitted.Invoke();
            return;
        }

        refused = true;
        shake = 0.4f;
        hold = 0.46f;
    }

    void Refresh()
    {
        for (int i = 0; i < Length; i++)
            glyphs[i].text = i < entered.Length ? entered[i].ToString() : "";
    }

    void BlinkCaret()
    {
        bool blink = focused && !refused && !thanks && entered.Length < Length && Mathf.Repeat(Time.unscaledTime, 1.1f) < 0.55f;
        for (int i = 0; i < Length; i++)
        {
            if (!blink || i != entered.Length)
                continue;
            lines[i].color = MarkInk;
        }
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

    static bool Pressed()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            return true;
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            return true;
        Pointer pointer = Pointer.current;
        return pointer != null && pointer.press.isPressed;
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

        var pink = new Color32(255, 183, 208, 255);
        var circle = new GameObject("Mark", typeof(RectTransform), typeof(Image));
        circle.transform.SetParent(host.transform, false);
        checkCircle = circle.GetComponent<RectTransform>();
        checkCircle.anchorMin = checkCircle.anchorMax = new Vector2(0.5f, 0.5f);
        checkCircle.pivot = new Vector2(0.5f, 0.5f);
        checkCircle.sizeDelta = new Vector2(132f, 132f);
        checkCircle.anchoredPosition = new Vector2(0f, 92f);
        var circleImage = circle.GetComponent<Image>();
        circleImage.sprite = Icon();
        circleImage.color = Color.white;
        circleImage.raycastTarget = false;

        thanksName = NewText("Thanks", nighty, 64f, pink, FontStyles.Normal);
        thanksName.overflowMode = TextOverflowModes.Overflow;
        thanksName.transform.SetParent(host.transform, false);
        thanksName.rectTransform.sizeDelta = new Vector2(920f, 130f);
        thanksName.rectTransform.anchoredPosition = new Vector2(0f, -36f);

        var note = NewText("Your code has been received.\nYou can continue.", plain, 30f, new Color32(186, 186, 186, 255), FontStyles.Normal);
        note.transform.SetParent(host.transform, false);
        note.rectTransform.sizeDelta = new Vector2(780f, 96f);
        note.rectTransform.anchoredPosition = new Vector2(0f, -118f);
    }

    static Sprite icon;

    static Sprite Icon()
    {
        if (icon != null)
            return icon;

        const int size = 160;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color32[size * size];
        var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float circle = size * 0.48f;
        var pink = new Color32(248, 214, 226, 255);
        var white = new Color32(255, 255, 255, 255);
        Vector2 start = new Vector2(size * 0.30f, size * 0.46f);
        Vector2 corner = new Vector2(size * 0.42f, size * 0.33f);
        Vector2 end = new Vector2(size * 0.73f, size * 0.67f);
        float thickness = size * 0.105f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                var point = new Vector2(x, y);
                float outside = Vector2.Distance(point, center) - circle;
                if (outside > 1f)
                    continue;

                float edge = Mathf.Clamp01(1f - outside);
                float mark = thickness - Mathf.Min(DistanceToSegment(point, start, corner), DistanceToSegment(point, corner, end));
                float cover = Mathf.Clamp01(mark);
                Color ink = Color.Lerp(pink, white, cover);
                ink.a *= edge;
                pixels[y * size + x] = ink;
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        icon = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        return icon;
    }

    static float DistanceToSegment(Vector2 point, Vector2 from, Vector2 to)
    {
        Vector2 line = to - from;
        float length = line.sqrMagnitude;
        if (length < 0.001f)
            return Vector2.Distance(point, from);

        float along = Mathf.Clamp01(Vector2.Dot(point - from, line) / length);
        return Vector2.Distance(point, from + line * along);
    }

    static string Pretty(string code)
    {
        if (string.IsNullOrEmpty(code))
            return "";
        return char.ToUpperInvariant(code[0]) + code.Substring(1).ToLowerInvariant();
    }

    static float Ease(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }
}
