using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The four entries live on the MainMenuPanel already in the scene.
// Missing ones are added once, and anything already placed is left where it is.
public class MainMenu : MonoBehaviour
{

    public static void Hide()
    {
        RectTransform panel = FindPanel();
        if (panel != null)
            panel.gameObject.SetActive(false);
    }

    public static void TakeOver()
    {
        var menus = new List<Transform>();
        var intros = new List<GameObject>();
        var words = new List<GameObject>();
        for (int s = 0; s < SceneManager.sceneCount; s++)
        {
            Scene scene = SceneManager.GetSceneAt(s);
            if (!scene.isLoaded)
                continue;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
                Collect(roots[i].transform, menus, intros, words);
        }

        Transform menu = null;
        int best = -1;
        for (int i = 0; i < menus.Count; i++)
        {
            int score = menus[i].childCount;
            if (score > best)
            {
                best = score;
                menu = menus[i];
            }
        }

        for (int i = 0; i < intros.Count; i++)
        {
            if (menu != null && (intros[i].transform == menu || intros[i].transform.IsChildOf(menu) || menu.IsChildOf(intros[i].transform)))
                continue;
            intros[i].SetActive(false);
        }

        for (int i = 0; i < words.Count; i++)
        {
            if (menu != null && (words[i].transform == menu || words[i].transform.IsChildOf(menu)))
                continue;
            words[i].SetActive(false);
        }

        if (menu is RectTransform panel)
            Show(panel);
    }

    static void Collect(Transform node, List<Transform> menus, List<GameObject> intros, List<GameObject> words)
    {
        if (IsMenu(node))
            menus.Add(node);

        string compact = Compact(node.name);
        if (compact.StartsWith("BeginningPanel") || compact.StartsWith("IntroPanel") || compact == "PanelWindow" || compact == "Ball")
            intros.Add(node.gameObject);

        var label = node.GetComponent<TMP_Text>();
        if (label != null && label.text != null && label.text.IndexOf("CINNARIOS", System.StringComparison.OrdinalIgnoreCase) >= 0)
            words.Add(node.gameObject);

        for (int i = 0; i < node.childCount; i++)
            Collect(node.GetChild(i), menus, intros, words);
    }

    static bool IsMenu(Transform node)
    {
        if (Compact(node.name) == "mainmenupanel")
            return true;

        bool drive = false;
        bool catalogue = false;
        for (int i = 0; i < node.childCount; i++)
        {
            string compact = Compact(node.GetChild(i).name);
            if (compact == "testdrive")
                drive = true;
            if (compact == "catalogue")
                catalogue = true;
        }

        return drive && catalogue;
    }

    static string Compact(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        return value.Replace(" ", "").Replace("\u00A0", "").ToLowerInvariant();
    }

    public static void Show()
    {
        Show(null);
    }

    public static void Show(RectTransform panel)
    {
        if (panel == null)
            panel = FindPanel();
        if (panel == null)
            return;

        Canvas canvas = Overlay();
        panel.SetParent(canvas.transform, false);
        panel.anchorMin = Vector2.zero;
        panel.anchorMax = Vector2.one;
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.offsetMin = panel.offsetMax = Vector2.zero;
        panel.localScale = Vector3.one;
        panel.localRotation = Quaternion.identity;
        panel.gameObject.SetActive(true);

        var veil = panel.GetComponent<CanvasGroup>();
        if (veil != null)
        {
            veil.alpha = 1f;
            veil.interactable = true;
            veil.blocksRaycasts = true;
        }

        var nested = panel.GetComponent<Canvas>();
        if (nested != null)
        {
            var ray = panel.GetComponent<GraphicRaycaster>();
            if (ray != null)
                Object.DestroyImmediate(ray);
            Object.DestroyImmediate(nested);
        }

        var wash = panel.GetComponent<Image>();
        if (wash != null)
            wash.enabled = false;
        var mask = panel.GetComponent<RectMask2D>();
        if (mask != null)
            mask.enabled = false;

        Ensure(panel);
        for (int i = 0; i < panel.childCount; i++)
            panel.GetChild(i).gameObject.SetActive(true);

        panel.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();
    }

    public static void Ensure(RectTransform panel)
    {
        Ground(panel);

        Card(panel, "Test Drive", new Vector2(-150f, 240f), new Vector2(640f, 760f),
            new Color32(247, 236, 232, 245), new Color32(28, 28, 30, 255), 52f);
        Card(panel, "Catalogue", new Vector2(230f, 60f), new Vector2(470f, 980f),
            new Color32(255, 255, 255, 255), new Color32(28, 28, 30, 255), 48f);
        Card(panel, "Favorites", new Vector2(-210f, -560f), new Vector2(500f, 230f),
            new Color32(22, 22, 24, 255), Color.white, 36f);
        Card(panel, "Gallery", new Vector2(230f, -590f), new Vector2(420f, 190f),
            new Color32(22, 22, 24, 255), Color.white, 34f);

        Transform catalogue = panel.Find("Catalogue");
        if (catalogue != null)
            catalogue.SetAsLastSibling();
        Transform favorites = panel.Find("Favorites");
        if (favorites != null)
            favorites.SetAsLastSibling();
        Transform gallery = panel.Find("Gallery");
        if (gallery != null)
            gallery.SetAsLastSibling();
    }

    static void Ground(RectTransform panel)
    {
        Transform existing = panel.Find("Ground");
        if (existing != null)
        {
            var rect = existing as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
            }

            var image = existing.GetComponent<Image>();
            if (image == null)
                image = existing.gameObject.AddComponent<Image>();
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = new Color32(243, 196, 214, 255);
            image.enabled = true;
            image.raycastTarget = false;
            existing.gameObject.SetActive(true);
            existing.SetAsFirstSibling();
            return;
        }

        var ground = new GameObject("Ground", typeof(RectTransform), typeof(Image));
        ground.transform.SetParent(panel, false);
        ground.transform.SetAsFirstSibling();
        var created = ground.GetComponent<RectTransform>();
        created.anchorMin = Vector2.zero;
        created.anchorMax = Vector2.one;
        created.offsetMin = created.offsetMax = Vector2.zero;
        ground.GetComponent<Image>().color = new Color32(243, 196, 214, 255);
        ground.GetComponent<Image>().raycastTarget = false;
        Record(ground);
    }

    static void Card(RectTransform panel, string name, Vector2 position, Vector2 size, Color fill, Color ink, float typeSize)
    {
        Transform found = panel.Find(name);
        if (found != null)
        {
            found.gameObject.SetActive(true);
            var rect = found as RectTransform;
            if (rect != null && rect.localScale.sqrMagnitude < 0.01f)
                rect.localScale = Vector3.one;
            var image = found.GetComponent<Image>();
            if (image == null)
                image = found.gameObject.AddComponent<Image>();
            image.sprite = Rounded();
            image.type = Image.Type.Sliced;
            image.color = fill;
            image.enabled = true;

            if (Application.isPlaying && rect != null)
            {
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = size;
                rect.anchoredPosition = position;
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.identity;
            }

            Listen(found.gameObject, name);
            return;
        }

        var card = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        card.transform.SetParent(panel, false);
        var cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = size;
        cardRect.anchoredPosition = position;

        var cardImage = card.GetComponent<Image>();
        cardImage.sprite = Rounded();
        cardImage.type = Image.Type.Sliced;
        cardImage.color = fill;
        var button = card.GetComponent<Button>();
        button.targetGraphic = cardImage;
        var colors = button.colors;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        var labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(card.transform, false);
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.sizeDelta = new Vector2(-48f, 90f);
        labelRect.anchoredPosition = new Vector2(0f, -28f);

        var text = labelObject.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;
        text.text = name;
        text.fontSize = typeSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.color = ink;
        text.raycastTarget = false;
        Listen(card, name);
        Record(card);
    }

    static void Listen(GameObject card, string name)
    {
        if (!Application.isPlaying || name != "Test Drive")
            return;

        var button = card.GetComponent<Button>();
        if (button == null)
            button = card.AddComponent<Button>();
        var image = card.GetComponent<Image>();
        if (image != null)
            button.targetGraphic = image;
        button.onClick.RemoveListener(TestDrive.Begin);
        button.onClick.AddListener(TestDrive.Begin);
    }

    static void Record(GameObject created)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            UnityEditor.Undo.RegisterCreatedObjectUndo(created, "Main menu");
#endif
    }

    static Canvas Overlay()
    {
        Canvas canvas = null;
        var canvases = Resources.FindObjectsOfTypeAll<Canvas>();
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] != null && canvases[i].name == "MainMenuOverlay" && canvases[i].gameObject.scene.IsValid())
            {
                canvas = canvases[i];
                break;
            }
        }

        if (canvas == null)
        {
            var host = new GameObject("MainMenuOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = host.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 500;
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1
                | AdditionalCanvasShaderChannels.Normal
                | AdditionalCanvasShaderChannels.Tangent;
            var scaler = host.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0f;
        }

        canvas.gameObject.SetActive(true);
        return canvas;
    }

    static RectTransform FindPanel()
    {
        Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
        RectTransform best = null;
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || !canvas.gameObject.scene.IsValid())
                continue;

            RectTransform found = FindNamed(canvas.transform);
            if (found == null)
                continue;
            if (best == null || found.childCount > best.childCount)
                best = found;
        }

        return best;
    }

    static RectTransform FindNamed(Transform root)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            string compact = child.name.Replace(" ", "");
            if (compact.Equals("MainMenuPanel", System.StringComparison.OrdinalIgnoreCase))
                return child as RectTransform;

            RectTransform nested = FindNamed(child);
            if (nested != null)
                return nested;
        }

        return null;
    }

    static Sprite rounded;

    static Sprite Rounded()
    {
        if (rounded != null)
            return rounded;

        const int size = 96;
        const int radius = 32;
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
}
