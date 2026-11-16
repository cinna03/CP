using System.IO;
using Capstone.App.Data;
using Capstone.App.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Capstone.App.EditorTools
{
    /// <summary>
    /// Writes the theme, the four frames, a session asset, and five sample works.
    /// Existing assets are left in place so a second run does not wipe a sprite or a size you already set.
    /// </summary>
    public static class CatalogueSceneBuilder
    {
        const string Root = "Assets/_Project";
        const string ThemePath = Root + "/Themes/DefaultUITheme.asset";
        const string SessionPath = Root + "/Themes/DefaultSession.asset";
        const string FrameFolder = Root + "/Themes/Frames";
        const string SampleFolder = Root + "/Artworks/Samples";
        const string CardPrefabPath = Root + "/Prefabs/ArtworkCard.prefab";

        [MenuItem("Tools/Capstone/Generate Default Theme & Sample Artworks")]
        public static void Generate()
        {
            EnsureFolder("Assets", "_Project");
            EnsureFolder(Root, "Themes");
            EnsureFolder(Root, "Artworks");
            EnsureFolder(Root, "Prefabs");
            EnsureFolder(Root + "/Themes", "Frames");
            EnsureFolder(Root + "/Artworks", "Samples");

            CreateTheme();
            CreateSession();
            CreateFrame("Unframed", new Color32(0xF2, 0xDC, 0xE3, 0xFF), false);
            CreateFrame("Matte Black", new Color32(0x1C, 0x19, 0x1A, 0xFF), true);
            CreateFrame("Natural Oak", new Color32(0xC4, 0xA5, 0x74, 0xFF), true);
            CreateFrame("Gallery White", new Color32(0xFB, 0xF9, 0xFA, 0xFF), true);

            CreateArtwork("BlueNote", "blue", "Blue note", "A. Maker", 2019, "Oil", new[] { new Vector2(40f, 50f) }, "Quiet", "Calm", "Blue", "Mid", "Painted for a small room.");
            CreateArtwork("RedField", "red", "Red field", "B. Maker", 2021, "Acrylic", new[] { new Vector2(60f, 80f) }, "Bold", "Bright", "Red", "High", "A wide field of red.");
            CreateArtwork("GreenStudy", "green", "Green study", "C. Maker", 2018, "Ink", new[] { new Vector2(30f, 30f) }, "Soft", "Still", "Green", "Low", "");
            CreateArtwork("LinenRoom", "linen", "Linen room", "A. Maker", 2020, "Oil", new[] { new Vector2(35f, 45f) }, "Quiet", "Calm", "Blue", "Mid", "The same quiet blue, in a smaller size.");
            CreateArtwork("RoseStudy", "rose", "Rose study", "D. Maker", 2022, "Oil", new[] { new Vector2(50f, 70f), new Vector2(80f, 110f) }, "Soft", "Calm", "Pink", "Mid", "");
            CreateCardPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Capstone catalogue assets are ready in Assets/_Project.");
        }

        static void CreateTheme()
        {
            if (File.Exists(ThemePath))
                return;

            UIThemeData theme = ScriptableObject.CreateInstance<UIThemeData>();
            AssetDatabase.CreateAsset(theme, ThemePath);
        }

        static void CreateSession()
        {
            if (File.Exists(SessionPath))
                return;

            ArtworkSessionState session = ScriptableObject.CreateInstance<ArtworkSessionState>();
            AssetDatabase.CreateAsset(session, SessionPath);
        }

        static void CreateFrame(string frameName, Color color, bool withMaterial)
        {
            string assetName = frameName.Replace(" ", "");
            string path = FrameFolder + "/" + assetName + ".asset";
            if (File.Exists(path))
                return;

            FrameOption frame = ScriptableObject.CreateInstance<FrameOption>();
            AssetDatabase.CreateAsset(frame, path);

            Material material = null;
            Shader shader = withMaterial ? FrameShader() : null;
            if (shader != null)
            {
                material = new Material(shader);
                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", color);
                material.color = color;
                AssetDatabase.CreateAsset(material, FrameFolder + "/" + assetName + ".mat");
            }

            SerializedObject serialized = new SerializedObject(frame);
            serialized.FindProperty("frameName").stringValue = frameName;
            serialized.FindProperty("uiHighlightColor").colorValue = color;
            serialized.FindProperty("frame3DMaterial").objectReferenceValue = material;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void CreateArtwork(
            string fileName,
            string identifier,
            string title,
            string artist,
            int year,
            string medium,
            Vector2[] sizes,
            string style,
            string mood,
            string colour,
            string priceBand,
            string storyNote)
        {
            string path = SampleFolder + "/" + fileName + ".asset";
            if (File.Exists(path))
                return;

            ArtworkData artwork = ScriptableObject.CreateInstance<ArtworkData>();
            AssetDatabase.CreateAsset(artwork, path);

            SerializedObject serialized = new SerializedObject(artwork);
            serialized.FindProperty("identifier").stringValue = identifier;
            serialized.FindProperty("title").stringValue = title;
            serialized.FindProperty("artist").stringValue = artist;
            serialized.FindProperty("year").intValue = year;
            serialized.FindProperty("medium").stringValue = medium;
            serialized.FindProperty("style").stringValue = style;
            serialized.FindProperty("mood").stringValue = mood;
            serialized.FindProperty("colour").stringValue = colour;
            serialized.FindProperty("priceBand").stringValue = priceBand;
            serialized.FindProperty("storyNote").stringValue = storyNote;

            SerializedProperty storedSizes = serialized.FindProperty("availableSizes");
            storedSizes.arraySize = sizes.Length;
            for (int i = 0; i < sizes.Length; i++)
                storedSizes.GetArrayElementAtIndex(i).vector2Value = sizes[i];

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void CreateCardPrefab()
        {
            if (File.Exists(CardPrefabPath))
                return;

            UIThemeData theme = AssetDatabase.LoadAssetAtPath<UIThemeData>(ThemePath);
            GameObject root = new GameObject("ArtworkCard", typeof(RectTransform), typeof(Image), typeof(Button), typeof(ThemeApplier), typeof(ArtworkCardView));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(280f, 380f);

            GameObject picture = new GameObject("Picture", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
            picture.transform.SetParent(root.transform, false);
            Stretch(picture.GetComponent<RectTransform>(), new Vector2(0f, 0.34f), Vector2.one, 16f, 16f, 16f, 12f);
            Image pictureImage = picture.GetComponent<Image>();
            pictureImage.color = Color.white;
            pictureImage.preserveAspect = true;
            AspectRatioFitter fitter = picture.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 0.8f;

            TextMeshProUGUI title = CreateLabel(root.transform, "Title", "Title", 22f, FontStyles.Normal);
            Stretch(title.rectTransform, new Vector2(0f, 0.2f), new Vector2(1f, 0.34f), 16f, 0f, 16f, 0f);
            ApplyRole(title.gameObject, theme, ThemeRole.PrimaryText);

            TextMeshProUGUI size = CreateLabel(root.transform, "Size", "Size", 16f, FontStyles.Normal);
            Stretch(size.rectTransform, new Vector2(0f, 0.1f), new Vector2(0.62f, 0.2f), 16f, 0f, 8f, 0f);
            ApplyRole(size.gameObject, theme, ThemeRole.SecondaryText);

            GameObject save = new GameObject("Save", typeof(RectTransform), typeof(Image), typeof(Button));
            save.transform.SetParent(root.transform, false);
            Stretch(save.GetComponent<RectTransform>(), new Vector2(0.62f, 0.08f), new Vector2(1f, 0.2f), 0f, 8f, 16f, 8f);
            TextMeshProUGUI saveLabel = CreateLabel(save.transform, "Label", "Save", 16f, FontStyles.Normal);
            Stretch(saveLabel.rectTransform, Vector2.zero, Vector2.one, 8f, 4f, 8f, 4f);
            ApplyRole(saveLabel.gameObject, theme, ThemeRole.PrimaryText);

            ApplyRole(root, theme, ThemeRole.CardSurface);
            LayoutElement layout = root.AddComponent<LayoutElement>();
            layout.preferredWidth = 280f;
            layout.preferredHeight = 380f;

            SerializedObject view = new SerializedObject(root.GetComponent<ArtworkCardView>());
            view.FindProperty("artworkImage").objectReferenceValue = pictureImage;
            view.FindProperty("aspectRatioFitter").objectReferenceValue = fitter;
            view.FindProperty("titleLabel").objectReferenceValue = title;
            view.FindProperty("dimensionsLabel").objectReferenceValue = size;
            view.FindProperty("openButton").objectReferenceValue = root.GetComponent<Button>();
            view.FindProperty("shortlistButton").objectReferenceValue = save.GetComponent<Button>();
            view.FindProperty("shortlistLabel").objectReferenceValue = saveLabel;
            view.FindProperty("shortlistImage").objectReferenceValue = save.GetComponent<Image>();
            view.FindProperty("theme").objectReferenceValue = theme;
            view.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
            Object.DestroyImmediate(root);
        }

        static TextMeshProUGUI CreateLabel(Transform parent, string name, string text, float fontSize, FontStyles style)
        {
            GameObject label = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = label.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null)
                tmp.font = TMP_Settings.defaultFontAsset;
            return tmp;
        }

        static void ApplyRole(GameObject target, UIThemeData theme, ThemeRole role)
        {
            ThemeApplier applier = target.GetComponent<ThemeApplier>();
            if (applier == null)
                applier = target.AddComponent<ThemeApplier>();

            SerializedObject serialized = new SerializedObject(applier);
            serialized.FindProperty("theme").objectReferenceValue = theme;
            serialized.FindProperty("role").enumValueIndex = (int)role;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            applier.Apply();
        }

        static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, float left, float bottom, float right, float top)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        static Shader FrameShader()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
                return shader;

            return Shader.Find("Standard");
        }

        static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
