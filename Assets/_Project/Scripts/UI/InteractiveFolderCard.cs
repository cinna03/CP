using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Capstone.App.UI
{
    /// <summary>
    /// A three-layer folder. The first tap lifts the paper. The next tap opens the page.
    /// </summary>
    public sealed class InteractiveFolderCard : MonoBehaviour
    {
        static readonly List<InteractiveFolderCard> Cards = new List<InteractiveFolderCard>();
        static readonly Color Dust = new Color32(0xD6, 0x73, 0x89, 0xFF);
        static readonly Color White = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        static readonly Color Ink = new Color32(0x1A, 0x17, 0x18, 0xFF);
        static readonly Color Taupe = new Color32(0x8A, 0x75, 0x6C, 0xFF);

        const float TabHeight = 16f;
        const float PeekLift = 32f;
        const float Tucked = 2f;

        RectTransform paper;
        CanvasGroup paperGroup;
        bool raised;
        float shown;
        Action open;

        public void Present(string title, int count, string unit, Color pocket, string preview, Sprite picture, TMP_FontAsset font, Action onOpen)
        {
            open = onOpen;
            if (!Cards.Contains(this))
                Cards.Add(this);

            Hide("Folder");
            Hide("Tab");
            Hide("Folder label");
            Hide("Peek");

            Image plate = GetComponent<Image>();
            if (plate == null)
                plate = gameObject.AddComponent<Image>();
            plate.color = new Color(1f, 1f, 1f, 0f);
            plate.raycastTarget = true;
            if (GetComponent<RectMask2D>() == null)
                gameObject.AddComponent<RectMask2D>();

            Button button = GetComponent<Button>();
            if (button == null)
                button = gameObject.AddComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.targetGraphic = plate;
            button.onClick.AddListener(Press);

            RectTransform back = Layer("Back", 0);
            Stretch(back, 0f, 0f, 0f, 0f);
            Image backPlate = back.GetComponent<Image>();
            backPlate.color = new Color(1f, 1f, 1f, 0f);
            backPlate.raycastTarget = false;
            Image backBody = Piece(back, "Body", Dust);
            Stretch(backBody.rectTransform, 0f, 0f, 0f, 16f);
            Image tab = Piece(back, "Tab", Dust);
            RectTransform tabRect = tab.rectTransform;
            tabRect.anchorMin = new Vector2(0f, 1f);
            tabRect.anchorMax = new Vector2(0.38f, 1f);
            tabRect.pivot = new Vector2(0f, 1f);
            tabRect.sizeDelta = new Vector2(0f, 16f);
            tabRect.anchoredPosition = new Vector2(18f, 0f);

            paper = Layer("Document", 1);
            paper.anchorMin = new Vector2(0.08f, 1f);
            paper.anchorMax = new Vector2(0.92f, 1f);
            paper.pivot = new Vector2(0.5f, 1f);
            paper.sizeDelta = new Vector2(0f, 96f);
            paper.anchoredPosition = new Vector2(0f, -(TabHeight + PeekLift + Tucked));
            Image paperImage = paper.GetComponent<Image>();
            if (paperImage == null)
                paperImage = paper.gameObject.AddComponent<Image>();
            paperImage.sprite = Round(12);
            paperImage.type = Image.Type.Sliced;
            paperImage.color = White;
            paperImage.raycastTarget = true;
            Outline edge = paper.GetComponent<Outline>();
            if (edge == null)
                edge = paper.gameObject.AddComponent<Outline>();
            edge.effectColor = new Color(0.1f, 0.09f, 0.09f, 0.12f);
            edge.effectDistance = new Vector2(0f, -2f);
            paperGroup = paper.GetComponent<CanvasGroup>();
            if (paperGroup == null)
                paperGroup = paper.gameObject.AddComponent<CanvasGroup>();
            paperGroup.blocksRaycasts = false;
            Button paperButton = paper.GetComponent<Button>();
            if (paperButton == null)
                paperButton = paper.gameObject.AddComponent<Button>();
            paperButton.onClick.RemoveAllListeners();
            paperButton.targetGraphic = paperImage;
            paperButton.onClick.AddListener(OpenNow);

            Write(paper, "Heading", HeadingFor(title), font, 13f, Taupe, FontStyles.Bold, 12f, 8f, 12f, 22f);
            Write(paper, "Lines", LinesFor(preview, count, unit), font, 16f, Ink, FontStyles.Normal, 12f, 32f, 72f, 64f);
            Image thumb = Thumb(paper);
            thumb.sprite = picture != null ? picture : Round(8);
            thumb.color = picture != null ? Color.white : new Color32(0xF8, 0xDC, 0xE3, 0xFF);
            thumb.preserveAspect = picture != null;

            RectTransform front = Layer("Pocket", 2);
            front.anchorMin = Vector2.zero;
            front.anchorMax = Vector2.one;
            front.offsetMin = Vector2.zero;
            front.offsetMax = new Vector2(0f, -(TabHeight + PeekLift));
            Image frontImage = front.GetComponent<Image>();
            if (frontImage == null)
                frontImage = front.gameObject.AddComponent<Image>();
            frontImage.sprite = Round(16);
            frontImage.type = Image.Type.Sliced;
            frontImage.color = pocket;
            frontImage.raycastTarget = false;

            Write(front, "Title", title, font, 26f, Ink, FontStyles.Bold, 18f, 16f, 64f, 36f);
            Write(front, "Count", count + " " + unit, font, 16f, Ink, FontStyles.Normal, 18f, 50f, 64f, 28f);
            TMP_Text mark = Write(front, "Mark", title == "Gallery" ? "●" : "♥", font, 22f, Ink, FontStyles.Normal, 0f, 14f, 14f, 32f);
            if (Capstone.App.UI.AppType.BodyFont != null)
                mark.font = Capstone.App.UI.AppType.BodyFont;
            mark.alignment = TextAlignmentOptions.TopRight;
            mark.rectTransform.anchorMin = new Vector2(1f, 1f);
            mark.rectTransform.anchorMax = new Vector2(1f, 1f);
            mark.rectTransform.pivot = new Vector2(1f, 1f);
            mark.rectTransform.sizeDelta = new Vector2(36f, 32f);
            mark.rectTransform.anchoredPosition = new Vector2(-14f, -14f);

            back.SetSiblingIndex(0);
            paper.SetSiblingIndex(1);
            front.SetSiblingIndex(2);
        }

        void OnDestroy()
        {
            Cards.Remove(this);
        }

        void Press()
        {
            if (raised)
            {
                OpenNow();
                return;
            }

            for (int i = 0; i < Cards.Count; i++)
            {
                if (Cards[i] != null && Cards[i] != this)
                    Cards[i].raised = false;
            }

            raised = true;
        }

        void OpenNow()
        {
            open?.Invoke();
        }

        void Update()
        {
            if (paper == null)
                return;

            shown = Mathf.MoveTowards(shown, raised ? 1f : 0f, Time.unscaledDeltaTime / 0.22f);
            float t = Mathf.Clamp01(shown);
            float rise = 1f + 2.4f * Mathf.Pow(t - 1f, 3f) + 1.4f * Mathf.Pow(t - 1f, 2f);
            paper.anchoredPosition = new Vector2(0f, -(TabHeight + PeekLift + Tucked) + PeekLift * rise);
            if (paperGroup != null)
                paperGroup.blocksRaycasts = t > 0.55f;
        }

        void Hide(string child)
        {
            Transform found = transform.Find(child);
            if (found != null)
                found.gameObject.SetActive(false);
        }

        RectTransform Layer(string name, int index)
        {
            Transform found = transform.Find(name);
            RectTransform rect;
            if (found == null)
            {
                var item = new GameObject(name, typeof(RectTransform), typeof(Image));
                item.transform.SetParent(transform, false);
                rect = item.GetComponent<RectTransform>();
            }
            else
            {
                found.gameObject.SetActive(true);
                rect = found as RectTransform;
            }

            rect.SetSiblingIndex(Mathf.Min(index, transform.childCount - 1));
            return rect;
        }

        static Image Piece(RectTransform parent, string name, Color color)
        {
            Transform found = parent.Find(name);
            Image image;
            if (found == null)
            {
                var item = new GameObject(name, typeof(RectTransform), typeof(Image));
                item.transform.SetParent(parent, false);
                image = item.GetComponent<Image>();
            }
            else
            {
                image = found.GetComponent<Image>();
            }

            image.sprite = Round(14);
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        static Image Thumb(RectTransform parent)
        {
            Transform found = parent.Find("Thumb");
            if (found == null)
            {
                var item = new GameObject("Thumb", typeof(RectTransform), typeof(Image));
                item.transform.SetParent(parent, false);
                found = item.transform;
            }

            var rect = found as RectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(44f, 44f);
            rect.anchoredPosition = new Vector2(-10f, -34f);
            Image image = found.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        static TMP_Text Write(RectTransform parent, string name, string value, TMP_FontAsset font, float size, Color color, FontStyles style, float left, float top, float right, float height)
        {
            Transform found = parent.Find(name);
            TMP_Text text;
            if (found == null)
            {
                var item = new GameObject(name, typeof(RectTransform));
                item.transform.SetParent(parent, false);
                text = item.AddComponent<TextMeshProUGUI>();
            }
            else
            {
                text = found.GetComponent<TextMeshProUGUI>();
                if (text == null)
                    text = found.gameObject.AddComponent<TextMeshProUGUI>();
            }

            if (font != null)
                text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(-right, -top);
            return text;
        }

        static string HeadingFor(string title)
        {
            return title == "Gallery" ? "LATEST WALL PHOTO" : "SAVED SHORTLIST";
        }

        static string LinesFor(string preview, int count, string unit)
        {
            if (string.IsNullOrEmpty(preview))
                return "Nothing saved yet\nOpen to add one";
            string second = count + " " + unit;
            return preview.Replace("\n", " ") + "\n" + second;
        }

        static Sprite round;

        static Sprite Round(int radius)
        {
            if (round != null)
                return round;

            const int edge = 48;
            var texture = new Texture2D(edge, edge, TextureFormat.RGBA32, false);
            var pixels = new Color32[edge * edge];
            for (int y = 0; y < edge; y++)
            {
                for (int x = 0; x < edge; x++)
                {
                    float dx = x < radius ? radius - x : x >= edge - radius ? x - (edge - radius - 1) : 0f;
                    float dy = y < radius ? radius - y : y >= edge - radius ? y - (edge - radius - 1) : 0f;
                    byte alpha = (byte)(Mathf.Clamp01(1f - (Mathf.Sqrt(dx * dx + dy * dy) - radius)) * 255f);
                    pixels[y * edge + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            round = Sprite.Create(texture, new Rect(0f, 0f, edge, edge), new Vector2(0.5f, 0.5f), edge, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            return round;
        }
    }
}
