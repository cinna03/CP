using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Capstone.App.UI
{
    /// <summary>
    /// The five-step guide. The menu still owns Back, and the catalogue button only asks the menu to open it.
    /// </summary>
    public sealed class HelpViewController : MonoBehaviour
    {
        static readonly Color Card = new Color32(255, 255, 255, 255);
        static readonly Color Accent = new Color32(233, 168, 188, 255);
        static readonly Color Ink = new Color32(42, 34, 37, 255);
        static readonly Color Stone = new Color32(125, 110, 115, 255);

        Sprite rounded;

        public static void Fill(RectTransform page, Action openCatalogue)
        {
            if (page == null)
                return;

            HelpViewController view = page.GetComponent<HelpViewController>();
            if (view == null)
                view = page.gameObject.AddComponent<HelpViewController>();
            view.Build(openCatalogue);
        }

        void Build(Action openCatalogue)
        {
            if (transform.Find("Help scroll") != null)
                return;

            var viewport = new GameObject("Help scroll", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            viewport.transform.SetParent(transform, false);
            var viewRect = viewport.GetComponent<RectTransform>();
            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.offsetMin = new Vector2(36f, 36f);
            viewRect.offsetMax = new Vector2(-36f, -240f);
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);

            var content = new GameObject("Guide", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var list = content.GetComponent<RectTransform>();
            list.anchorMin = new Vector2(0f, 1f);
            list.anchorMax = new Vector2(1f, 1f);
            list.pivot = new Vector2(0.5f, 1f);
            var stack = content.GetComponent<VerticalLayoutGroup>();
            stack.spacing = 16f;
            stack.padding = new RectOffset(8, 8, 8, 28);
            stack.childControlWidth = true;
            stack.childControlHeight = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = viewport.GetComponent<ScrollRect>();
            scroll.viewport = viewRect;
            scroll.content = list;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            Step(list, "1. Catalogue — Browse & Customize", "Search by mood, colour, style, or artist, narrow results with filter pills, and open any work to choose its print size (cm) and frame.");
            Step(list, "2. Try on My Wall — True-Scale AR & Advisor", "Preview your chosen piece on your wall at exact real-world dimensions (cm ÷ 100), ask the advisor about fit, lighting, or placement, and use Keep, Save, or Dismiss to explore tailored suggestions.");
            Step(list, "3. Gallery — Your Wall Try-On Photos", "Tap the camera button during any wall visit to save a clean snapshot of how a piece looks on your wall, then revisit Gallery anytime to compare photos without re-scanning.");
            Step(list, "4. Favorites — Your Saved Shortlist", "Tap the heart icon on any card (or Save Work on a detail page) to keep pieces stored on your device in Favorites across visits.");
            Step(list, "5. About This Prototype", "This app is an offline-first decision-support prototype designed to help buyers evaluate scale, colour, and room fit before purchasing. No account or payment is required.");

            var button = new GameObject("Go to Catalogue", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            button.transform.SetParent(list, false);
            Image fill = button.GetComponent<Image>();
            fill.sprite = Rounded();
            fill.type = Image.Type.Sliced;
            fill.color = Accent;
            button.GetComponent<LayoutElement>().preferredHeight = 76f;
            Text(button.transform, "Go to Catalogue", 28f, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
            button.GetComponent<Button>().onClick.AddListener(delegate
            {
                if (openCatalogue != null)
                    openCatalogue();
            });
        }

        void Step(RectTransform list, string heading, string body)
        {
            var card = new GameObject(heading, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            card.transform.SetParent(list, false);
            Image plate = card.GetComponent<Image>();
            plate.sprite = Rounded();
            plate.type = Image.Type.Sliced;
            plate.color = Card;
            card.GetComponent<LayoutElement>().preferredHeight = 320f;

            TMP_Text title = Text(card.transform, heading, 28f, Ink, FontStyles.Bold, TextAlignmentOptions.TopLeft);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.sizeDelta = new Vector2(-48f, 84f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -20f);

            TMP_Text copy = Text(card.transform, body, 24f, Stone, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            copy.rectTransform.anchorMin = Vector2.zero;
            copy.rectTransform.anchorMax = Vector2.one;
            copy.rectTransform.offsetMin = new Vector2(24f, 20f);
            copy.rectTransform.offsetMax = new Vector2(-24f, -108f);
        }

        TMP_Text Text(Transform parent, string caption, float size, Color color, FontStyles style, TextAlignmentOptions alignment)
        {
            var host = new GameObject("Label", typeof(RectTransform));
            host.transform.SetParent(parent, false);
            var rect = host.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = host.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
                text.font = TMP_Settings.defaultFontAsset;
            text.text = caption;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.enableWordWrapping = true;
            return text;
        }

        Sprite Rounded()
        {
            if (rounded != null)
                return rounded;

            const int edge = 96;
            const float radius = 28f;
            var texture = new Texture2D(edge, edge, TextureFormat.RGBA32, false);
            var pixels = new Color32[edge * edge];
            var clear = new Color32(255, 255, 255, 0);
            var solid = new Color32(255, 255, 255, 255);
            for (int y = 0; y < edge; y++)
            {
                for (int x = 0; x < edge; x++)
                {
                    float dx = 0f;
                    float dy = 0f;
                    if (x < radius)
                        dx = radius - x;
                    else if (x > edge - 1 - radius)
                        dx = x - (edge - 1 - radius);
                    if (y < radius)
                        dy = radius - y;
                    else if (y > edge - 1 - radius)
                        dy = y - (edge - 1 - radius);
                    pixels[y * edge + x] = dx * dx + dy * dy <= radius * radius ? solid : clear;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            rounded = Sprite.Create(texture, new Rect(0f, 0f, edge, edge), new Vector2(0.5f, 0.5f), edge, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            return rounded;
        }
    }
}
