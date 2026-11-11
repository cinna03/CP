using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Plays the opening on the two panels already under the canvas.
// The first Cinnarios is the start pose. The second is the pose it lands in.
// The ball carries the painting, rests at the end of the word, then fills the screen.
[AddComponentMenu("Intro/Intro Reveal")]
public class IntroReveal : MonoBehaviour
{
    [SerializeField] RectTransform beginningPanel;
    [SerializeField] TMP_Text beginningName;
    [SerializeField] RectTransform paintingPanel;
    [SerializeField] TMP_Text paintingName;
    [SerializeField] float hopSeconds = 0.34f;
    [SerializeField] float morphSeconds = 1.05f;

    Canvas canvas;
    RectTransform canvasRect;
    RectTransform wordRect;
    Image ball;
    Image pink;
    RawImage fill;
    Texture paintingTexture;
    CanvasGroup paintingGroup;
    float restDiameter;
    float coverDiameter;
    Vector2 restPoint;

    Color hidden;
    Color ink;
    Color ballPink;
    Pose startPose;
    Pose endPose;
    readonly List<int> letters = new List<int>();
    float[] shown;

    Beat beat = Beat.Wait;
    float time;
    int letter;
    Vector2 ballFrom;
    Vector2 ballTo;
    Vector2 hopUp = Vector2.up;
    float arc;
    bool firstDrop = true;

    const float WaitSeconds = 0.22f;
    const float HoldSeconds = 0.06f;
    const float SettleSeconds = 0.36f;
    const float RestSeconds = 0.28f;

    enum Beat { Wait, Drop, Hold, Settle, Rest, Morph, Done }

    struct Pose
    {
        public Vector3 center;
        public Quaternion rotation;
        public Vector2 size;
        public Vector3 localScale;
        public float fontSize;
        public float spacing;
    }

    void Awake()
    {
        canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            enabled = false;
            return;
        }

        canvasRect = canvas.transform as RectTransform;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;
        canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1
            | AdditionalCanvasShaderChannels.Normal
            | AdditionalCanvasShaderChannels.Tangent;

        Resolve();
        if (beginningPanel == null || beginningName == null || paintingPanel == null || paintingName == null)
        {
            Debug.LogWarning("Intro Reveal needs the plain panel, its Cinnarios text, the painting panel, and the Cinnarios text on that painting.");
            enabled = false;
            return;
        }

        hidden = PanelColor(beginningPanel);
        ink = paintingName.color;
        if (SameColor(hidden, ink))
            ink = new Color(0.878f, 0.082f, 0.384f, 1f);
        ballPink = Colorful(ink) ? ink : new Color(0.89f, 0.29f, 0.48f, 1f);

        CenterInPanel(paintingName, paintingPanel, FlatAngle(paintingName));
        CenterInPanel(beginningName, beginningPanel, FlatAngle(beginningName));

        endPose = ReadPose(paintingName);
        endPose.localScale = ScaleOnCanvas(paintingName.rectTransform);
        paintingName.gameObject.SetActive(false);

        paintingGroup = paintingPanel.GetComponent<CanvasGroup>();
        if (paintingGroup == null)
            paintingGroup = paintingPanel.gameObject.AddComponent<CanvasGroup>();
        paintingGroup.alpha = 0f;
        paintingGroup.blocksRaycasts = false;

        beginningPanel.SetSiblingIndex(0);
        paintingPanel.SetSiblingIndex(1);

        wordRect = beginningName.rectTransform;
        beginningName.enableAutoSizing = false;
        beginningName.outlineWidth = 0f;
        beginningName.color = Color.white;
        beginningName.ForceMeshUpdate();
        CollectLetters();
        if (letters.Count == 0)
        {
            Debug.LogWarning("Intro Reveal could not see any letters in the opening Cinnarios.");
            enabled = false;
            return;
        }

        shown = new float[letters.Count];
        ApplyInk();

        paintingTexture = PaintingTexture();
        ball = MakeBall();
        float openingHeight = LetterHeight(0);
        ball.rectTransform.anchoredPosition = Rest(0) + new Vector2(0f, openingHeight * 2.6f);
        PosePainting();
        wordRect.SetAsLastSibling();
        ball.rectTransform.SetAsLastSibling();
    }

    void Update()
    {
        float step = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        time += step;
        AdvanceShows(step);

        if (beat == Beat.Wait && time >= WaitSeconds)
            BeginDrop();
        else if (beat == Beat.Drop)
            StepDrop();
        else if (beat == Beat.Hold && time >= HoldSeconds)
            NextLetter();
        else if (beat == Beat.Settle)
            StepSettle();
        else if (beat == Beat.Rest && time >= RestSeconds)
            BeginMorph();
        else if (beat == Beat.Morph)
            StepMorph();

        if (ball == null || !ball.gameObject.activeSelf)
            return;

        if (beat != Beat.Morph)
            ball.rectTransform.SetAsLastSibling();
        PosePainting();
    }

    void BeginDrop()
    {
        Vector2 land = Rest(letter);
        float height = LetterHeight(letter);
        hopUp = LetterUp(letter);
        ballFrom = firstDrop ? land + hopUp * (height * 2.6f) : ball.rectTransform.anchoredPosition;
        ballTo = land;
        arc = firstDrop ? 0f : height * 1.15f;
        time = 0f;
        beat = Beat.Drop;
    }

    void StepDrop()
    {
        float duration = firstDrop ? 0.46f : hopSeconds;
        float t = Mathf.Clamp01(time / duration);
        float along = firstDrop ? t * t : t;
        Vector2 pos = Vector2.Lerp(ballFrom, ballTo, along);
        pos += hopUp * (Mathf.Sin(t * Mathf.PI) * arc);
        ball.rectTransform.anchoredPosition = pos;

        float squash = Mathf.Clamp01((t - 0.78f) / 0.22f);
        squash = Mathf.Sin(squash * Mathf.PI);
        FaceBall(hopUp, squash);
        if (t > 0.8f && shown[letter] <= 0f)
            shown[letter] = 0.01f;

        if (t < 1f)
            return;

        ball.rectTransform.localScale = Vector3.one;
        ball.rectTransform.anchoredPosition = ballTo;
        time = 0f;
        beat = Beat.Hold;
    }

    void NextLetter()
    {
        letter++;
        firstDrop = false;
        if (letter < letters.Count)
        {
            BeginDrop();
            return;
        }

        ballFrom = ball.rectTransform.anchoredPosition;
        ballTo = EndOfWord();
        hopUp = LetterUp(letters.Count - 1);
        arc = LetterHeight(letters.Count - 1) * 0.42f;
        time = 0f;
        beat = Beat.Settle;
    }

    void StepSettle()
    {
        float t = Mathf.Clamp01(time / SettleSeconds);
        Vector2 pos = Vector2.Lerp(ballFrom, ballTo, t);
        pos += hopUp * (Mathf.Sin(t * Mathf.PI) * arc);
        ball.rectTransform.anchoredPosition = pos;

        float squash = Mathf.Sin(Mathf.Clamp01((t - 0.78f) / 0.22f) * Mathf.PI);
        FaceBall(hopUp, squash);
        if (t < 1f)
            return;

        ball.rectTransform.localScale = Vector3.one;
        ball.rectTransform.anchoredPosition = ballTo;
        time = 0f;
        beat = Beat.Rest;
    }

    void BeginMorph()
    {
        ball.rectTransform.localRotation = Quaternion.identity;
        ball.rectTransform.localScale = Vector3.one;
        restPoint = ball.rectTransform.anchoredPosition;
        restDiameter = ball.rectTransform.sizeDelta.x;
        coverDiameter = CoverDiameter(restPoint);

        beginningName.color = ink;
        beginningName.ForceMeshUpdate();
        startPose = ReadPose(beginningName);
        PlaceFree(wordRect);
        startPose.center = WorldCenter(wordRect);
        startPose.localScale = wordRect.localScale;

        fill.enabled = paintingTexture != null;
        ball.rectTransform.SetAsLastSibling();
        wordRect.SetAsLastSibling();
        time = 0f;
        beat = Beat.Morph;
    }

    void StepMorph()
    {
        float u = Ease(Mathf.Clamp01(time / morphSeconds));
        wordRect.localScale = Vector3.LerpUnclamped(startPose.localScale, endPose.localScale, u);
        wordRect.sizeDelta = Vector2.LerpUnclamped(startPose.size, endPose.size, u);
        wordRect.rotation = Quaternion.Slerp(startPose.rotation, endPose.rotation, u);
        beginningName.fontSize = Mathf.LerpUnclamped(startPose.fontSize, endPose.fontSize, u);
        beginningName.characterSpacing = Mathf.LerpUnclamped(startPose.spacing, endPose.spacing, u);

        Vector3 center = Vector3.LerpUnclamped(startPose.center, endPose.center, u);
        wordRect.position += center - WorldCenter(wordRect);

        float diameter = Mathf.LerpUnclamped(restDiameter, coverDiameter, u);
        ball.rectTransform.localScale = Vector3.one;
        ball.rectTransform.anchoredPosition = restPoint;
        ball.rectTransform.sizeDelta = Vector2.one * diameter;
        float reveal = Mathf.InverseLerp(0.18f, 0.5f, u);
        Color cap = ballPink;
        cap.a = 1f - reveal;
        pink.color = cap;
        wordRect.SetAsLastSibling();

        if (u < 1f)
            return;

        paintingPanel.localScale = endPanelScale;
        paintingGroup.alpha = 1f;
        ball.gameObject.SetActive(false);
        wordRect.SetAsLastSibling();
        beat = Beat.Done;
        enabled = false;
    }

    Vector3 endPanelScale;

    Vector3 ScaleOnCanvas(RectTransform rect)
    {
        Vector3 canvasScale = canvasRect.lossyScale;
        Vector3 scale = rect.lossyScale;
        return new Vector3(
            canvasScale.x == 0f ? 1f : scale.x / canvasScale.x,
            canvasScale.y == 0f ? 1f : scale.y / canvasScale.y,
            1f);
    }

    void Resolve()
    {
        Transform root = canvasRect;
        if (paintingPanel == null || beginningPanel == null)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                RectTransform child = root.GetChild(i) as RectTransform;
                if (child == null)
                    continue;
                if (paintingPanel == null && child.GetComponentInChildren<RawImage>(true) != null)
                    paintingPanel = child;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                RectTransform child = root.GetChild(i) as RectTransform;
                if (child == null || child == paintingPanel)
                    continue;
                if (beginningPanel == null && child.GetComponentInChildren<TMP_Text>(true) != null)
                    beginningPanel = child;
            }
        }

        if (beginningName == null && beginningPanel != null)
            beginningName = NameOn(beginningPanel);
        if (paintingName == null && paintingPanel != null)
            paintingName = NameOn(paintingPanel);

        if (paintingPanel != null)
            endPanelScale = paintingPanel.localScale;
    }

    void CenterInPanel(TMP_Text text, RectTransform panel, float tilt)
    {
        RectTransform rect = text.rectTransform;
        Quaternion rotation = rect.localRotation;
        rect.SetParent(panel, false);
        rect.localRotation = rotation;
        rect.localScale = Vector3.one;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = false;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;

        Vector2 room = FitRoom(panel);
        text.ForceMeshUpdate();
        float factor = FitFactor(text.GetPreferredValues(text.text), tilt, room);
        text.fontSize = Mathf.Max(12f, text.fontSize * factor);

        text.ForceMeshUpdate();
        rect.sizeDelta = text.GetPreferredValues(text.text);
        rect.anchoredPosition = Vector2.zero;
    }

    Vector2 FitRoom(RectTransform panel)
    {
        Vector2 panelSize = panel.rect.size;
        Vector2 screen = canvasRect.rect.size;
        return new Vector2(
            Mathf.Max(40f, Mathf.Min(panelSize.x, screen.x) * 0.9f),
            Mathf.Max(40f, Mathf.Min(panelSize.y, screen.y) * 0.5f));
    }

    static float FitFactor(Vector2 size, float tilt, Vector2 room)
    {
        float z = tilt * Mathf.Deg2Rad;
        float cos = Mathf.Abs(Mathf.Cos(z));
        float sin = Mathf.Abs(Mathf.Sin(z));
        float width = Mathf.Max(1f, size.x * cos + size.y * sin);
        float height = Mathf.Max(1f, size.x * sin + size.y * cos);
        return Mathf.Min(room.x / width, room.y / height);
    }

    static float FlatAngle(TMP_Text text)
    {
        return Mathf.Abs(Mathf.DeltaAngle(0f, text.rectTransform.localEulerAngles.z));
    }

    static TMP_Text NameOn(RectTransform panel)
    {
        TMP_Text[] texts = panel.GetComponentsInChildren<TMP_Text>(true);
        TMP_Text largest = null;
        foreach (TMP_Text text in texts)
        {
            if (text.text.IndexOf("cinnario", StringComparison.OrdinalIgnoreCase) >= 0)
                return text;
            if (largest == null || text.fontSize > largest.fontSize)
                largest = text;
        }

        return largest;
    }

    void CollectLetters()
    {
        TMP_TextInfo info = beginningName.textInfo;
        for (int i = 0; i < info.characterCount; i++)
        {
            if (info.characterInfo[i].isVisible)
                letters.Add(i);
        }
    }

    void AdvanceShows(float step)
    {
        if (beat == Beat.Morph || beat == Beat.Done || shown == null)
            return;

        bool moving = false;
        for (int i = 0; i < shown.Length; i++)
        {
            if (shown[i] <= 0f || shown[i] >= 1f)
                continue;
            shown[i] = Mathf.Min(1f, shown[i] + step / 0.12f);
            moving = true;
        }

        if (moving || beat == Beat.Wait)
            ApplyInk();
    }

    void ApplyInk()
    {
        TMP_TextInfo info = beginningName.textInfo;
        for (int i = 0; i < letters.Count; i++)
        {
            TMP_CharacterInfo character = info.characterInfo[letters[i]];
            Color32 color = Color32.Lerp(hidden, ink, shown[i]);
            int vertex = character.vertexIndex;
            Color32[] colors = info.meshInfo[character.materialReferenceIndex].colors32;
            colors[vertex] = color;
            colors[vertex + 1] = color;
            colors[vertex + 2] = color;
            colors[vertex + 3] = color;
        }

        beginningName.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    Vector2 Rest(int index)
    {
        float radius = ball.rectTransform.sizeDelta.x * 0.5f;
        return LetterTop(index) + LetterUp(index) * radius;
    }

    Vector2 EndOfWord()
    {
        int index = letters.Count - 1;
        Vector2 end = GlyphCenter(index);
        Vector2 before = index > 0 ? GlyphCenter(index - 1) : end - Vector2.right;
        Vector2 dir = end - before;
        if (dir.sqrMagnitude < 1f)
            dir = Vector2.right;
        dir.Normalize();

        float reach = 0f;
        Vector2[] corners = GlyphCorners(index);
        for (int i = 0; i < corners.Length; i++)
            reach = Mathf.Max(reach, Vector2.Dot(corners[i] - end, dir));

        float radius = ball.rectTransform.sizeDelta.x * 0.5f;
        return end + dir * (reach + radius + LetterHeight(index) * 0.12f);
    }

    float CoverDiameter(Vector2 center)
    {
        Vector3[] corners = new Vector3[4];
        canvasRect.GetWorldCorners(corners);
        float reach = 0f;
        for (int i = 0; i < corners.Length; i++)
            reach = Mathf.Max(reach, Vector2.Distance(center, ToCanvas(corners[i])));
        return reach * 2f + 12f;
    }

    void PosePainting()
    {
        if (fill == null)
            return;

        Vector2 scale = ball.rectTransform.localScale;
        scale.x = Mathf.Abs(scale.x) < 0.001f ? 1f : scale.x;
        scale.y = Mathf.Abs(scale.y) < 0.001f ? 1f : scale.y;
        Vector2 cover = PaintingCover();
        RectTransform picture = fill.rectTransform;
        picture.anchorMin = picture.anchorMax = new Vector2(0.5f, 0.5f);
        picture.pivot = new Vector2(0.5f, 0.5f);
        picture.anchoredPosition = new Vector2(
            -ball.rectTransform.anchoredPosition.x / scale.x,
            -ball.rectTransform.anchoredPosition.y / scale.y);
        picture.sizeDelta = new Vector2(cover.x / scale.x, cover.y / scale.y);
    }

    Vector2 PaintingCover()
    {
        Rect screen = canvasRect.rect;
        if (paintingTexture == null || paintingTexture.width < 1 || paintingTexture.height < 1)
            return screen.size;

        float scale = Mathf.Max(screen.width / paintingTexture.width, screen.height / paintingTexture.height);
        return new Vector2(paintingTexture.width * scale, paintingTexture.height * scale);
    }

    Texture PaintingTexture()
    {
        RawImage image = paintingPanel.GetComponentInChildren<RawImage>(true);
        return image != null ? image.texture : null;
    }

    Vector2 LetterTop(int index)
    {
        Vector2[] corners = GlyphCorners(index);
        return (corners[1] + corners[2]) * 0.5f;
    }

    Vector2 LetterUp(int index)
    {
        Vector2[] corners = GlyphCorners(index);
        Vector2 up = (corners[1] + corners[2]) * 0.5f - (corners[0] + corners[3]) * 0.5f;
        return up.sqrMagnitude < 1f ? Vector2.up : up.normalized;
    }

    void FaceBall(Vector2 up, float squash)
    {
        float angle = Mathf.Atan2(up.y, up.x) * Mathf.Rad2Deg - 90f;
        ball.rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);
        ball.rectTransform.localScale = new Vector3(1f + 0.16f * squash, 1f - 0.2f * squash, 1f);
    }

    Vector2 GlyphCenter(int index)
    {
        Vector2[] corners = GlyphCorners(index);
        Vector2 center = Vector2.zero;
        for (int i = 0; i < corners.Length; i++)
            center += corners[i];
        return center / corners.Length;
    }

    Vector2[] GlyphCorners(int index)
    {
        TMP_CharacterInfo character = beginningName.textInfo.characterInfo[letters[index]];
        return new[]
        {
            ToCanvas(wordRect.TransformPoint(character.bottomLeft)),
            ToCanvas(wordRect.TransformPoint(new Vector3(character.topLeft.x, character.topLeft.y, 0f))),
            ToCanvas(wordRect.TransformPoint(character.topRight)),
            ToCanvas(wordRect.TransformPoint(new Vector3(character.bottomRight.x, character.bottomRight.y, 0f)))
        };
    }

    float LetterHeight(int index)
    {
        TMP_CharacterInfo character = beginningName.textInfo.characterInfo[letters[index]];
        float x = (character.bottomLeft.x + character.topRight.x) * 0.5f;
        Vector3 top = wordRect.TransformPoint(new Vector3(x, character.topRight.y, 0f));
        Vector3 bottom = wordRect.TransformPoint(new Vector3(x, character.bottomLeft.y, 0f));
        return Vector2.Distance(ToCanvas(top), ToCanvas(bottom));
    }

    Vector2 ToCanvas(Vector3 world)
    {
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            RectTransformUtility.WorldToScreenPoint(camera, world),
            camera,
            out Vector2 local);
        return local;
    }

    Image MakeBall()
    {
        var obj = new GameObject("Ball", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(canvasRect, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        float height = letters.Count > 0 ? LetterHeight(0) : 48f;
        rect.sizeDelta = Vector2.one * height * 0.72f;
        Image image = obj.GetComponent<Image>();
        image.sprite = Circle();
        image.color = Color.white;
        image.raycastTarget = false;
        Mask mask = obj.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        var picture = new GameObject("Painting", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        picture.transform.SetParent(obj.transform, false);
        fill = picture.GetComponent<RawImage>();
        fill.texture = paintingTexture;
        fill.color = Color.white;
        fill.raycastTarget = false;
        fill.enabled = false;

        var cap = new GameObject("Pink", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        cap.transform.SetParent(obj.transform, false);
        RectTransform capRect = cap.GetComponent<RectTransform>();
        capRect.anchorMin = Vector2.zero;
        capRect.anchorMax = Vector2.one;
        capRect.offsetMin = Vector2.zero;
        capRect.offsetMax = Vector2.zero;
        pink = cap.GetComponent<Image>();
        pink.sprite = Circle();
        pink.color = ballPink;
        pink.raycastTarget = false;
        if (paintingTexture == null)
            mask.enabled = false;

        return image;
    }

    static Sprite circle;

    static Sprite Circle()
    {
        if (circle != null)
            return circle;

        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        float radius = (size - 1) * 0.5f;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - radius;
                float dy = y - radius;
                float edge = radius - Mathf.Sqrt(dx * dx + dy * dy);
                pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(edge));
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        circle = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        return circle;
    }

    static Pose ReadPose(TMP_Text text)
    {
        RectTransform rect = text.rectTransform;
        return new Pose
        {
            center = WorldCenter(rect),
            rotation = rect.rotation,
            size = rect.rect.size,
            localScale = rect.localScale,
            fontSize = text.fontSize,
            spacing = text.characterSpacing
        };
    }

    void PlaceFree(RectTransform rect)
    {
        Vector2 size = rect.rect.size;
        Vector3 center = WorldCenter(rect);
        Quaternion rotation = rect.rotation;
        Vector3 scale = rect.lossyScale;
        rect.SetParent(canvasRect, true);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = new Vector3(
            scale.x / canvasRect.lossyScale.x,
            scale.y / canvasRect.lossyScale.y,
            1f);
        rect.sizeDelta = size;
        rect.rotation = rotation;
        rect.position += center - WorldCenter(rect);
    }

    static Vector3 WorldCenter(RectTransform rect)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return (corners[0] + corners[2]) * 0.5f;
    }

    static Color PanelColor(RectTransform panel)
    {
        Image image = panel.GetComponent<Image>();
        if (image == null)
            return Color.white;
        Color color = image.color;
        color.a = 1f;
        return color;
    }

    static bool SameColor(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.04f
            && Mathf.Abs(a.g - b.g) < 0.04f
            && Mathf.Abs(a.b - b.b) < 0.04f;
    }

    static bool Colorful(Color color)
    {
        float max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
        float min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
        return max - min > 0.18f;
    }

    static float Ease(float t)
    {
        return t * t * (3f - 2f * t);
    }
}
