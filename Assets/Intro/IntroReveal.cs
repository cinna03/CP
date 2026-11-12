using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Plays the opening on the panels already under the canvas.
// The ball paints Cinnarios, rests as a period, and the word turns upright.
// A tap then opens the first intro panel from inside the ball.
// Another tap slides to the next intro panel from the right. Each later panel uses that same slide.
[AddComponentMenu("Intro/Intro Reveal")]
public class IntroReveal : MonoBehaviour
{
    [SerializeField] RectTransform beginningPanel;
    [SerializeField] TMP_Text beginningName;
    [SerializeField] RectTransform paintingPanel;
    [SerializeField] RectTransform nextPanel;
    [SerializeField] TMP_Text paintingName;
    [SerializeField] Image bubble;
    [SerializeField] float hopSeconds = 0.34f;
    [SerializeField] float morphSeconds = 1.05f;

    Canvas canvas;
    RectTransform canvasRect;
    RectTransform wordRect;
    Image ball;
    CanvasGroup paintingGroup;

    Color hidden;
    Color ink;
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

    const float OpenSeconds = 0.08f;
    const float HoldSeconds = 0.06f;
    const float SettleSeconds = 0.36f;
    const float RestSeconds = 0.18f;
    const float PeriodSeconds = 0.34f;
    const float TiltSeconds = 0.9f;
    const float PeriodScale = 0.16f;
    const float FillSeconds = 0.9f;
    const float SlideSeconds = 1.15f;

    float fillScale = 1f;
    float fillFrom = 1f;
    float tiltFrom;
    float slideWidth;
    RectTransform window;
    RectTransform leavingSlot;
    RectTransform arrivingSlot;
    Vector2 revealedSize;
    Vector3 revealedCenter;
    Vector3 revealedScale;
    Quaternion revealedRotation;
    PanelPlace savedPanel;
    PanelPlace savedNext;
    RectTransform revealTarget;
    RectTransform returnPanel;
    CanvasGroup revealGroup;
    bool returnTrip;
    bool closing;
    bool copyOpening;
    readonly List<RectTransform> intros = new List<RectTransform>();
    int introIndex;
    RectTransform leavingPanel;

    enum Beat { Wait, Drop, Hold, Settle, Rest, Period, Tilt, Ready, Fill, Shown, Slide, ReturnReady, Done }

    struct PanelPlace
    {
        public Transform parent;
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public Vector3 localScale;
        public Quaternion localRotation;
    }

    public static void OpenMenu()
    {
        MainMenu.TakeOver();
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
        MainMenu.Hide();

        Resolve();
        if (beginningPanel == null || beginningName == null || paintingPanel == null || paintingName == null)
        {
            Debug.LogWarning("Intro Reveal needs the plain panel, its Cinnarios text, the painting panel, and the Cinnarios text on that painting.");
            enabled = false;
            return;
        }

        paintingPanel.gameObject.SetActive(true);
        for (int i = 0; i < paintingPanel.childCount; i++)
            paintingPanel.GetChild(i).gameObject.SetActive(true);
        CollectIntros();

        PaintWhite(beginningPanel, PhotoWhite);
        PaintWhite(paintingPanel, PhotoWhite);
        CopyObjectSizes();
        hidden = PanelColor(beginningPanel);
        ink = paintingName.color;
        if (ink.a < 0.2f || (ink.r > 0.97f && ink.g > 0.97f && ink.b > 0.97f))
            ink = BrushPink;

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
        if (shown.Length > 0)
            shown[0] = 0.45f;
        ApplyInk();

        ball = MakeBall();
        ball.rectTransform.localScale = Vector3.one * PeriodScale;
        ball.rectTransform.anchoredPosition = StartOfWord();
        wordRect.SetAsLastSibling();
        ball.rectTransform.SetAsLastSibling();
        WarmWindow();
        BeginDrop();
        time = 0.05f;
        StepDrop();
    }

    void Update()
    {
        float step = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        time += step;
        AdvanceShows(step);

        if (beat == Beat.Wait && time >= OpenSeconds)
            BeginDrop();
        else if (beat == Beat.Drop)
            StepDrop();
        else if (beat == Beat.Hold && time >= HoldSeconds)
            NextLetter();
        else if (beat == Beat.Settle)
            StepSettle();
        else if (beat == Beat.Rest && time >= RestSeconds)
            BeginPeriod();
        else if (beat == Beat.Period)
            StepPeriod();
        else if (beat == Beat.Tilt)
            StepTilt();
        else if (beat == Beat.Ready)
        {
            if (ScreenAdvance.Tapped())
            {
                if (returnTrip)
                    AskForEntrance();
                else
                    BeginFill();
            }
        }
        else if (beat == Beat.Fill)
            StepFill();
        else if (beat == Beat.Shown)
        {
            if (ScreenAdvance.Tapped())
            {
                if (introIndex + 1 < intros.Count)
                    BeginSlide();
                else
                    BeginReturn();
            }
        }
        else if (beat == Beat.Slide)
            StepSlide();
        else if (beat == Beat.ReturnReady)
        {
            if (ScreenAdvance.Tapped())
                BeginReturn();
        }

    }

    void BeginDrop()
    {
        Vector2 land = Rest(letter);
        float height = LetterHeight(letter);
        hopUp = LetterUp(letter);
        ballFrom = ball.rectTransform.anchoredPosition;
        ballTo = land;
        arc = firstDrop ? height * 0.28f : height * 1.15f;
        time = 0f;
        beat = Beat.Drop;
        if (!copyOpening)
            return;

        copyOpening = false;
        time = 0.02f;
        if (shown != null && letter < shown.Length && shown[letter] <= 0f)
            shown[letter] = 0.01f;
        StepDrop();
    }

    void StepDrop()
    {
        float duration = firstDrop ? 0.26f : hopSeconds;
        float t = Mathf.Clamp01(time / duration);
        float along = firstDrop ? EaseOut(t) : t;
        Vector2 pos = Vector2.Lerp(ballFrom, ballTo, along);
        pos += hopUp * (Mathf.Sin(t * Mathf.PI) * arc);
        ball.rectTransform.anchoredPosition = pos;

        float squash = Mathf.Clamp01((t - 0.78f) / 0.22f);
        squash = Mathf.Sin(squash * Mathf.PI);
        float size = firstDrop ? Mathf.Lerp(PeriodScale, 1f, EaseOut(t)) : 1f;
        FaceBall(hopUp, squash, size);
        if (t > 0.28f && shown[letter] <= 0f)
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

    void BeginPeriod()
    {
        ball.rectTransform.localRotation = Quaternion.identity;
        time = 0f;
        beat = Beat.Period;
    }

    void StepPeriod()
    {
        float u = Ease(Mathf.Clamp01(time / PeriodSeconds));
        float size = Mathf.Lerp(1f, PeriodScale, u);
        ball.rectTransform.localScale = Vector3.one * size;
        ball.rectTransform.localRotation = Quaternion.identity;
        if (u < 1f)
            return;

        ball.rectTransform.localScale = Vector3.one * PeriodScale;
        BeginTilt();
    }

    void BeginTilt()
    {
        tiltFrom = wordRect.localEulerAngles.z;
        ball.rectTransform.SetParent(wordRect, true);
        ball.rectTransform.SetAsLastSibling();
        time = 0f;
        beat = Beat.Tilt;
    }

    void StepTilt()
    {
        float u = Smoother(Mathf.Clamp01(time / TiltSeconds));
        float z = Mathf.LerpAngle(tiltFrom, 0f, u);
        wordRect.localRotation = Quaternion.Euler(0f, 0f, z);
        if (u < 1f)
            return;

        wordRect.localRotation = Quaternion.identity;
        ReleaseBall();
        time = 0f;
        beat = Beat.Ready;
    }

    void ReleaseBall()
    {
        Vector3 world = ball.rectTransform.position;
        ball.rectTransform.SetParent(canvasRect, false);
        ball.rectTransform.anchorMin = ball.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        ball.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        ball.rectTransform.localRotation = Quaternion.identity;
        ball.rectTransform.localScale = Vector3.one * PeriodScale;
        ball.rectTransform.position = world;
    }

    void AskForEntrance()
    {
        enabled = false;
        AccessGate.Present(null, beginningName != null ? beginningName.font : null);
    }

    void BeginFill()
    {
        returnTrip = false;
        ball.rectTransform.localRotation = Quaternion.identity;
        fillFrom = Mathf.Max(0.05f, ball.rectTransform.localScale.x);
        beginningName.color = ink;
        OpenPanel(paintingPanel);
    }

    void BeginReturn()
    {
        if (returnPanel == null)
            returnPanel = FindBeginningCopy();
        if (returnPanel == null)
            return;

        MatchBeginningWhite(returnPanel);
        beginningPanel.gameObject.SetActive(false);
        returnPanel.gameObject.SetActive(true);
        HideCopyLetters();
        returnTrip = true;
        closing = true;

        RectTransform leaving = introIndex >= 0 && introIndex < intros.Count ? intros[introIndex] : null;
        if (leaving == null)
            return;

        ClosePanel(leaving);
    }

    void OpenPanel(RectTransform panel)
    {
        revealTarget = panel;
        revealTarget.gameObject.SetActive(true);
        Canvas.ForceUpdateCanvases();
        revealGroup = GroupOn(revealTarget);

        savedPanel = new PanelPlace
        {
            parent = revealTarget.parent,
            anchorMin = revealTarget.anchorMin,
            anchorMax = revealTarget.anchorMax,
            pivot = revealTarget.pivot,
            anchoredPosition = revealTarget.anchoredPosition,
            sizeDelta = revealTarget.sizeDelta,
            localScale = revealTarget.localScale,
            localRotation = revealTarget.localRotation
        };
        revealedSize = revealTarget.rect.size;
        revealedCenter = revealTarget.TransformPoint(revealTarget.rect.center);
        revealedScale = revealTarget.localScale;
        revealedRotation = revealTarget.localRotation;

        WarmWindow();
        window.gameObject.SetActive(true);
        window.SetParent(canvasRect, false);
        window.sizeDelta = ball.rectTransform.sizeDelta;
        window.anchoredPosition = ball.rectTransform.anchoredPosition;
        window.localRotation = Quaternion.identity;
        window.localScale = new Vector3(fillFrom, fillFrom, 1f);

        revealGroup.alpha = 1f;
        revealGroup.blocksRaycasts = false;
        revealTarget.SetParent(window, false);
        revealTarget.anchorMin = revealTarget.anchorMax = new Vector2(0.5f, 0.5f);
        revealTarget.pivot = new Vector2(0.5f, 0.5f);
        revealTarget.sizeDelta = revealedSize;
        revealTarget.localRotation = revealedRotation;
        HoldPanelStill(fillFrom);

        fillScale = CoverScale(window.anchoredPosition);
        ball.rectTransform.SetAsLastSibling();
        time = 0.16f;
        beat = Beat.Fill;
        StepFill();
    }

    void WarmWindow()
    {
        if (window != null)
            return;

        var host = new GameObject("PanelWindow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        host.transform.SetParent(canvasRect, false);
        Image opening = host.GetComponent<Image>();
        opening.sprite = CircleSprite();
        opening.color = Color.white;
        opening.raycastTarget = false;
        opening.preserveAspect = true;
        host.GetComponent<Mask>().showMaskGraphic = false;

        window = host.GetComponent<RectTransform>();
        window.anchorMin = window.anchorMax = new Vector2(0.5f, 0.5f);
        window.pivot = new Vector2(0.5f, 0.5f);
        window.sizeDelta = Vector2.one * 32f;
        window.anchoredPosition = new Vector2(8000f, 8000f);
        window.localScale = Vector3.one;
        host.SetActive(false);
    }

    void ClosePanel(RectTransform panel)
    {
        revealTarget = panel;
        revealTarget.gameObject.SetActive(true);
        revealGroup = GroupOn(revealTarget);
        savedPanel = new PanelPlace
        {
            parent = revealTarget.parent,
            anchorMin = revealTarget.anchorMin,
            anchorMax = revealTarget.anchorMax,
            pivot = revealTarget.pivot,
            anchoredPosition = revealTarget.anchoredPosition,
            sizeDelta = revealTarget.sizeDelta,
            localScale = revealTarget.localScale,
            localRotation = revealTarget.localRotation
        };
        revealedSize = revealTarget.rect.size;
        revealedCenter = revealTarget.TransformPoint(revealTarget.rect.center);
        revealedScale = revealTarget.localScale;
        revealedRotation = revealTarget.localRotation;

        WarmWindow();
        window.gameObject.SetActive(true);
        window.SetParent(canvasRect, false);
        window.anchorMin = window.anchorMax = new Vector2(0.5f, 0.5f);
        window.pivot = new Vector2(0.5f, 0.5f);
        window.sizeDelta = ball != null ? ball.rectTransform.sizeDelta : Vector2.one * 220f;
        window.anchoredPosition = Vector2.zero;
        window.localRotation = Quaternion.identity;

        fillFrom = CoverScale(Vector2.zero);
        fillScale = 0.18f;
        window.localScale = new Vector3(fillFrom, fillFrom, 1f);

        revealGroup.alpha = 1f;
        revealGroup.blocksRaycasts = false;
        revealTarget.SetParent(window, false);
        revealTarget.anchorMin = revealTarget.anchorMax = new Vector2(0.5f, 0.5f);
        revealTarget.pivot = new Vector2(0.5f, 0.5f);
        revealTarget.sizeDelta = revealedSize;
        revealTarget.localRotation = revealedRotation;
        HoldPanelStill(fillFrom);

        returnPanel.SetSiblingIndex(window.GetSiblingIndex());
        window.SetAsLastSibling();
        if (ball != null)
            ball.gameObject.SetActive(false);

        time = 0f;
        beat = Beat.Fill;
    }

    void StepFill()
    {
        float duration = FillSeconds;
        float u = EaseOut(Mathf.Clamp01(time / duration));
        float scale = Mathf.Lerp(fillFrom, fillScale, u);
        window.localScale = new Vector3(scale, scale, 1f);
        HoldPanelStill(scale);

        if (!closing && ball != null && ball.gameObject.activeSelf)
        {
            ball.rectTransform.anchoredPosition = window.anchoredPosition;
            ball.rectTransform.localRotation = Quaternion.identity;
            ball.rectTransform.localScale = new Vector3(scale, scale, 1f);
            Color bubble = Color.white;
            bubble.a = 1f - Ease(Mathf.Clamp01(time / 0.32f));
            ball.color = bubble;
            if (bubble.a <= 0.001f)
                ball.gameObject.SetActive(false);
        }

        if (u < 1f)
            return;

        HoldPainting();
        time = 0f;
        closing = false;
        if (returnTrip)
        {
            if (introIndex >= 0 && introIndex < intros.Count)
                intros[introIndex].gameObject.SetActive(false);
            PlayCopy();
            return;
        }

        beat = Beat.Shown;
    }

    void CopyObjectSizes()
    {
        if (returnPanel == null || beginningPanel == null)
            return;

        beginningPanel.localScale = returnPanel.localScale;
        CopyChildSizes(returnPanel, beginningPanel);
    }

    static void CopyChildSizes(RectTransform from, RectTransform to)
    {
        for (int i = 0; i < from.childCount; i++)
        {
            RectTransform source = from.GetChild(i) as RectTransform;
            if (source == null)
                continue;

            RectTransform dest = to.Find(source.name) as RectTransform;
            if (dest == null)
                continue;

            dest.localScale = source.localScale;
            dest.sizeDelta = source.sizeDelta;
            TMP_Text sourceText = source.GetComponent<TMP_Text>();
            TMP_Text destText = dest.GetComponent<TMP_Text>();
            if (sourceText != null && destText != null)
            {
                destText.enableAutoSizing = false;
                destText.fontSize = sourceText.fontSize;
            }

            CopyChildSizes(source, dest);
        }
    }

    void HideCopyLetters()
    {
        TMP_Text title = NameOn(returnPanel);
        if (title == null)
            return;

        beginningName = title;
        wordRect = title.rectTransform;
        hidden = PanelColor(returnPanel);
        title.color = Color.white;
        title.ForceMeshUpdate();
        letters.Clear();
        CollectLetters();
        if (letters.Count == 0)
            return;

        shown = new float[letters.Count];
        ApplyInk();
    }

    void PlayCopy()
    {
        if (returnPanel == null)
            returnPanel = FindBeginningCopy();
        if (returnPanel == null)
        {
            beat = Beat.Done;
            enabled = false;
            return;
        }

        returnPanel.gameObject.SetActive(true);
        returnPanel.SetAsLastSibling();
        MatchBeginningWhite(returnPanel);
        TMP_Text title = NameOn(returnPanel);
        if (title == null)
        {
            beat = Beat.Done;
            enabled = false;
            return;
        }

        Transform dot = returnPanel.Find("Ball");
        if (dot != null && (ball == null || dot.gameObject != ball.gameObject))
            dot.gameObject.SetActive(false);

        beginningName = title;
        wordRect = title.rectTransform;
        hidden = PanelColor(returnPanel);
        title.color = Color.white;
        title.ForceMeshUpdate();
        letters.Clear();
        CollectLetters();
        if (letters.Count == 0)
        {
            beat = Beat.Done;
            enabled = false;
            return;
        }

        shown = new float[letters.Count];
        shown[0] = 0.01f;
        ApplyInk();
        letter = 0;
        firstDrop = true;
        copyOpening = true;
        if (ball != null)
        {
            ball.gameObject.SetActive(true);
            ball.color = Color.white;
            ball.rectTransform.SetParent(canvasRect, false);
            ball.rectTransform.anchorMin = ball.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            ball.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            ball.rectTransform.localRotation = Quaternion.identity;
            float height = letters.Count > 0 ? LetterHeight(0) : 48f;
            ball.rectTransform.sizeDelta = Vector2.one * height * 1.35f;
            ball.rectTransform.localScale = Vector3.one * PeriodScale;
            ball.rectTransform.anchoredPosition = StartOfWord();
            wordRect.SetAsLastSibling();
            ball.rectTransform.SetAsLastSibling();
        }

        time = 0f;
        BeginDrop();
    }

    void HoldPanelStill(float scale)
    {
        float inv = 1f / Mathf.Max(0.001f, scale);
        revealTarget.localScale = new Vector3(revealedScale.x * inv, revealedScale.y * inv, revealedScale.z);
        revealTarget.position = revealedCenter;
    }

    void HoldPainting()
    {
        revealTarget.SetParent(savedPanel.parent, false);
        revealTarget.anchorMin = savedPanel.anchorMin;
        revealTarget.anchorMax = savedPanel.anchorMax;
        revealTarget.pivot = savedPanel.pivot;
        revealTarget.anchoredPosition = savedPanel.anchoredPosition;
        revealTarget.sizeDelta = savedPanel.sizeDelta;
        revealTarget.localRotation = savedPanel.localRotation;
        revealTarget.localScale = savedPanel.localScale;
        revealTarget.SetAsLastSibling();
        revealGroup.alpha = 1f;
        revealGroup.blocksRaycasts = false;

        if (window != null)
            window.gameObject.SetActive(false);
        if (ball != null)
            ball.gameObject.SetActive(false);
    }

    void BeginSlide()
    {
        if (introIndex + 1 >= intros.Count)
            return;

        leavingPanel = intros[introIndex];
        RectTransform arriving = intros[introIndex + 1];
        savedNext = Capture(arriving);
        Canvas.ForceUpdateCanvases();
        slideWidth = canvasRect.rect.width;
        if (slideWidth < 2f)
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            slideWidth = scaler != null ? scaler.referenceResolution.x : Screen.width;
        }

        leavingSlot = MakeSlot(0f);
        arrivingSlot = MakeSlot(slideWidth);
        arriving.gameObject.SetActive(true);
        leavingPanel.SetParent(leavingSlot, false);
        arriving.SetParent(arrivingSlot, false);
        arrivingSlot.SetAsLastSibling();
        time = 0f;
        beat = Beat.Slide;
    }

    void StepSlide()
    {
        float u = Smoother(Mathf.Clamp01(time / SlideSeconds));
        float x = Mathf.Lerp(0f, -slideWidth, u);
        SetSlotX(leavingSlot, x);
        SetSlotX(arrivingSlot, x + slideWidth);
        if (u < 1f)
            return;

        RectTransform arriving = intros[introIndex + 1];
        Apply(arriving, savedNext);
        arriving.SetAsLastSibling();
        leavingPanel.SetParent(canvasRect, false);
        leavingPanel.gameObject.SetActive(false);
        if (leavingSlot != null)
            Destroy(leavingSlot.gameObject);
        if (arrivingSlot != null)
            Destroy(arrivingSlot.gameObject);
        introIndex++;
        if (introIndex + 1 < intros.Count)
        {
            time = 0f;
            beat = Beat.Shown;
            return;
        }

        if (returnPanel != null)
        {
            time = 0f;
            beat = Beat.ReturnReady;
            return;
        }

        beat = Beat.Done;
        enabled = false;
    }

    RectTransform MakeSlot(float x)
    {
        var rect = new GameObject("SlideSlot", typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(canvasRect, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        SetSlotX(rect, x);
        return rect;
    }

    static void SetSlotX(RectTransform slot, float x)
    {
        slot.offsetMin = new Vector2(x, 0f);
        slot.offsetMax = new Vector2(x, 0f);
    }

    static PanelPlace Capture(RectTransform rect)
    {
        return new PanelPlace
        {
            parent = rect.parent,
            anchorMin = rect.anchorMin,
            anchorMax = rect.anchorMax,
            pivot = rect.pivot,
            anchoredPosition = rect.anchoredPosition,
            sizeDelta = rect.sizeDelta,
            localScale = rect.localScale,
            localRotation = rect.localRotation
        };
    }

    static void Apply(RectTransform rect, PanelPlace place)
    {
        rect.SetParent(place.parent, false);
        rect.anchorMin = place.anchorMin;
        rect.anchorMax = place.anchorMax;
        rect.pivot = place.pivot;
        rect.anchoredPosition = place.anchoredPosition;
        rect.sizeDelta = place.sizeDelta;
        rect.localRotation = place.localRotation;
        rect.localScale = place.localScale;
    }

    Vector2 StartOfWord()
    {
        Vector2 start = GlyphCenter(0);
        Vector2 next = letters.Count > 1 ? GlyphCenter(1) : start + Vector2.right;
        Vector2 dir = start - next;
        if (dir.sqrMagnitude < 1f)
            dir = -Vector2.right;
        dir.Normalize();

        float reach = 0f;
        Vector2[] corners = GlyphCorners(0);
        for (int i = 0; i < corners.Length; i++)
            reach = Mathf.Max(reach, Vector2.Dot(corners[i] - start, dir));

        float radius = ball.rectTransform.sizeDelta.x * 0.5f * PeriodScale;
        return start + dir * (reach + radius + LetterHeight(0) * 0.2f);
    }

    float CoverScale(Vector2 center)
    {
        Vector3[] corners = new Vector3[4];
        canvasRect.GetWorldCorners(corners);
        float reach = 0f;
        for (int i = 0; i < corners.Length; i++)
            reach = Mathf.Max(reach, Vector2.Distance(center, ToCanvas(corners[i])));

        float radius = ball.rectTransform.sizeDelta.x * 0.5f;
        return reach * 1.08f / Mathf.Max(1f, radius);
    }

    static readonly Color32 PhotoWhite = new Color32(255, 255, 255, 255);
    static readonly Color BrushPink = new Color32(255, 183, 208, 255);

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
        CollectIntros();
    }

    void CollectIntros()
    {
        intros.Clear();
        if (paintingPanel != null)
            intros.Add(paintingPanel);
        if (nextPanel == null)
            nextPanel = FindIntro("2");
        if (nextPanel != null && !intros.Contains(nextPanel))
            intros.Add(nextPanel);
        RectTransform third = FindIntro("3");
        if (third != null && !intros.Contains(third))
            intros.Add(third);
        RectTransform fourth = FindIntro("4");
        if (fourth != null && !intros.Contains(fourth))
            intros.Add(fourth);
        returnPanel = FindBeginningCopy();
        for (int i = 1; i < intros.Count; i++)
            intros[i].gameObject.SetActive(false);
        if (returnPanel != null)
        {
            MatchBeginningWhite(returnPanel);
            returnPanel.gameObject.SetActive(false);
        }
        introIndex = 0;
    }

    RectTransform FindBeginningCopy()
    {
        RectTransform found = MatchCopy(canvas.GetComponentsInChildren<RectTransform>(true));
        if (found != null)
            return found;

        return MatchCopy(FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None));
    }

    RectTransform MatchCopy(RectTransform[] rects)
    {
        RectTransform found = null;
        for (int i = 0; i < rects.Length; i++)
        {
            RectTransform rect = rects[i];
            if (rect == null || rect == canvasRect || rect == beginningPanel)
                continue;
            string compact = rect.name.Replace(" ", "").Replace("(", "").Replace(")", "");
            if (compact.IndexOf("BeginningPanel", StringComparison.OrdinalIgnoreCase) >= 0)
                found = rect;
        }

        return found;
    }

    CanvasGroup GroupOn(RectTransform panel)
    {
        if (panel == paintingPanel && paintingGroup != null)
            return paintingGroup;

        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        if (group == null)
            group = panel.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        return group;
    }

    RectTransform FindIntro(string number)
    {
        RectTransform[] rects = canvas.GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < rects.Length; i++)
        {
            RectTransform rect = rects[i];
            if (rect == canvasRect || rect == beginningPanel)
                continue;
            string compact = rect.name.Replace(" ", "");
            if (compact.Equals("IntroPanel" + number, StringComparison.OrdinalIgnoreCase))
                return rect;
        }

        return null;
    }

    static TMP_Text NameOn(RectTransform panel)
    {
        TMP_Text[] texts = panel.GetComponentsInChildren<TMP_Text>(true);
        TMP_Text named = null;
        TMP_Text largest = null;
        foreach (TMP_Text text in texts)
        {
            if (text.gameObject.name == "Slogan")
                continue;
            if (text.gameObject.name == "App Name")
                return text;
            if (named == null && text.text.IndexOf("cinnario", StringComparison.OrdinalIgnoreCase) >= 0)
                named = text;
            if (largest == null || text.fontSize > largest.fontSize)
                largest = text;
        }

        return named != null ? named : largest;
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
        if (beat == Beat.Fill || beat == Beat.Done || beat == Beat.Ready || shown == null)
            return;

        bool moving = false;
        for (int i = 0; i < shown.Length; i++)
        {
            if (shown[i] <= 0f || shown[i] >= 1f)
                continue;
            shown[i] = Mathf.Min(1f, shown[i] + step / 0.12f);
            moving = true;
        }

        if (moving)
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

    static Sprite solidWhite;

    static void MatchBeginningWhite(RectTransform panel)
    {
        Image image = panel.GetComponent<Image>();
        if (image != null && image.sprite != null && image.sprite != solidWhite && image.sprite.name != "Background")
            image.sprite = null;
        PaintWhite(panel, PhotoWhite);
    }

    static void PaintWhite(RectTransform panel, Color32 color)
    {
        if (panel.GetComponent<RawImage>() != null)
            return;

        Image image = panel.GetComponent<Image>();
        if (image != null && image.sprite != null && image.sprite.name != "Background")
            return;
        if (image == null)
            image = panel.gameObject.AddComponent<Image>();

        if (solidWhite == null)
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color32[16];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(pixels);
            tex.Apply();
            solidWhite = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
        }

        image.sprite = solidWhite;
        image.type = Image.Type.Simple;
        image.color = color;
        image.preserveAspect = false;
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

    void FaceBall(Vector2 up, float squash, float size = 1f)
    {
        float angle = Mathf.Atan2(up.y, up.x) * Mathf.Rad2Deg - 90f;
        ball.rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);
        ball.rectTransform.localScale = new Vector3(1f + 0.16f * squash, 1f - 0.2f * squash, 1f) * size;
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
        Image image = bubble;
        if (image == null && beginningPanel != null)
        {
            Transform found = beginningPanel.Find("Ball");
            if (found != null)
                image = found.GetComponent<Image>();
        }

        GameObject obj;
        if (image == null)
        {
            obj = new GameObject("Ball", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(canvasRect, false);
            image = obj.GetComponent<Image>();
            image.sprite = Resources.Load<Sprite>("Bubble");
            image.color = Color.white;
        }
        else
        {
            obj = image.gameObject;
            obj.transform.SetParent(canvasRect, true);
            if (image.sprite == null)
                image.sprite = Resources.Load<Sprite>("Bubble");
        }

        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        float height = letters.Count > 0 ? LetterHeight(0) : 48f;
        rect.sizeDelta = Vector2.one * height * 1.35f;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = Color.white;

        Transform cover = obj.transform.Find("Reveal");
        if (cover != null)
            Destroy(cover.gameObject);

        Mask mask = image.GetComponent<Mask>();
        if (mask != null)
            mask.enabled = false;

        return image;
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

    static float Ease(float t)
    {
        return t * t * (3f - 2f * t);
    }

    static float EaseOut(float t)
    {
        float u = 1f - Mathf.Clamp01(t);
        return 1f - u * u * u;
    }

    static float Smoother(float t)
    {
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }

    static Sprite circleSprite;

    static Sprite CircleSprite()
    {
        if (circleSprite != null)
            return circleSprite;

        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color32[size * size];
        float radius = (size - 2) * 0.5f;
        var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                byte alpha = (byte)(Mathf.Clamp01(radius - distance) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        circleSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        return circleSprite;
    }
}
