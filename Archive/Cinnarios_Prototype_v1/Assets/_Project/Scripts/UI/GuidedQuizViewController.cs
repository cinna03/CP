using System;
using Capstone.App.Data;
using Capstone.App.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Capstone.App.UI
{
    /// <summary>
    /// Three taps that record a room, a mood, and a colour before the catalogue opens.
    /// </summary>
    public sealed class GuidedQuizViewController : MonoBehaviour
    {
        static readonly Color Card = new Color32(255, 255, 255, 255);
        static readonly Color Accent = new Color32(233, 168, 188, 255);
        static readonly Color Blush = new Color32(249, 235, 240, 255);
        static readonly Color Ink = new Color32(42, 34, 37, 255);
        static readonly string[] Rooms = { "Living Room", "Bedroom", "Hallway / Narrow Wall", "Workspace", "I don't know" };
        static readonly string[] Moods = { "Calm", "Bold", "Warm", "Minimalist", "I don't know" };
        static readonly string[] Colours = { "Blue / Cool", "Warm / Earthy", "Pink / Pastel", "Neutral", "I don't know" };

        Action finished;
        ArtworkSessionState session;
        int step;
        bool any;
        string roomName = "";
        Sprite rounded;

        public static void Present(Action finished)
        {
            var host = new GameObject("Art finder", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(SafeAreaFitter));
            var canvas = host.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 800;
            var scaler = host.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0f;
            var wash = host.AddComponent<Image>();
            wash.color = new Color32(251, 249, 250, 230);
            var view = host.AddComponent<GuidedQuizViewController>();
            view.finished = finished;
            CatalogueShelf shelf = Resources.Load<CatalogueShelf>("CatalogueShelf");
            view.session = shelf != null ? shelf.Session : null;
            view.ShowStep();
            view.Chrome();
        }

        void Chrome()
        {
            Button back = Edge("← Back", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(220f, 72f), new Vector2(146f, -56f), Blush);
            back.onClick.AddListener(Close);
            Button skip = Edge("Skip to Full Catalogue", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(640f, 80f), new Vector2(0f, 72f), Card);
            skip.onClick.AddListener(Skip);
        }

        void Close()
        {
            Destroy(gameObject);
        }

        void Skip()
        {
            OpenCatalogue(false);
        }

        void ShowStep()
        {
            Transform old = transform.Find("Card");
            if (old != null)
                Destroy(old.gameObject);

            string question;
            string[] options;
            if (step == 0)
            {
                question = "Which room is this painting for?";
                options = Rooms;
            }
            else if (step == 1)
            {
                question = "What mood do you want in that space?";
                options = Moods;
            }
            else
            {
                question = "Which colour palette fits your space best?";
                options = Colours;
            }

            var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(transform, false);
            var rect = card.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(920f, 860f);
            Image plate = card.GetComponent<Image>();
            plate.sprite = Rounded();
            plate.type = Image.Type.Sliced;
            plate.color = Card;

            TMP_Text title = Text(card.transform, question, 32f, Ink, FontStyles.Bold);
            var titleRect = title.rectTransform;
            titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(820f, 120f);
            titleRect.anchoredPosition = new Vector2(0f, -28f);

            for (int i = 0; i < options.Length; i++)
            {
                string option = options[i];
                Button pill = Pill(card.transform, option, new Vector2(0f, -180f - i * 112f), option == "I don't know" ? Blush : Accent);
                pill.onClick.AddListener(delegate { Choose(option); });
            }
        }

        string[] answers = { "", "", "" };

        void Choose(string option)
        {
            bool skip = option == "I don't know";
            answers[step] = skip ? "" : option;
            if (!skip)
                any = true;

            step++;
            if (step < 3)
            {
                ShowStep();
                return;
            }

            RememberRoom(answers[0]);
            RememberMood(answers[1]);
            RememberColour(answers[2]);
            OpenCatalogue(any);
        }

        void OpenCatalogue(bool pin)
        {
            ArtFinderArrival.Pending = true;
            ArtFinderArrival.PinMatches = pin;
            if (pin)
            {
                string room = roomName.Length > 0 ? roomName : "space";
                ArtFinderArrival.Message = "Picked for your " + room + " · Tap any piece to choose size & frame";
            }
            else
            {
                ArtFinderArrival.Message = "No worries! Browse all 25 works below and tap any piece to try it on your wall.";
            }

            Action done = finished;
            Destroy(gameObject);
            if (done != null)
                done();
        }

        void RememberRoom(string option)
        {
            if (option.Length == 0 || session == null || session.Taste == null)
                return;

            string room = option == "Hallway / Narrow Wall" ? UserTasteProfile.Hallway : option;
            roomName = room;
            session.Taste.Remember(room, "", "");
        }

        void RememberMood(string option)
        {
            if (option.Length == 0 || session == null || session.Taste == null)
                return;

            if (option == "Calm")
                session.Taste.NoteDecision("", "Calm", "", 2f);
            else if (option == "Bold")
                session.Taste.NoteDecision("Bold", "", "", 2f);
            else if (option == "Warm")
            {
                session.Taste.NoteDecision("", "", "Ochre", 2f);
                session.Taste.NoteDecision("", "", "Orange", 2f);
            }
            else if (option == "Minimalist")
                session.Taste.NoteDecision("Quiet", "", "White", 2f);
        }

        void RememberColour(string option)
        {
            if (option.Length == 0 || session == null || session.Taste == null)
                return;

            if (option == "Blue / Cool")
                session.Taste.NoteDecision("", "", "Blue", 2f);
            else if (option == "Warm / Earthy")
            {
                session.Taste.NoteDecision("", "", "Ochre", 2f);
                session.Taste.NoteDecision("", "", "Orange", 2f);
            }
            else if (option == "Pink / Pastel")
                session.Taste.NoteDecision("", "", "Pink", 2f);
            else if (option == "Neutral")
            {
                session.Taste.NoteDecision("", "", "Grey", 2f);
                session.Taste.NoteDecision("", "", "White", 2f);
            }
        }

        Button Pill(Transform parent, string caption, Vector2 position, Color fill)
        {
            var host = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button));
            host.transform.SetParent(parent, false);
            var rect = host.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(820f, 88f);
            rect.anchoredPosition = position;
            Image image = host.GetComponent<Image>();
            image.sprite = Rounded();
            image.type = Image.Type.Sliced;
            image.color = fill;
            Text(host.transform, caption, 28f, Ink, FontStyles.Bold);
            Button button = host.GetComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        Button Edge(Transform parent, string caption, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 position, Color fill)
        {
            var host = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button));
            host.transform.SetParent(parent, false);
            var rect = host.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            Image image = host.GetComponent<Image>();
            image.sprite = Rounded();
            image.type = Image.Type.Sliced;
            image.color = fill;
            Text(host.transform, caption, 26f, Ink, FontStyles.Bold);
            Button button = host.GetComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        Button Edge(string caption, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 position, Color fill)
        {
            return Edge(transform, caption, anchorMin, anchorMax, size, position, fill);
        }

        TMP_Text Text(Transform parent, string caption, float size, Color color, FontStyles style)
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
            text.alignment = TextAlignmentOptions.Center;
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
