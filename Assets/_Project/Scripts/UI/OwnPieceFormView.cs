using System;
using Capstone.App.Data;
using Capstone.App.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Capstone.App.UI
{
    /// <summary>
    /// "Try your own piece": the buyer chooses a photo of a work they are considering, enters its
    /// painted width and height, and sends it to the wall at that size. The view only collects input
    /// and shows messages; the checks live in <see cref="OwnPieceRules"/> and the photo comes from
    /// an <see cref="IPhotoPicker"/>.
    /// </summary>
    public sealed class OwnPieceFormView : MonoBehaviour
    {
        static readonly Color Paper = new Color32(0xFC, 0xFB, 0xFA, 0xFF);
        static readonly Color Ink = new Color32(0x2B, 0x21, 0x24, 0xFF);
        static readonly Color Stone = new Color32(0x6B, 0x53, 0x59, 0xFF);
        static readonly Color Rose = new Color32(0xB8, 0x5C, 0x74, 0xFF);
        static readonly Color Well = new Color32(0xF7, 0xEC, 0xEF, 0xFF);
        static readonly Color Line = new Color32(0xD9, 0xC3, 0xC9, 0xFF);

        IPhotoPicker picker;
        Action<ArtworkData> ready;
        Texture2D photo;
        Image photoImage;
        TMP_Text photoHint;
        TMP_InputField titleField;
        TMP_InputField widthField;
        TMP_InputField heightField;
        TMP_Text message;
        bool shapeWarned;

        /// <summary>Opens the form. <paramref name="onReady"/> receives the new artwork when the buyer taps Try on my wall.</summary>
        public static OwnPieceFormView Present(Action<ArtworkData> onReady, IPhotoPicker photoPicker = null)
        {
            var host = new GameObject("Own piece", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = host.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            var scaler = host.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0f;

            var view = host.AddComponent<OwnPieceFormView>();
            view.ready = onReady;
            view.picker = photoPicker ?? PhotoPicker.Ensure();
            view.Build();
            UIMotion.Appear(host.transform.GetChild(0).gameObject, 0f, UIMotion.Standard);
            return view;
        }

        void Build()
        {
            var page = Box(transform, "Page", Paper, false);
            Stretch(page.rectTransform);
            page.raycastTarget = true;
            page.gameObject.AddComponent<SafeAreaFitter>();
            Transform root = page.transform;

            Button cancel = Pill(root, "Cancel", new Vector2(0f, 1f), new Vector2(200f, 88f), new Vector2(150f, -110f), Paper, Ink, Close);
            cancel.GetComponent<Image>().color = Well;

            Label(root, "Try your own piece", AppType.HeavyFont, AppType.Display, Ink, new Vector2(78f, -220f), new Vector2(924f, 80f), TextAlignmentOptions.Left);
            Label(root, "Use a clear, straight-on photo of the artwork only, then enter the size of the painted surface.", AppType.BodyFont, AppType.Body, Stone, new Vector2(78f, -300f), new Vector2(924f, 90f), TextAlignmentOptions.TopLeft);

            Image well = Box(root, "Photo", Well, true);
            Place(well.rectTransform, new Vector2(78f, -410f), new Vector2(924f, 620f));
            well.gameObject.AddComponent<Button>().onClick.AddListener(ChoosePhoto);
            var pictureObject = new GameObject("Picture", typeof(RectTransform), typeof(Image));
            pictureObject.transform.SetParent(well.transform, false);
            photoImage = pictureObject.GetComponent<Image>();
            photoImage.preserveAspect = true;
            photoImage.raycastTarget = false;
            photoImage.enabled = false;
            var pictureRect = photoImage.rectTransform;
            pictureRect.anchorMin = Vector2.zero;
            pictureRect.anchorMax = Vector2.one;
            pictureRect.offsetMin = new Vector2(24f, 24f);
            pictureRect.offsetMax = new Vector2(-24f, -24f);
            photoHint = Label(well.transform, "Tap to choose a photo", AppType.LabelFont, AppType.Heading, Rose, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            Stretch(photoHint.rectTransform);

            Label(root, "TITLE (OPTIONAL)", AppType.LabelFont, AppType.Micro, Stone, new Vector2(78f, -1070f), new Vector2(924f, 30f), TextAlignmentOptions.Left);
            titleField = Field(root, "My piece", TMP_InputField.ContentType.Standard, new Vector2(78f, -1106f), new Vector2(924f, 96f));

            Label(root, "WIDTH (CM)", AppType.LabelFont, AppType.Micro, Stone, new Vector2(78f, -1240f), new Vector2(440f, 30f), TextAlignmentOptions.Left);
            widthField = Field(root, "e.g. 60", TMP_InputField.ContentType.DecimalNumber, new Vector2(78f, -1276f), new Vector2(440f, 96f));
            Label(root, "HEIGHT (CM)", AppType.LabelFont, AppType.Micro, Stone, new Vector2(562f, -1240f), new Vector2(440f, 30f), TextAlignmentOptions.Left);
            heightField = Field(root, "e.g. 80", TMP_InputField.ContentType.DecimalNumber, new Vector2(562f, -1276f), new Vector2(440f, 96f));
            widthField.onValueChanged.AddListener(_ => shapeWarned = false);
            heightField.onValueChanged.AddListener(_ => shapeWarned = false);

            message = Label(root, "", AppType.BodyFont, AppType.Caption, Rose, new Vector2(78f, -1400f), new Vector2(924f, 110f), TextAlignmentOptions.TopLeft);

            Button go = Pill(root, "Try on my wall", new Vector2(0.5f, 0f), new Vector2(924f, 112f), new Vector2(0f, 120f), Ink, Paper, Submit);
            go.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
        }

        void ChoosePhoto()
        {
            message.text = "";
            picker.Pick(OnPhoto);
        }

        void OnPhoto(Texture2D picked)
        {
            if (this == null)
                return;
            if (picked == null)
            {
                if (photo == null)
                    message.text = "No photo was chosen.";
                return;
            }

            if (photo != null)
                Destroy(photo);
            photo = picked;
            photoImage.sprite = Sprite.Create(photo, new Rect(0, 0, photo.width, photo.height), new Vector2(0.5f, 0.5f), 100f);
            photoImage.enabled = true;
            photoHint.text = "";
            shapeWarned = false;
        }

        void Submit()
        {
            OwnPieceProblem problem = OwnPieceRules.Validate(photo != null, widthField.text, heightField.text, out float width, out float height);
            if (problem != OwnPieceProblem.None)
            {
                message.text = OwnPieceRules.Message(problem);
                return;
            }

            if (!shapeWarned && OwnPieceRules.ShapeMismatch(photo.width, photo.height, width, height))
            {
                shapeWarned = true;
                message.text = OwnPieceRules.LooksSwapped(photo.width, photo.height, width, height)
                    ? "The photo looks like width and height may be swapped. Check them, or tap Try on my wall again to continue."
                    : "The photo's shape does not match the size entered. Crop the photo to the artwork, or tap Try on my wall again to continue.";
                return;
            }

            ArtworkData work = ArtworkData.CreateOwnPiece(titleField.text, new Vector2(width, height), photoImage.sprite);
            Action<ArtworkData> done = ready;
            ready = null;
            Destroy(gameObject);
            done?.Invoke(work);
        }

        void Close()
        {
            if (photo != null)
                Destroy(photo);
            Destroy(gameObject);
        }

        static Image Box(Transform parent, string name, Color color, bool rounded)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(Image));
            item.transform.SetParent(parent, false);
            Image image = item.GetComponent<Image>();
            if (rounded)
            {
                image.sprite = UIShapes.Rounded();
                image.type = Image.Type.Sliced;
            }
            image.color = color;
            return image;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        static void Place(RectTransform rect, Vector2 topLeft, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = topLeft;
        }

        static TMP_Text Label(Transform parent, string value, TMP_FontAsset font, float size, Color color, Vector2 topLeft, Vector2 box, TextAlignmentOptions align)
        {
            var item = new GameObject("Text", typeof(RectTransform));
            item.transform.SetParent(parent, false);
            var text = item.AddComponent<TextMeshProUGUI>();
            if (font != null)
                text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = align;
            text.raycastTarget = false;
            Place(text.rectTransform, topLeft, box);
            return text;
        }

        static Button Pill(Transform parent, string caption, Vector2 anchor, Vector2 size, Vector2 position, Color fill, Color ink, UnityEngine.Events.UnityAction tap)
        {
            Image image = Box(parent, caption, fill, true);
            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(tap);
            TMP_Text text = Label(image.transform, caption, AppType.LabelFont, AppType.Body, ink, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            Stretch(text.rectTransform);
            return button;
        }

        static TMP_InputField Field(Transform parent, string hint, TMP_InputField.ContentType type, Vector2 topLeft, Vector2 size)
        {
            Image frame = Box(parent, "Field", Color.clear, false);
            Place(frame.rectTransform, topLeft, size);
            frame.raycastTarget = true;
            Image underline = Box(frame.transform, "Line", Line, false);
            underline.rectTransform.anchorMin = new Vector2(0f, 0f);
            underline.rectTransform.anchorMax = new Vector2(1f, 0f);
            underline.rectTransform.pivot = new Vector2(0.5f, 0f);
            underline.rectTransform.sizeDelta = new Vector2(0f, 4f);
            underline.raycastTarget = false;

            var area = new GameObject("Area", typeof(RectTransform), typeof(RectMask2D));
            area.transform.SetParent(frame.transform, false);
            var areaRect = area.GetComponent<RectTransform>();
            Stretch(areaRect);
            areaRect.offsetMin = new Vector2(4f, 8f);
            areaRect.offsetMax = new Vector2(-4f, -8f);

            TMP_Text value = Label(area.transform, "", AppType.LabelFont, AppType.Heading, Ink, Vector2.zero, Vector2.zero, TextAlignmentOptions.MidlineLeft);
            Stretch(value.rectTransform);
            TMP_Text placeholder = Label(area.transform, hint, AppType.LabelFont, AppType.Heading, Line, Vector2.zero, Vector2.zero, TextAlignmentOptions.MidlineLeft);
            Stretch(placeholder.rectTransform);

            var field = frame.gameObject.AddComponent<TMP_InputField>();
            field.textViewport = areaRect;
            field.textComponent = value;
            field.placeholder = placeholder;
            field.fontAsset = value.font;
            field.pointSize = AppType.Heading;
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.contentType = type;
            field.targetGraphic = frame;
            return field;
        }
    }
}
