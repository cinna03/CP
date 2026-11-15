using System.Collections.Generic;
using Capstone.App.Data;
using Capstone.App.Services;
using Capstone.App.UI;
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

        return HasEntry(node, "catalogue") && (HasEntry(node, "testdrive") || HasEntry(node, "arvisualizationsession"));
    }

    static string Compact(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        return value.Replace(" ", "").Replace("\u00A0", "").ToLowerInvariant();
    }

    static bool HasEntry(Transform node, string compactName)
    {
        if (node == null)
            return false;

        for (int i = 0; i < node.childCount; i++)
        {
            Transform child = node.GetChild(i);
            if (Compact(child.name) == compactName)
                return true;
            if (Compact(child.name) != "dock")
                continue;
            for (int j = 0; j < child.childCount; j++)
            {
                if (Compact(child.GetChild(j).name) == compactName)
                    return true;
            }
        }

        return false;
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
        canvas.gameObject.SetActive(true);
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

        SafeAreaFitter fit = panel.GetComponent<SafeAreaFitter>();
        if (fit == null)
            fit = panel.gameObject.AddComponent<SafeAreaFitter>();
        fit.Apply();
        Ensure(panel);
        for (int i = 0; i < panel.childCount; i++)
            panel.GetChild(i).gameObject.SetActive(true);

        panel.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();
        if (Application.isPlaying)
        {
            UserProfileFlush.Ensure();
            AddUserBar(panel);
        }
    }

    public static void Ensure(RectTransform panel)
    {
        Ground(panel);
        Board(panel);
        Canvas.ForceUpdateCanvases();

        float width;
        float height;
        Measure(panel, out width, out height);

        float margin = 28f;
        float gap = 16f;
        float left = -width * 0.5f + margin;
        float right = width * 0.5f - margin;
        float top = height * 0.5f - 12f;
        float bottom = -height * 0.5f;
        float innerW = right - left;
        float dockH = 64f;
        float dockLift = 20f;
        float headerH = 88f;
        float reserved = gap + 158f + gap + dockH + dockLift;
        float topZone = Mathf.Min(height * 0.38f, Mathf.Max(160f, height - reserved - 12f));
        float zoneBottom = top - topZone;
        float dashTop = top - headerH;
        float dashH = Mathf.Max(0f, dashTop - zoneBottom);
        float dashY = dashTop - dashH * 0.5f;
        float folderCeiling = zoneBottom - gap;
        float folderFloor = bottom + dockLift + dockH + gap;
        float folderRoom = folderCeiling - folderFloor;
        float folderH = folderRoom >= 150f ? Mathf.Min(158f, folderRoom) : Mathf.Max(120f, folderRoom);
        float minCenter = folderFloor + folderH * 0.5f;
        float maxCenter = folderCeiling - folderH * 0.5f;
        float folderY = maxCenter < minCenter ? minCenter : (minCenter + maxCenter) * 0.5f;
        float half = (innerW - gap) * 0.5f;

        Title(panel, new Vector2(left + 250f, top - headerH * 0.5f));
        HideNamed(panel, "Slogan");
        HideNamed(panel, "Open");

        RectTransform dashboard = Host(panel, "Studio dashboard", new Vector2(0f, dashY), new Vector2(innerW, dashH));
        if (Application.isPlaying)
        {
            var view = dashboard.GetComponent<MainMenuDashboardController>();
            if (view == null)
                view = dashboard.gameObject.AddComponent<MainMenuDashboardController>();
            CatalogueShelf shelf = Resources.Load<CatalogueShelf>("CatalogueShelf");
            ArtworkSessionState session = shelf != null ? shelf.Session : null;
            if (session != null && shelf != null)
                session.RestoreSaved(shelf.Works);
            view.Show(session, new WallSnapshotService().GetAllSnapshots(), Face(), work => CataloguePage.OpenWork(work));
        }

        StudioDigest digest = StudioDigest.Read(
            Resources.Load<CatalogueShelf>("CatalogueShelf") != null ? Resources.Load<CatalogueShelf>("CatalogueShelf").Session : null,
            Application.isPlaying ? new WallSnapshotService().GetAllSnapshots() : null);
        Folder(panel, "Favorites", new Vector2(left + half * 0.5f, folderY), new Vector2(half, folderH),
            new Color32(0xE3, 0x8F, 0xA3, 0xFF), digest.Hearts, "works", digest.FavoriteLine, digest.FavoriteThumb);
        Folder(panel, "Gallery", new Vector2(right - half * 0.5f, folderY), new Vector2(half, folderH),
            new Color32(0xF4, 0xC6, 0xD2, 0xFF), digest.Photos, "photos", digest.GalleryLine, null);

        RectTransform dock = DockBar(panel, margin, dockH, dockLift);
        float dockW = (Mathf.Max(120f, width - margin * 2f) - 24f) / 3f;
        Dock(dock, "Catalogue", "Catalogue", 0, dockW, false);
        Dock(dock, "AR visualization session", "Try on My Wall", 1, dockW, true);
        Dock(dock, "Help", "Help", 2, dockW, false);

        Transform board = panel.Find("Board");
        if (board != null)
            board.SetSiblingIndex(1);
    }

    static void OpenWallOrQuiz()
    {
        CatalogueShelf shelf = Resources.Load<CatalogueShelf>("CatalogueShelf");
        if (shelf == null || shelf.Session == null || shelf.Session.SelectedArtwork == null)
        {
            GuidedQuizViewController.Present(delegate { MenuPage.Open("Catalogue"); });
            return;
        }

        TestDrive.Begin();
    }

    static void AddUserBar(RectTransform panel)
    {
        Transform existing = panel.Find("User switch");
        if (existing != null)
            Destroy(existing.gameObject);

        var profiles = new UserProfileTracker();
        string code = profiles.CurrentEntranceCode;
        if (string.IsNullOrEmpty(code))
            code = "—";

        var bar = new GameObject("User switch", typeof(RectTransform));
        bar.transform.SetParent(panel, false);
        var rect = bar.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(460f, 56f);
        rect.anchoredPosition = new Vector2(-24f, -24f);

        var pill = new GameObject("Switch", typeof(RectTransform), typeof(Image), typeof(Button));
        pill.transform.SetParent(bar.transform, false);
        var pillRect = pill.GetComponent<RectTransform>();
        pillRect.anchorMin = Vector2.zero;
        pillRect.anchorMax = Vector2.one;
        pillRect.offsetMin = pillRect.offsetMax = Vector2.zero;
        Image pillImage = pill.GetComponent<Image>();
        pillImage.sprite = Rounded();
        pillImage.type = Image.Type.Sliced;
        pillImage.color = new Color32(0xF8, 0xDC, 0xE3, 0xFF);
        var label = new GameObject("Label", typeof(RectTransform));
        label.transform.SetParent(pill.transform, false);
        var labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(16f, 0f);
        labelRect.offsetMax = new Vector2(-16f, 0f);
        var text = label.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset face = Face();
        if (face != null)
            text.font = face;
        text.text = "User: " + code + " · Switch";
        text.fontSize = 22f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color32(0x1A, 0x17, 0x18, 0xFF);
        text.raycastTarget = false;
        pill.GetComponent<Button>().onClick.AddListener(SwitchUser);
        bar.transform.SetAsLastSibling();
    }

    static void SwitchUser()
    {
        new UserProfileTracker().ClearActiveUser();
        CatalogueShelf shelf = Resources.Load<CatalogueShelf>("CatalogueShelf");
        if (shelf != null && shelf.Session != null)
            shelf.Session.ReleaseForNextUser();

        Canvas canvas = Overlay();
        if (canvas != null)
            canvas.gameObject.SetActive(false);
        AccessGate.Reopen();
    }

    static void Measure(RectTransform panel, out float width, out float height)
    {
        Rect area = panel.rect;
        width = area.width;
        height = area.height;
        if (width > 8f && height > 8f)
            return;

        var canvas = panel.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            var canvasRect = canvas.transform as RectTransform;
            if (canvasRect != null && canvasRect.rect.width > 8f && canvasRect.rect.height > 8f)
            {
                width = canvasRect.rect.width;
                height = canvasRect.rect.height;
                return;
            }
        }

        width = 1080f;
        height = 1920f;
    }

    static void Board(RectTransform panel)
    {
        Transform existing = panel.Find("Board");
        GameObject board = existing != null ? existing.gameObject : new GameObject("Board", typeof(RectTransform), typeof(Image));
        if (existing == null)
        {
            board.transform.SetParent(panel, false);
            Record(board);
        }

        var rect = board.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var image = board.GetComponent<Image>();
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = new Color32(0xFA, 0xF7, 0xF6, 0xFF);
        image.raycastTarget = false;
        board.transform.SetSiblingIndex(1);
    }

    static void Title(RectTransform panel, Vector2 center)
    {
        Line(panel, "Masthead", "cinnarios", 56f, center, new Vector2(520f, 72f), TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
        Transform line = panel.Find("Masthead");
        if (line == null)
            return;
        var text = line.GetComponent<TMP_Text>();
        if (text == null)
            return;
        TMP_FontAsset face = Face();
        if (face != null)
            text.font = face;
        text.color = new Color32(0xE3, 0x8F, 0xA3, 0xFF);
        text.text = "cinnarios";
    }

    static void HideNamed(RectTransform panel, string name)
    {
        Transform found = panel.Find(name);
        if (found != null)
            found.gameObject.SetActive(false);
    }

    static RectTransform Host(RectTransform panel, string name, Vector2 position, Vector2 size)
    {
        if (name == "AR visualization session")
        {
            Transform old = panel.Find("Test Drive");
            if (old != null)
                old.name = name;
        }

        Transform found = panel.Find(name);
        GameObject card = found != null ? found.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image));
        if (found == null)
        {
            card.transform.SetParent(panel, false);
            Record(card);
        }

        card.SetActive(true);
        var rect = card.GetComponent<RectTransform>();
        Place(rect, position, size);
        return rect;
    }

    static void Folder(RectTransform panel, string name, Vector2 position, Vector2 size, Color body, int count, string unit, string preview, Sprite picture)
    {
        RectTransform rect = Host(panel, name, position, size);
        var card = rect.GetComponent<InteractiveFolderCard>();
        if (card == null)
            card = rect.gameObject.AddComponent<InteractiveFolderCard>();
        var legacy = rect.GetComponent<FolderPeek>();
        if (legacy != null)
            legacy.enabled = false;
        card.Present(name, count, unit, body, preview, picture, Face(), () => MenuPage.Open(name));
    }

    static RectTransform DockBar(RectTransform panel, float margin, float height, float lift)
    {
        Transform found = panel.Find("Dock");
        GameObject bar = found != null ? found.gameObject : new GameObject("Dock", typeof(RectTransform));
        if (found == null)
        {
            bar.transform.SetParent(panel, false);
            Record(bar);
        }

        bar.SetActive(true);
        var rect = bar.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, lift);
        rect.sizeDelta = new Vector2(-margin * 2f, height);
        rect.localScale = Vector3.one;
        return rect;
    }

    static void Dock(RectTransform bar, string name, string copy, int index, float width, bool primary)
    {
        Transform found = bar.Find(name);
        if (found == null && name == "AR visualization session" && bar.parent != null)
            found = bar.parent.Find("Test Drive");
        if (found == null && bar.parent != null)
            found = bar.parent.Find(name);
        if (found == null && name == "AR visualization session")
            found = bar.Find("Test Drive");

        GameObject card = found != null ? found.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image));
        if (found == null)
        {
            card.transform.SetParent(bar, false);
            Record(card);
        }
        else if (card.transform.parent != bar)
        {
            card.transform.SetParent(bar, false);
        }

        card.name = name;
        card.SetActive(true);
        var rect = card.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(width, 0f);
        rect.anchoredPosition = new Vector2(index * (width + 12f), 0f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        Image image = rect.GetComponent<Image>();
        if (image == null)
            image = rect.gameObject.AddComponent<Image>();
        image.sprite = Rounded();
        image.type = Image.Type.Sliced;
        image.color = new Color32(0xEB, 0xC8, 0xD2, 0xFF);
        image.raycastTarget = true;

        Transform fill = rect.Find("Fill");
        if (fill == null)
        {
            var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(rect, false);
            fill = fillObject.transform;
        }

        var fillRect = fill as RectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);
        Image fillImage = fill.GetComponent<Image>();
        fillImage.sprite = Rounded();
        fillImage.type = Image.Type.Sliced;
        fillImage.color = primary ? new Color32(0xE3, 0x8F, 0xA3, 0xFF) : Color.white;
        fillImage.raycastTarget = false;

        Transform label = rect.Find("Label");
        if (label == null)
        {
            var labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(rect, false);
            label = labelObject.transform;
            var created = labelObject.AddComponent<TextMeshProUGUI>();
            created.raycastTarget = false;
            created.alignment = TextAlignmentOptions.Center;
            created.fontStyle = FontStyles.Bold;
        }

        var labelRect = label as RectTransform;
        if (labelRect != null)
        {
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.offsetMin = new Vector2(8f, 6f);
            labelRect.offsetMax = new Vector2(-8f, -6f);
        }

        label.SetAsLastSibling();
        var text = label.GetComponent<TMP_Text>();
        if (text != null)
        {
            TMP_FontAsset face = Face();
            if (face != null)
                text.font = face;
            text.text = copy;
            text.fontSize = 20f;
            text.color = primary ? Color.white : new Color32(0x1A, 0x17, 0x18, 0xFF);
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
        }

        Listen(rect.gameObject, name);
    }

    static TMP_FontAsset Face()
    {
        if (nighty != null)
            return nighty;
        TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        for (int i = 0; i < fonts.Length; i++)
        {
            if (fonts[i] != null && fonts[i].name.IndexOf("Nighty", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                nighty = fonts[i];
                return nighty;
            }
        }

        nighty = TMP_Settings.defaultFontAsset;
        return nighty;
    }

    static TMP_FontAsset nighty;

    static void Masthead(RectTransform panel, Vector2 center, Vector2 size)
    {
        float titleSize = Mathf.Clamp(size.x * 0.16f, 32f, 56f);
        float sloganSize = Mathf.Clamp(size.x * 0.058f, 16f, 24f);
        float titleH = titleSize * 1.2f;
        float sloganH = sloganSize * 2.6f;
        float pillH = Mathf.Clamp(size.y * 0.2f, 44f, 60f);
        float pillW = Mathf.Min(size.x * 0.46f, 160f);
        float block = titleH + sloganH + pillH + 20f;
        if (block > size.y && size.y > 1f)
        {
            float fit = size.y / block;
            titleSize *= fit;
            sloganSize *= fit;
            titleH *= fit;
            sloganH *= fit;
            pillH *= fit;
            pillW *= fit;
        }

        float left = center.x - size.x * 0.5f;
        float cursor = center.y + size.y * 0.5f;
        float titleY = cursor - titleH * 0.5f;
        cursor -= titleH + 4f;
        float sloganY = cursor - sloganH * 0.5f;
        cursor -= sloganH + 12f;
        float pillY = cursor - pillH * 0.5f;

        Line(panel, "Masthead", "Cinnarios", titleSize, new Vector2(center.x, titleY), new Vector2(size.x, titleH), TextAlignmentOptions.Left, FontStyles.Bold);
        Line(panel, "Slogan", "At its size, in your space.", sloganSize, new Vector2(center.x, sloganY), new Vector2(size.x, sloganH), TextAlignmentOptions.TopLeft, FontStyles.Normal);

        Transform found = panel.Find("Open");
        GameObject pill = found != null ? found.gameObject : new GameObject("Open", typeof(RectTransform), typeof(Image), typeof(Button));
        if (found == null)
        {
            pill.transform.SetParent(panel, false);
            Record(pill);
        }

        var rect = pill.GetComponent<RectTransform>();
        Place(rect, new Vector2(left + pillW * 0.5f, pillY), new Vector2(pillW, pillH));
        var image = pill.GetComponent<Image>();
        image.sprite = Rounded();
        image.type = Image.Type.Sliced;
        image.color = new Color32(28, 28, 30, 255);
        image.raycastTarget = true;

        Transform label = pill.transform.Find("Label");
        if (label == null)
        {
            var labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(pill.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            var text = labelObject.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
                text.font = TMP_Settings.defaultFontAsset;
            text.text = "Open";
            text.fontSize = Mathf.Clamp(pillH * 0.42f, 16f, 26f);
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
        }
        else
        {
            var existing = label.GetComponent<TMP_Text>();
            if (existing != null)
                existing.fontSize = Mathf.Clamp(pillH * 0.42f, 16f, 26f);
        }

        if (Application.isPlaying)
        {
            var button = pill.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.RemoveListener(TestDrive.Begin);
            button.onClick.AddListener(TestDrive.Begin);
        }
    }

    static void Line(RectTransform panel, string name, string copy, float size, Vector2 position, Vector2 box, TextAlignmentOptions align, FontStyles style)
    {
        Transform found = panel.Find(name);
        GameObject line = found != null ? found.gameObject : new GameObject(name, typeof(RectTransform));
        if (found == null)
        {
            line.transform.SetParent(panel, false);
            var text = line.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
                text.font = TMP_Settings.defaultFontAsset;
            text.raycastTarget = false;
            Record(line);
        }

        var rect = line.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = box;
        rect.anchoredPosition = position;
        var label = line.GetComponent<TMP_Text>();
        label.text = copy;
        label.fontSize = size;
        label.fontStyle = style;
        label.alignment = align;
        label.color = new Color32(28, 28, 30, 255);
        label.textWrappingMode = TextWrappingModes.Normal;
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
            image.color = new Color32(0xFA, 0xF7, 0xF6, 0xFF);
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
        ground.GetComponent<Image>().color = new Color32(0xFA, 0xF7, 0xF6, 0xFF);
        ground.GetComponent<Image>().raycastTarget = false;
        Record(ground);
    }

    static void Card(RectTransform panel, string name, Vector2 position, Vector2 size, Color fill, Color ink, float typeSize)
    {
        Transform found = panel.Find(name);
        if (found == null && name == "AR visualization session")
        {
            found = panel.Find("Test Drive");
            if (found != null)
                found.name = name;
        }

        if (found != null)
        {
            found.gameObject.SetActive(true);
            var rect = found as RectTransform;
            Place(rect, position, size);
            var image = found.GetComponent<Image>();
            if (image == null)
                image = found.gameObject.AddComponent<Image>();
            image.sprite = Rounded();
            image.type = Image.Type.Sliced;
            image.color = fill;
            image.enabled = true;
            WriteLabel(found, name, typeSize, ink);
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
        labelRect.sizeDelta = new Vector2(-56f, 140f);
        labelRect.anchoredPosition = new Vector2(0f, -36f);

        var text = labelObject.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;
        text.text = name;
        text.fontSize = typeSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.color = ink;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        Listen(card, name);
        Record(card);
    }

    static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        if (rect == null)
            return;

        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    static void WriteLabel(Transform card, string name, float typeSize, Color ink)
    {
        Transform label = card.Find("Label");
        if (label == null)
            return;

        var labelRect = label as RectTransform;
        if (labelRect != null)
        {
            labelRect.sizeDelta = new Vector2(-56f, 140f);
            labelRect.anchoredPosition = new Vector2(0f, -36f);
        }

        var text = label.GetComponent<TMP_Text>();
        if (text == null)
            return;

        text.text = name;
        text.fontSize = typeSize;
        text.color = ink;
        text.textWrappingMode = TextWrappingModes.Normal;
    }

    static void Listen(GameObject card, string name)
    {
        if (!Application.isPlaying)
            return;

        var button = card.GetComponent<Button>();
        if (button == null)
            button = card.AddComponent<Button>();
        var image = card.GetComponent<Image>();
        if (image != null)
            button.targetGraphic = image;
        button.onClick.RemoveAllListeners();
        if (name == "AR visualization session")
            button.onClick.AddListener(OpenWallOrQuiz);
        else
            button.onClick.AddListener(() => MenuPage.Open(name));
    }

    static void FolderCard(RectTransform panel, string name, Vector2 position, Vector2 size)
    {
        Transform found = panel.Find(name);
        GameObject card;
        if (found == null)
        {
            card = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            card.transform.SetParent(panel, false);
            var created = card.GetComponent<RectTransform>();
            created.anchorMin = created.anchorMax = new Vector2(0.5f, 0.5f);
            created.pivot = new Vector2(0.5f, 0.5f);
            var image = card.GetComponent<Image>();
            image.sprite = Rounded();
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            var labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(card.transform, false);
            var text = labelObject.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
                text.font = TMP_Settings.defaultFontAsset;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            Record(card);
        }
        else
        {
            card = found.gameObject;
        }

        var rect = card.GetComponent<RectTransform>();
        Place(rect, position, size);
        var hit = card.GetComponent<Image>();
        if (hit == null)
            hit = card.AddComponent<Image>();
        hit.sprite = Rounded();
        hit.type = Image.Type.Sliced;
        hit.color = new Color(1f, 1f, 1f, 0f);
        hit.enabled = true;
        hit.raycastTarget = true;

        Transform folder = card.transform.Find("Folder");
        if (folder == null)
        {
            var created = new GameObject("Folder", typeof(RectTransform));
            created.transform.SetParent(card.transform, false);
            folder = created.transform;
            Record(created);
        }

        var folderRect = folder as RectTransform;
        folderRect.anchorMin = Vector2.zero;
        folderRect.anchorMax = Vector2.one;
        folderRect.offsetMin = folderRect.offsetMax = Vector2.zero;
        folderRect.localScale = Vector3.one;

        Transform label = card.transform.Find("Label");
        if (label == null)
            label = folder.Find("Front/Label");
        if (label != null)
            label.SetParent(card.transform, false);

        if (folder.Find("Page") == null)
        {
            for (int i = folder.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(folder.GetChild(i).gameObject);

            BuildFolder(folder);
        }

        PlaceFolder(folder, size);

        CanvasGroup heading = null;
        var flap = folder.Find("Front") as RectTransform;
        if (label != null && flap != null)
        {
            label.SetParent(flap, false);
            var labelRect = label as RectTransform;
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0f, 1f);
            float faceW = flap.sizeDelta.x;
            float faceH = flap.sizeDelta.y;
            labelRect.sizeDelta = new Vector2(faceW * 0.62f, faceH * 0.16f);
            labelRect.anchoredPosition = new Vector2(-faceW * 0.34f, faceH * 0.1f);
            var text = label.GetComponent<TMP_Text>();
            if (text != null)
            {
                text.text = name;
                text.fontSize = Mathf.Clamp(faceH * 0.075f, 16f, 26f);
                text.fontStyle = FontStyles.Bold;
                text.alignment = TextAlignmentOptions.TopLeft;
                text.color = new Color32(18, 58, 92, 255);
                text.textWrappingMode = TextWrappingModes.Normal;
                text.raycastTarget = false;
            }

            heading = label.GetComponent<CanvasGroup>();
            if (heading == null)
                heading = label.gameObject.AddComponent<CanvasGroup>();
            heading.alpha = 0f;
            heading.blocksRaycasts = false;
        }

        var peek = card.GetComponent<FolderPeek>();
        if (peek == null)
            peek = card.AddComponent<FolderPeek>();
        peek.Bind(flap, heading);

        Listen(card, name);
        var button = card.GetComponent<Button>();
        var ink = folder.Find("Front");
        if (button != null && ink != null)
        {
            button.targetGraphic = ink.GetComponent<Image>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
            colors.pressedColor = new Color(0.86f, 0.88f, 0.96f, 1f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }
    }

    static void BuildFolder(Transform folder)
    {
        var backObject = new GameObject("Back", typeof(RectTransform), typeof(Image));
        backObject.transform.SetParent(folder, false);
        var backImage = backObject.GetComponent<Image>();
        backImage.sprite = FolderBack();
        backImage.raycastTarget = false;

        var pageObject = new GameObject("Page", typeof(RectTransform), typeof(Image));
        pageObject.transform.SetParent(folder, false);
        var pageImage = pageObject.GetComponent<Image>();
        pageImage.sprite = Rounded();
        pageImage.type = Image.Type.Sliced;
        pageImage.color = Color.white;
        pageImage.raycastTarget = false;

        var frontObject = new GameObject("Front", typeof(RectTransform), typeof(Image));
        frontObject.transform.SetParent(folder, false);
        var frontImage = frontObject.GetComponent<Image>();
        frontImage.sprite = FolderFront();
        frontImage.raycastTarget = false;
        Record(backObject);
        Record(pageObject);
        Record(frontObject);
    }

    static void PlaceFolder(Transform folder, Vector2 card)
    {
        const float aspect = 480f / 400f;
        float faceW = card.x * 0.96f;
        float faceH = faceW / aspect;
        float limit = card.y * 0.92f;
        if (faceH > limit)
        {
            faceH = limit;
            faceW = faceH * aspect;
        }

        var back = folder.Find("Back") as RectTransform;
        var front = folder.Find("Front") as RectTransform;
        var page = folder.Find("Page") as RectTransform;
        FitSprite(back, FolderBack(), faceW, faceH);

        float coverW = faceW * (420f / 480f);
        float coverH = faceH * (246f / 400f);
        float coverBottom = -faceH * 0.5f + faceH * (40f / 400f);
        FitSprite(front, FolderFront(), coverW, coverH);
        front.pivot = new Vector2(0.5f, 0f);
        front.anchoredPosition = new Vector2(0f, coverBottom);
        if (front.GetComponent<FolderBow>() == null)
            front.gameObject.AddComponent<FolderBow>();

        float coverTop = coverBottom + coverH;
        float pageW = coverW * 0.86f;
        float pageH = coverH * 0.42f;
        page.anchorMin = page.anchorMax = new Vector2(0.5f, 0.5f);
        page.pivot = new Vector2(0.5f, 0.5f);
        page.sizeDelta = new Vector2(pageW, pageH);
        page.anchoredPosition = new Vector2(0f, coverTop - coverH * 0.06f - pageH * 0.5f);

        back.SetAsFirstSibling();
        page.SetSiblingIndex(1);
        front.SetAsLastSibling();
    }

    static void FitSprite(RectTransform rect, Sprite sprite, float width, float height)
    {
        var image = rect.GetComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.type = Image.Type.Simple;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    static void Record(GameObject created)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            UnityEditor.Undo.RegisterCreatedObjectUndo(created, "Main menu");
#endif
    }

    public static Canvas OverlayCanvas { get; private set; }

    static Canvas Overlay()
    {
        if (OverlayCanvas != null)
        {
            OverlayCanvas.gameObject.SetActive(true);
            return OverlayCanvas;
        }

        var host = new GameObject("MainMenuOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = host.GetComponent<Canvas>();
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

        OverlayCanvas = canvas;
        canvas.gameObject.SetActive(true);
        return canvas;
    }

    static RectTransform FindPanel()
    {
        return MenuHome.Panel;
    }

    static Sprite rounded;
    static Sprite folderBack;
    static Sprite folderFront;

    static Sprite FolderBack()
    {
        if (folderBack != null)
            return folderBack;

        const int width = 480;
        const int height = 400;
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color[width * height];
        var top = new Color(0.36f, 0.7f, 0.9f, 1f);
        var bottom = new Color(0.2f, 0.55f, 0.8f, 1f);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                float shape = FolderShell(px, py);
                float shadow = FolderShell(px, py + 18f);
                float cover = Mathf.Clamp01(0.9f - shape);
                float shade = Mathf.Clamp01(0.5f - shadow / 16f) * 0.18f * (1f - cover);
                Color ink = Color.Lerp(bottom, top, Mathf.InverseLerp(44f, 368f, py));
                Color pixel = Color.Lerp(new Color(0.1f, 0.25f, 0.5f, shade), ink, cover);
                pixel.a = Mathf.Clamp01(cover + shade);
                pixels[y * width + x] = pixel;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        folderBack = Sprite.Create(tex, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
        return folderBack;
    }

    static Sprite FolderFront()
    {
        if (folderFront != null)
            return folderFront;

        const int width = 420;
        const int height = 246;
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color[width * height];
        var lip = new Color(0.78f, 0.91f, 0.99f, 1f);
        var top = new Color(0.55f, 0.82f, 0.97f, 1f);
        var bottom = new Color(0.42f, 0.74f, 0.95f, 1f);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                float shape = RoundBox(px, py, 8f, 8f, width - 8f, height - 8f, 42f);
                float cover = Mathf.Clamp01(0.9f - shape);
                Color ink = Color.Lerp(bottom, top, py / height);
                if (py > height - 28f && shape < 1f)
                    ink = Color.Lerp(ink, lip, Mathf.Clamp01((py - (height - 28f)) / 20f));
                ink.a = cover;
                pixels[y * width + x] = ink;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        folderFront = Sprite.Create(tex, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0f), 100f);
        return folderFront;
    }

    static float FolderShell(float x, float y)
    {
        float body = RoundBox(x, y, 26f, 34f, 454f, 312f, 38f);
        float tab = RoundBox(x, y, 26f, 270f, 188f, 364f, 30f);
        float distance = Mathf.Min(body, tab);
        float dx = x - 228f;
        float dy = y - 272f;
        float notch = 40f - Mathf.Sqrt(dx * dx + dy * dy);
        return Mathf.Max(distance, notch);
    }

    static float RoundBox(float px, float py, float left, float bottom, float right, float top, float radius)
    {
        float halfX = (right - left) * 0.5f - radius;
        float halfY = (top - bottom) * 0.5f - radius;
        float qx = Mathf.Abs(px - (left + right) * 0.5f) - halfX;
        float qy = Mathf.Abs(py - (bottom + top) * 0.5f) - halfY;
        float outside = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
        return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
    }

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
