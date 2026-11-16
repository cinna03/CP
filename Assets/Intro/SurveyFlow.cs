using System.Collections;
using System.Collections.Generic;
using Capstone.App.Data;
using Capstone.App.Services;
using Capstone.App.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "After Visit": the wrap-up check, opened by Finish Visit. One question per card on the app's paper
/// background, sliding in from the right. Screen 3 quotes the buyer's own blocker; screen 4 opens an
/// optional follow-up only for buyers who are still unsure. It ends on a before -> now summary.
/// </summary>
public class SurveyFlow : MonoBehaviour
{
    static readonly Color Paper = new Color32(0xFC, 0xFB, 0xFA, 0xFF);
    static readonly Color Ink = new Color32(0x2B, 0x21, 0x24, 0xFF);
    static readonly Color Stone = new Color32(0x6B, 0x53, 0x59, 0xFF);
    static readonly Color Rose = new Color32(0xB8, 0x5C, 0x74, 0xFF);
    static readonly Color Petal = new Color32(0xF7, 0xEC, 0xEF, 0xFF);
    const float SlideSeconds = 0.42f;
    const float FollowUpHeight = 300f;

    static SurveyFlow live;

    RectTransform page;
    QuestionCardView card;
    StudyQuestion[] after;
    string preBlocker;
    int index;
    readonly HashSet<string> tags = new HashSet<string>();
    TMP_InputField comment;
    bool followUpShown;

    /// <summary>Kept for older callers: the "before" questions now live on the intro slides.</summary>
    public static void OpenPre()
    {
    }

    public static void OpenPost()
    {
        SurveyDataManager.Ensure();
        if (live == null)
            live = BuildHost();
        live.gameObject.SetActive(true);
        live.transform.SetAsLastSibling();
        live.Begin();
    }

    public static void StartSurvey(bool isPostAR)
    {
        if (isPostAR)
            OpenPost();
    }

    static SurveyFlow BuildHost()
    {
        var host = new GameObject("Survey", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(SurveyFlow));
        Canvas canvas = host.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;
        CanvasScaler scaler = host.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0f;
        DontDestroyOnLoad(host);
        return host.GetComponent<SurveyFlow>();
    }

    void Begin()
    {
        if (page != null)
            Destroy(page.gameObject);
        page = Fill("Page", transform, Paper);
        Heading(page, "Before you go", "Four quick taps so we know whether this visit helped.");
        preBlocker = SurveyDataManager.Instance.PreBlocker();
        after = StudyQuestions.After(preBlocker);
        tags.Clear();
        comment = null;
        followUpShown = false;
        index = 0;
        card = null;
        Ask();
    }

    void Ask()
    {
        StudyQuestion question = after[index];
        bool last = index == after.Length - 1;
        string prompt = question is ChoiceQuestion && question.Id == StudyQuestions.BlockerResolution.Id
            ? StudyQuestions.ResolutionPrompt(preBlocker)
            : null;
        QuestionCardView previous = card;
        card = QuestionCardView.Show(page, question, prompt, index + 1, after.Length,
            answer => Answered(question, answer),
            last, last ? (System.Action<int>)(pick => Picked(question, pick)) : null);
        if (previous != null)
            StartCoroutine(Slide(previous, card));
    }

    void Answered(StudyQuestion question, int answer)
    {
        SurveyDataManager.Instance.Record(question, answer);
        index++;
        if (index < after.Length)
        {
            Ask();
            return;
        }

        string note = comment != null ? comment.text : null;
        StudySession session = SurveyDataManager.Instance.SaveAfter(tags, note);
        ShowResult(session);
    }

    /// <summary>On the last screen: open the optional follow-up for buyers who are still unsure.</summary>
    void Picked(StudyQuestion question, int pick)
    {
        SurveyDataManager.Instance.Record(question, pick);
        if (followUpShown)
            return;
        int confidence = SurveyDataManager.Instance.Answer(StudyQuestions.PostConfidence);
        string resolution = StudyQuestions.BlockerResolution.Key(SurveyDataManager.Instance.Answer(StudyQuestions.BlockerResolution));
        if (!StudyQuestions.NeedsFollowUp(confidence, resolution))
            return;
        followUpShown = true;
        card.Reveal(FollowUp(), FollowUpHeight);
    }

    RectTransform FollowUp()
    {
        var section = new GameObject("Follow-up", typeof(RectTransform));
        var rect = (RectTransform)section.transform;
        rect.SetParent(page, false);
        float w = card.Width - 96f;
        QuestionCardView.Label(rect, "Mind sharing what held you back? (Optional)", AppType.BodyFont, 26f, Stone,
            new Vector2(0f, 0f), new Vector2(w, 40f), TextAlignmentOptions.Left);

        float chipW = (w - 12f) / 2f;
        for (int i = 0; i < StudyQuestions.OutlierTags.Length; i++)
        {
            string tag = StudyQuestions.OutlierTags[i];
            Button chip = null;
            chip = QuestionCardView.Pill(rect, tag, new Vector2(chipW, 64f), () => Toggle(tag, chip), 22f, TextAlignmentOptions.Center, Petal, Ink);
            var chipRect = (RectTransform)chip.transform;
            chipRect.anchorMin = chipRect.anchorMax = new Vector2(0f, 1f);
            chipRect.pivot = new Vector2(0f, 1f);
            chipRect.anchoredPosition = new Vector2((i % 2) * (chipW + 12f), -52f - (i / 2) * 76f);
        }

        comment = CommentField(rect, new Vector2(0f, -212f), new Vector2(w, 72f));
        return rect;
    }

    void Toggle(string tag, Button chip)
    {
        bool on = !tags.Remove(tag);
        if (on)
            tags.Add(tag);
        chip.GetComponent<Image>().color = on ? Rose : Petal;
        chip.GetComponentInChildren<TMP_Text>().color = on ? Color.white : Ink;
    }

    static TMP_InputField CommentField(RectTransform parent, Vector2 at, Vector2 size)
    {
        var host = new GameObject("Comment", typeof(RectTransform), typeof(Image));
        host.SetActive(false);
        host.transform.SetParent(parent, false);
        var rect = (RectTransform)host.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = at;
        rect.sizeDelta = size;
        QuestionCardView.Paint(host.GetComponent<Image>(), Petal, 0.5f);

        var area = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        area.transform.SetParent(host.transform, false);
        var areaRect = (RectTransform)area.transform;
        areaRect.anchorMin = Vector2.zero;
        areaRect.anchorMax = Vector2.one;
        areaRect.offsetMin = new Vector2(28f, 8f);
        areaRect.offsetMax = new Vector2(-28f, -8f);

        TMP_Text hint = Stretch(QuestionCardView.Label(area.transform, "Anything else? (one line)", AppType.BodyFont, 24f, Stone, Vector2.zero, Vector2.zero, TextAlignmentOptions.MidlineLeft));
        hint.fontStyle = FontStyles.Italic;
        TMP_Text typed = Stretch(QuestionCardView.Label(area.transform, "", AppType.BodyFont, 24f, Ink, Vector2.zero, Vector2.zero, TextAlignmentOptions.MidlineLeft));
        typed.textWrappingMode = TextWrappingModes.NoWrap;

        TMP_InputField field = host.AddComponent<TMP_InputField>();
        field.textViewport = areaRect;
        field.textComponent = typed;
        field.placeholder = hint;
        field.lineType = TMP_InputField.LineType.SingleLine;
        field.characterLimit = 140;
        if (AppType.BodyFont != null)
            field.fontAsset = AppType.BodyFont;
        field.pointSize = 24f;
        host.SetActive(true);
        return field;
    }

    static TMP_Text Stretch(TMP_Text text)
    {
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
        return text;
    }

    /// <summary>The old card leaves to the left as the new one arrives from the right.</summary>
    IEnumerator Slide(QuestionCardView leaving, QuestionCardView arriving)
    {
        float width = page.rect.width > 10f ? page.rect.width : 1080f;
        RectTransform from = leaving.Rect;
        RectTransform to = arriving.Rect;
        Vector2 home = to.anchoredPosition;
        float t = 0f;
        while (t < 1f && from != null && to != null)
        {
            t += Time.unscaledDeltaTime / SlideSeconds;
            float u = UIMotion.EaseOutCubic(Mathf.Clamp01(t));
            from.anchoredPosition = new Vector2(Mathf.Lerp(0f, -width, u), home.y);
            to.anchoredPosition = new Vector2(Mathf.Lerp(width, 0f, u), home.y);
            yield return null;
        }

        if (from != null)
            Destroy(from.gameObject);
        if (to != null)
            to.anchoredPosition = home;
    }

    /// <summary>Before and after side by side, so the change is the first thing the buyer sees.</summary>
    void ShowResult(StudySession session)
    {
        if (card != null)
            UIMotion.Dismiss(card.gameObject);
        card = null;

        var result = new GameObject("Result", typeof(RectTransform), typeof(Image));
        result.transform.SetParent(page, false);
        var rect = (RectTransform)result.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        float w = Mathf.Min(968f, page.rect.width > 300f ? page.rect.width - 64f : 968f);
        rect.sizeDelta = new Vector2(w, 720f);
        rect.anchoredPosition = new Vector2(0f, 120f);
        QuestionCardView.Paint(result.GetComponent<Image>(), Color.white, 0.5f);

        float half = (w - 96f - 24f) / 2f;
        QuestionCardView.Label(result.transform, "HOW READY YOU FEEL TO BUY", AppType.LabelFont, 22f, Stone, new Vector2(48f, -48f), new Vector2(w - 96f, 40f), TextAlignmentOptions.Left);
        Score(result.transform, "Before", session.pre_confidence, new Vector2(48f, -108f), half, Petal, Ink);
        Score(result.transform, "Now", session.post_confidence, new Vector2(48f + half + 24f, -108f), half, Rose, Color.white);

        int? change = session.ConfidenceDelta;
        string shift = !change.HasValue ? "Thanks — your answers are saved."
            : change.Value > 0 ? "You feel " + change.Value + (change.Value == 1 ? " step" : " steps") + " more ready than when you arrived."
            : change.Value < 0 ? "You feel less ready than when you arrived. That is useful to know before spending."
            : "You feel as ready as when you arrived.";
        QuestionCardView.Label(result.transform, shift, AppType.DisplayFont, 32f, Ink, new Vector2(48f, -392f), new Vector2(w - 96f, 110f), TextAlignmentOptions.TopLeft);
        int? size = session.SizeClarityDelta;
        if (size.HasValue && size.Value != 0)
            QuestionCardView.Label(result.transform, "Picturing true size: " + (size.Value > 0 ? "+" : "") + size.Value + " on a 7-point scale.",
                AppType.BodyFont, 26f, Stone, new Vector2(48f, -500f), new Vector2(w - 96f, 50f), TextAlignmentOptions.TopLeft);

        Button close = QuestionCardView.Pill(result.transform, "Back to the catalogue", new Vector2(w - 96f, 96f), Close);
        var closeRect = (RectTransform)close.transform;
        closeRect.anchorMin = closeRect.anchorMax = new Vector2(0.5f, 0f);
        closeRect.pivot = new Vector2(0.5f, 0f);
        closeRect.anchoredPosition = new Vector2(0f, 48f);
        UIMotion.Appear(result);
    }

    void Close()
    {
        gameObject.SetActive(false);
        MainMenu.TakeOver();
        MenuPage.Open("Catalogue");
    }

    static void Score(Transform parent, string caption, int value, Vector2 at, float width, Color fill, Color ink)
    {
        var box = new GameObject(caption, typeof(RectTransform), typeof(Image));
        box.transform.SetParent(parent, false);
        var rect = (RectTransform)box.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = at;
        rect.sizeDelta = new Vector2(width, 260f);
        QuestionCardView.Paint(box.GetComponent<Image>(), fill, 0.5f);
        QuestionCardView.Label(box.transform, caption.ToUpperInvariant(), AppType.LabelFont, 22f, ink, new Vector2(32f, -28f), new Vector2(width - 64f, 36f), TextAlignmentOptions.Left);
        TMP_Text number = QuestionCardView.Label(box.transform, value > 0 ? value + "<size=40%> / 7</size>" : "–", AppType.DisplayFont, 110f, ink, new Vector2(32f, -66f), new Vector2(width - 64f, 130f), TextAlignmentOptions.Left);
        number.richText = true;
        QuestionCardView.Label(box.transform, value > 0 ? StudyQuestions.PostConfidence.Describe(value) : "", AppType.BodyFont, 24f, ink, new Vector2(32f, -200f), new Vector2(width - 64f, 40f), TextAlignmentOptions.Left);
    }

    static RectTransform Fill(string name, Transform parent, Color color)
    {
        var host = new GameObject(name, typeof(RectTransform), typeof(Image));
        host.transform.SetParent(parent, false);
        var rect = (RectTransform)host.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        host.GetComponent<Image>().color = color;
        return rect;
    }

    static void Heading(RectTransform parent, string title, string line)
    {
        QuestionCardView.Label(parent, "Nº 02  ·  AFTER YOUR VISIT", AppType.LabelFont, 22f, Stone, new Vector2(78f, -200f), new Vector2(924f, 40f), TextAlignmentOptions.Left);
        TMP_Text big = QuestionCardView.Label(parent, title, AppType.DisplayFont, 72f, Ink, new Vector2(78f, -260f), new Vector2(924f, 110f), TextAlignmentOptions.TopLeft);
        big.lineSpacing = AppType.Leading(72f);
        QuestionCardView.Label(parent, line, AppType.BodyFont, 30f, Stone, new Vector2(78f, -380f), new Vector2(924f, 100f), TextAlignmentOptions.TopLeft);
    }
}
