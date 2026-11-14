using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// The front stays fixed along the bottom and bows open at the top.
public class FolderPeek : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    FolderBow bow;
    RectTransform titleRect;
    CanvasGroup title;
    Vector2 titleHome;
    float shown;
    bool hot;

    public void Bind(RectTransform flap, CanvasGroup heading)
    {
        bow = flap.GetComponent<FolderBow>();
        if (bow == null)
            bow = flap.gameObject.AddComponent<FolderBow>();
        title = heading;
        titleRect = heading != null ? heading.GetComponent<RectTransform>() : null;
        if (titleRect != null)
            titleHome = titleRect.anchoredPosition;
        if (title != null)
            title.alpha = 0f;
        if (bow != null)
            bow.Set(0f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hot = true;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        hot = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hot = false;
    }

    void Update()
    {
        if (bow == null)
            return;

        shown = Mathf.MoveTowards(shown, hot ? 1f : 0f, Time.unscaledDeltaTime / 0.22f);
        float t = shown * shown * (3f - 2f * shown);
        bow.Set(t);
        if (title != null)
            title.alpha = t;
        if (titleRect != null)
        {
            float height = bow.GetComponent<RectTransform>().rect.height;
            float fromBottom = titleHome.y + height * 0.5f;
            float along = Mathf.Clamp01(fromBottom / Mathf.Max(1f, height));
            float dip = height * 0.2f * t * along * along;
            titleRect.anchoredPosition = titleHome + new Vector2(0f, -dip);
        }
    }
}

public class FolderBow : BaseMeshEffect
{
    float amount;

    public void Set(float value)
    {
        value = Mathf.Clamp01(value);
        if (Mathf.Abs(amount - value) < 0.001f)
            return;
        amount = value;
        if (graphic != null)
            graphic.SetVerticesDirty();
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (amount < 0.001f || graphic == null)
            return;

        var area = graphic.rectTransform.rect;
        vh.Clear();
        const int cols = 14;
        const int rows = 8;
        float dip = area.height * 0.2f * amount;
        for (int y = 0; y <= rows; y++)
        {
            float v = y / (float)rows;
            for (int x = 0; x <= cols; x++)
            {
                float u = x / (float)cols;
                float curve = Mathf.Sin(u * Mathf.PI);
                float drop = dip * v * v * Mathf.Lerp(0.7f, 1f, curve);
                var vert = UIVertex.simpleVert;
                vert.color = graphic.color;
                vert.position = new Vector3(
                    Mathf.Lerp(area.xMin, area.xMax, u),
                    Mathf.Lerp(area.yMin, area.yMax, v) - drop);
                vert.uv0 = new Vector4(u, v, 0f, 0f);
                vh.AddVert(vert);
            }
        }

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                int i = y * (cols + 1) + x;
                vh.AddTriangle(i, i + cols + 1, i + 1);
                vh.AddTriangle(i + 1, i + cols + 1, i + cols + 2);
            }
        }
    }
}

public class MenuPage : MonoBehaviour
{
    public static void Open(string title)
    {
        var host = new GameObject(title + " Page", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = host.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 650;
        canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1
            | AdditionalCanvasShaderChannels.Normal
            | AdditionalCanvasShaderChannels.Tangent;

        var scaler = host.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0f;

        var root = host.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;

        bool catalogue = title == "Catalogue";
        var paper = host.AddComponent<Image>();
        paper.color = catalogue ? new Color32(251, 249, 250, 255) : new Color32(232, 230, 227, 255);

        var backObject = new GameObject("Back", typeof(RectTransform), typeof(Image), typeof(Button));
        backObject.transform.SetParent(host.transform, false);
        var backRect = backObject.GetComponent<RectTransform>();
        backRect.anchorMin = backRect.anchorMax = new Vector2(0f, 1f);
        backRect.pivot = new Vector2(0f, 1f);
        backRect.sizeDelta = new Vector2(180f, 72f);
        backRect.anchoredPosition = new Vector2(36f, -48f);
        var backImage = backObject.GetComponent<Image>();
        backImage.color = Color.white;
        var backButton = backObject.GetComponent<Button>();
        backButton.targetGraphic = backImage;
        backButton.onClick.AddListener(() => Destroy(host));

        var backLabel = new GameObject("Label", typeof(RectTransform));
        backLabel.transform.SetParent(backObject.transform, false);
        var backLabelRect = backLabel.GetComponent<RectTransform>();
        backLabelRect.anchorMin = Vector2.zero;
        backLabelRect.anchorMax = Vector2.one;
        backLabelRect.offsetMin = backLabelRect.offsetMax = Vector2.zero;
        var backText = backLabel.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            backText.font = TMP_Settings.defaultFontAsset;
        backText.text = "Back";
        backText.fontSize = 32f;
        backText.fontStyle = FontStyles.Bold;
        backText.alignment = TextAlignmentOptions.Center;
        backText.color = new Color32(28, 28, 30, 255);
        backText.raycastTarget = false;

        var heading = new GameObject("Title", typeof(RectTransform));
        heading.transform.SetParent(host.transform, false);
        var headingRect = heading.GetComponent<RectTransform>();
        var headingText = heading.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            headingText.font = TMP_Settings.defaultFontAsset;
        headingText.text = title;
        headingText.fontStyle = FontStyles.Bold;
        headingText.color = new Color32(42, 34, 37, 255);
        headingText.raycastTarget = false;

        if (catalogue)
        {
            headingRect.anchorMin = headingRect.anchorMax = new Vector2(0f, 1f);
            headingRect.pivot = new Vector2(0f, 1f);
            headingRect.anchoredPosition = new Vector2(36f, -148f);
            headingRect.sizeDelta = new Vector2(900f, 80f);
            headingText.fontSize = 48f;
            headingText.alignment = TextAlignmentOptions.MidlineLeft;
            CataloguePage.Fill(root);
            return;
        }

        headingRect.anchorMin = headingRect.anchorMax = new Vector2(0.5f, 0.5f);
        headingRect.sizeDelta = new Vector2(860f, 160f);
        headingText.fontSize = 64f;
        headingText.alignment = TextAlignmentOptions.Center;
    }
}
