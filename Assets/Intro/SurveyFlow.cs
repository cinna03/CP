using Capstone.App.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runs the same four slides before the catalogue and again after the wall session.
/// Drag existing slide roots, question labels, and Next/Submit buttons into the inspector
/// if those panels are already in the scene. Empty slots are built at runtime.
/// The heart on a catalogue card is unchanged. Favorites and Gallery are not opened from here.
/// </summary>
public class SurveyFlow : MonoBehaviour
{
    static readonly string[] Questions =
    {
        "I can tell whether this painting fits my space.",
        "I can tell how big this painting will look on my wall.",
        "I can tell whether its colours and style suit the room.",
        "I can decide whether this painting is right for me."
    };

    static readonly Color Ink = new Color32(0x1A, 0x17, 0x18, 0xFF);
    static readonly Color Rose = new Color32(0xE3, 0x8F, 0xA3, 0xFF);
    static readonly Color Blush = new Color32(0xF8, 0xDC, 0xE3, 0xFF);
    static readonly Color Paper = new Color32(0xFA, 0xF7, 0xF6, 0xFF);

    static SurveyFlow live;

    [SerializeField] GameObject[] slides = new GameObject[4];
    [SerializeField] TMP_Text[] questionLabels = new TMP_Text[4];
    [SerializeField] Button[] nextButtons = new Button[4];

    readonly int[] picks = new int[4];
    readonly Button[][] ratings = new Button[4][];
    bool post;
    int index;

    public static void OpenPre()
    {
        Open(false);
    }

    public static void OpenPost()
    {
        Open(true);
    }

    public static void StartSurvey(bool isPostAR)
    {
        Open(isPostAR);
    }

    static void Open(bool isPostAR)
    {
        SurveyDataManager.Ensure();
        if (live == null)
            live = BuildHost();
        live.gameObject.SetActive(true);
        live.transform.SetAsLastSibling();
        live.Begin(isPostAR);
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
        RectTransform rect = host.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        DontDestroyOnLoad(host);
        return host.GetComponent<SurveyFlow>();
    }

    void Begin(bool isPostAR)
    {
        post = isPostAR;
        index = 0;
        SurveyDataManager.Instance.ClearRound();
        EnsureSlides();
        for (int i = 0; i < 4; i++)
        {
            picks[i] = 0;
            if (questionLabels[i] != null && i < Questions.Length)
                questionLabels[i].text = Questions[i];
            PaintRatings(i);
            int captured = i;
            if (nextButtons[i] != null)
            {
                nextButtons[i].onClick.RemoveAllListeners();
                nextButtons[i].onClick.AddListener(() => Advance(captured));
                nextButtons[i].interactable = false;
            }
        }

        Show(0);
    }

    void EnsureSlides()
    {
        for (int i = 0; i < 4; i++)
        {
            if (slides[i] != null && questionLabels[i] != null && nextButtons[i] != null)
            {
                EnsureRatings(i);
                continue;
            }

            BuildSlide(i);
        }
    }

    void BuildSlide(int slide)
    {
        var panel = new GameObject("Slide " + (slide + 1), typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = Paper;

        var labelObject = new GameObject("Question", typeof(RectTransform));
        labelObject.transform.SetParent(panel.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 1f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.sizeDelta = new Vector2(920f, 280f);
        labelRect.anchoredPosition = new Vector2(0f, -280f);
        TMP_Text label = labelObject.AddComponent<TextMeshProUGUI>();
        if (Capstone.App.UI.AppType.BodyFont != null)
            label.font = Capstone.App.UI.AppType.BodyFont;
        label.fontSize = 40f;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.color = Ink;
        label.textWrappingMode = TextWrappingModes.Normal;

        var nextObject = new GameObject(slide == 3 ? "Submit" : "Next", typeof(RectTransform), typeof(Image), typeof(Button));
        nextObject.transform.SetParent(panel.transform, false);
        RectTransform nextRect = nextObject.GetComponent<RectTransform>();
        nextRect.anchorMin = nextRect.anchorMax = new Vector2(0.5f, 0f);
        nextRect.pivot = new Vector2(0.5f, 0f);
        nextRect.sizeDelta = new Vector2(420f, 88f);
        nextRect.anchoredPosition = new Vector2(0f, 160f);
        nextObject.GetComponent<Image>().color = Rose;
        Button next = nextObject.GetComponent<Button>();
        next.targetGraphic = nextObject.GetComponent<Image>();
        var nextLabel = new GameObject("Label", typeof(RectTransform));
        nextLabel.transform.SetParent(nextObject.transform, false);
        RectTransform nextLabelRect = nextLabel.GetComponent<RectTransform>();
        nextLabelRect.anchorMin = Vector2.zero;
        nextLabelRect.anchorMax = Vector2.one;
        nextLabelRect.offsetMin = nextLabelRect.offsetMax = Vector2.zero;
        TMP_Text nextText = nextLabel.AddComponent<TextMeshProUGUI>();
        if (Capstone.App.UI.AppType.BodyFont != null)
            nextText.font = Capstone.App.UI.AppType.BodyFont;
        nextText.text = slide == 3 ? "Submit" : "Next";
        nextText.fontSize = 32f;
        nextText.alignment = TextAlignmentOptions.Center;
        nextText.color = Color.white;
        nextText.raycastTarget = false;
        int captured = slide;
        next.onClick.AddListener(() => Advance(captured));

        slides[slide] = panel;
        questionLabels[slide] = label;
        nextButtons[slide] = next;
        EnsureRatings(slide);
    }

    void EnsureRatings(int slide)
    {
        if (ratings[slide] != null && ratings[slide].Length == 5)
            return;

        Transform parent = slides[slide].transform;
        Transform existing = parent.Find("Ratings");
        RectTransform row;
        if (existing == null)
        {
            var rowObject = new GameObject("Ratings", typeof(RectTransform));
            rowObject.transform.SetParent(parent, false);
            row = rowObject.GetComponent<RectTransform>();
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 0.5f);
            row.sizeDelta = new Vector2(860f, 120f);
            row.anchoredPosition = new Vector2(0f, -40f);
        }
        else
        {
            row = existing as RectTransform;
        }

        ratings[slide] = new Button[5];
        for (int score = 1; score <= 5; score++)
        {
            Transform found = row.Find(score.ToString());
            Button button;
            if (found == null)
            {
                var item = new GameObject(score.ToString(), typeof(RectTransform), typeof(Image), typeof(Button));
                item.transform.SetParent(row, false);
                RectTransform itemRect = item.GetComponent<RectTransform>();
                itemRect.anchorMin = itemRect.anchorMax = new Vector2(0f, 0.5f);
                itemRect.pivot = new Vector2(0f, 0.5f);
                itemRect.sizeDelta = new Vector2(140f, 100f);
                itemRect.anchoredPosition = new Vector2((score - 1) * 172f, 0f);
                item.GetComponent<Image>().color = Color.white;
                button = item.GetComponent<Button>();
                button.targetGraphic = item.GetComponent<Image>();
                var caption = new GameObject("Label", typeof(RectTransform));
                caption.transform.SetParent(item.transform, false);
                RectTransform captionRect = caption.GetComponent<RectTransform>();
                captionRect.anchorMin = Vector2.zero;
                captionRect.anchorMax = Vector2.one;
                captionRect.offsetMin = captionRect.offsetMax = Vector2.zero;
                TMP_Text text = caption.AddComponent<TextMeshProUGUI>();
                if (Capstone.App.UI.AppType.BodyFont != null)
                    text.font = Capstone.App.UI.AppType.BodyFont;
                text.text = score.ToString();
                text.fontSize = 36f;
                text.alignment = TextAlignmentOptions.Center;
                text.color = Ink;
                text.raycastTarget = false;
            }
            else
            {
                button = found.GetComponent<Button>();
            }

            int picked = score;
            int slideIndex = slide;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => Choose(slideIndex, picked));
            ratings[slide][score - 1] = button;
        }
    }

    void Choose(int slide, int score)
    {
        picks[slide] = score;
        if (post)
            SurveyDataManager.Instance.RecordPostAnswer(slide, score);
        else
            SurveyDataManager.Instance.RecordPreAnswer(slide, score);
        PaintRatings(slide);
        if (nextButtons[slide] != null)
            nextButtons[slide].interactable = true;
    }

    void PaintRatings(int slide)
    {
        Button[] row = ratings[slide];
        if (row == null)
            return;
        for (int i = 0; i < row.Length; i++)
        {
            if (row[i] == null)
                continue;
            Image image = row[i].GetComponent<Image>();
            if (image != null)
                image.color = picks[slide] == i + 1 ? Rose : Blush;
        }
    }

    void Advance(int slide)
    {
        if (picks[slide] < 1)
            return;
        if (slide < 3)
        {
            Show(slide + 1);
            return;
        }

        if (post)
            SurveyDataManager.Instance.SavePostSurveyImmediately();
        else
            SurveyDataManager.Instance.SavePreSurveyImmediately();
        gameObject.SetActive(false);
        MainMenu.TakeOver();
        MenuPage.Open("Catalogue");
    }

    void Show(int slide)
    {
        index = slide;
        for (int i = 0; i < slides.Length; i++)
        {
            if (slides[i] != null)
                slides[i].SetActive(i == slide);
        }
    }
}
