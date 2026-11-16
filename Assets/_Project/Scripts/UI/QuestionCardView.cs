using System;
using System.Collections;
using System.Collections.Generic;
using Capstone.App.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Capstone.App.UI
{
    /// <summary>
    /// One study question as one card, in the app's own type, palette and motion: a thin progress bar,
    /// the prompt, then either a 1-7 slider whose words change as the buyer drags (with Next), or tap cards
    /// in a two-column grid that move on by themselves 300 ms after a tap. With needsDone the choice waits
    /// for a Done button instead, and Reveal() can open an optional section above it.
    /// </summary>
    public sealed class QuestionCardView : MonoBehaviour
    {
        static readonly Color Card = new Color32(0xFF, 0xFF, 0xFF, 0xF7);
        static readonly Color Ink = new Color32(0x2B, 0x21, 0x24, 0xFF);
        static readonly Color Stone = new Color32(0x6B, 0x53, 0x59, 0xFF);
        static readonly Color Rose = new Color32(0xB8, 0x5C, 0x74, 0xFF);
        static readonly Color Petal = new Color32(0xF7, 0xEC, 0xEF, 0xFF);

        const float MaxWidth = 968f;
        const float Pad = 48f;
        const float AutoAdvanceSeconds = 0.3f;
        const float ButtonHeight = 96f;

        StudyQuestion question;
        Action<int> answered;
        Action<int> picked;
        readonly List<Image> marks = new List<Image>();
        readonly List<TMP_Text> markLabels = new List<TMP_Text>();
        RectTransform rect;
        RectTransform body;
        Button done;
        TMP_Text bigNumber;
        TMP_Text words;
        float width = MaxWidth;
        float baseHeight;
        int choice;
        bool locked;

        public RectTransform Rect => rect;
        public float Width => width;

        /// <summary>
        /// Shows a question at the bottom of the parent. onAnswer gets the 1-based answer.
        /// needsDone: choices wait for a Done tap (onPick fires on each tap) instead of moving on.
        /// </summary>
        public static QuestionCardView Show(RectTransform parent, StudyQuestion question, string prompt,
            int step, int total, Action<int> onAnswer, bool needsDone = false, Action<int> onPick = null)
        {
            var root = new GameObject("Question " + question.Id, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            var view = root.AddComponent<QuestionCardView>();
            view.Build(question, string.IsNullOrEmpty(prompt) ? question.Prompt : prompt, step, total, onAnswer, needsDone, onPick);
            UIMotion.Appear(root);
            return view;
        }

        void Build(StudyQuestion asked, string prompt, int step, int total, Action<int> onAnswer, bool needsDone, Action<int> onPick)
        {
            question = asked;
            answered = onAnswer;
            picked = onPick;
            rect = (RectTransform)transform;
            float room = ((RectTransform)transform.parent).rect.width;
            if (room > 300f)
                width = Mathf.Min(MaxWidth, room - 64f);

            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 120f);
            Paint(GetComponent<Image>(), Card, 0.5f);

            body = Block("Body", transform);
            body.anchorMin = Vector2.zero;
            body.anchorMax = Vector2.one;
            body.offsetMin = body.offsetMax = Vector2.zero;

            float y = -Pad;
            Progress(y, step, total);
            y -= 32f;
            Label(body, step + " of " + total, AppType.LabelFont, 22f, Stone, new Vector2(Pad, y), new Vector2(width - Pad * 2f, 34f), TextAlignmentOptions.Left);
            y -= 46f;
            TMP_Text ask = Label(body, prompt, AppType.DisplayFont, 38f, Ink, new Vector2(Pad, y), new Vector2(width - Pad * 2f, 150f), TextAlignmentOptions.TopLeft);
            ask.enableAutoSizing = true;
            ask.fontSizeMin = 28f;
            ask.fontSizeMax = 38f;
            ask.richText = true;
            y -= 174f;

            y = asked.IsScale ? BuildSlider(y) : BuildCards(y);

            bool withButton = asked.IsScale || needsDone;
            if (withButton)
            {
                done = Pill(transform, asked.IsScale ? "Next" : "Done", new Vector2(width - Pad * 2f, ButtonHeight), Finish);
                var doneRect = (RectTransform)done.transform;
                doneRect.anchorMin = doneRect.anchorMax = new Vector2(0.5f, 0f);
                doneRect.pivot = new Vector2(0.5f, 0f);
                doneRect.anchoredPosition = new Vector2(0f, Pad);
                done.interactable = asked.IsScale;
                y -= ButtonHeight + 24f;
            }

            baseHeight = -y + Pad;
            rect.sizeDelta = new Vector2(width, baseHeight);
        }

        void Progress(float y, int step, int total)
        {
            RectTransform track = Block("Progress", body);
            Place(track, new Vector2(Pad, y), new Vector2(width - Pad * 2f, 8f));
            Paint(track.gameObject.AddComponent<Image>(), Petal, 6f);
            RectTransform fill = Block("Fill", track);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(Mathf.Clamp01(total > 0 ? (float)step / total : 0f), 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            Paint(fill.gameObject.AddComponent<Image>(), Rose, 6f);
        }

        float BuildSlider(float y)
        {
            choice = question.Default;
            bigNumber = Label(body, "", AppType.DisplayFont, 96f, Rose, new Vector2(Pad, y), new Vector2(140f, 110f), TextAlignmentOptions.Left);
            words = Label(body, "", AppType.DisplayFont, 40f, Ink, new Vector2(Pad + 140f, y - 30f), new Vector2(width - Pad * 2f - 140f, 60f), TextAlignmentOptions.Left);
            y -= 140f;

            var sliderObject = new GameObject("Slider", typeof(RectTransform));
            sliderObject.transform.SetParent(body, false);
            sliderObject.SetActive(false);
            var sliderRect = (RectTransform)sliderObject.transform;
            Place(sliderRect, new Vector2(Pad, y), new Vector2(width - Pad * 2f, 72f));

            RectTransform track = Block("Track", sliderRect);
            track.anchorMin = new Vector2(0f, 0.5f);
            track.anchorMax = new Vector2(1f, 0.5f);
            track.sizeDelta = new Vector2(0f, 16f);
            Paint(track.gameObject.AddComponent<Image>(), Petal, 8f);

            RectTransform fillArea = Block("Fill Area", sliderRect);
            fillArea.anchorMin = new Vector2(0f, 0.5f);
            fillArea.anchorMax = new Vector2(1f, 0.5f);
            fillArea.sizeDelta = new Vector2(-36f, 16f);
            RectTransform fill = Block("Fill", fillArea);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f, 1f);
            fill.sizeDelta = Vector2.zero;
            Image fillImage = fill.gameObject.AddComponent<Image>();
            Paint(fillImage, Rose, 8f);

            RectTransform handleArea = Block("Handle Area", sliderRect);
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = new Vector2(36f, 0f);
            handleArea.offsetMax = new Vector2(-36f, 0f);
            RectTransform handle = Block("Handle", handleArea);
            handle.anchorMin = Vector2.zero;
            handle.anchorMax = new Vector2(0f, 1f);
            handle.sizeDelta = new Vector2(72f, 0f);
            Image handleImage = handle.gameObject.AddComponent<Image>();
            Paint(handleImage, Rose, 0.66f);

            Slider slider = sliderObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 1f;
            slider.maxValue = 7f;
            slider.wholeNumbers = true;
            slider.SetValueWithoutNotify(choice);
            slider.onValueChanged.AddListener(value => ShowPoint(Mathf.RoundToInt(value)));
            sliderObject.SetActive(true);
            ShowPoint(choice);
            y -= 84f;

            float span = width - Pad * 2f;
            Label(body, question.Describe(1), AppType.BodyFont, 22f, Stone, new Vector2(Pad, y), new Vector2(span / 3f, 40f), TextAlignmentOptions.Left);
            Label(body, question.Describe(4), AppType.BodyFont, 22f, Stone, new Vector2(Pad + span / 3f, y), new Vector2(span / 3f, 40f), TextAlignmentOptions.Center);
            Label(body, question.Describe(7), AppType.BodyFont, 22f, Stone, new Vector2(Pad + span * 2f / 3f, y), new Vector2(span / 3f, 40f), TextAlignmentOptions.Right);
            return y - 64f;
        }

        void ShowPoint(int value)
        {
            choice = value;
            bigNumber.text = value.ToString();
            words.text = question.Describe(value);
        }

        float BuildCards(float y)
        {
            int count = question.Labels.Count;
            const float gap = 16f;
            const float cardHeight = 128f;
            float cardWidth = (width - Pad * 2f - gap) / 2f;
            for (int i = 0; i < count; i++)
            {
                int column = i % 2;
                int row = i / 2;
                bool alone = i == count - 1 && column == 0;
                var at = new Vector2(Pad + column * (cardWidth + gap), y - row * (cardHeight + gap));
                var size = new Vector2(alone ? width - Pad * 2f : cardWidth, cardHeight);
                int value = i + 1;
                Button card = Pill(body, question.Labels[i], size, () => Choose(value), 28f, TextAlignmentOptions.Left, Petal, Ink);
                Place((RectTransform)card.transform, at, size);
                marks.Add(card.GetComponent<Image>());
                markLabels.Add(card.GetComponentInChildren<TMP_Text>());
            }

            int rows = (count + 1) / 2;
            return y - rows * (cardHeight + gap) - 8f;
        }

        void Choose(int value)
        {
            if (locked)
                return;
            choice = value;
            for (int i = 0; i < marks.Count; i++)
            {
                bool on = i == value - 1;
                marks[i].color = on ? Rose : Petal;
                markLabels[i].color = on ? Color.white : Ink;
            }

            if (done != null)
            {
                done.interactable = true;
                picked?.Invoke(value);
                return;
            }

            locked = true;
            StartCoroutine(After(AutoAdvanceSeconds, () => answered?.Invoke(value)));
        }

        void Finish()
        {
            if (locked || !question.Accepts(choice))
                return;
            locked = true;
            answered?.Invoke(choice);
        }

        /// <summary>Opens an extra section between the answers and the Done button (grows the card upward).</summary>
        public void Reveal(RectTransform section, float height)
        {
            section.SetParent(transform, false);
            section.anchorMin = section.anchorMax = new Vector2(0.5f, 0f);
            section.pivot = new Vector2(0.5f, 0f);
            section.anchoredPosition = new Vector2(0f, Pad + ButtonHeight + 24f);
            section.sizeDelta = new Vector2(width - Pad * 2f, height);
            rect.sizeDelta = new Vector2(width, baseHeight + height + 24f);
            UIMotion.Appear(section.gameObject);
        }

        /// <summary>Swaps the card's contents for a short line, then calls back (e.g. "tailoring your gallery").</summary>
        public void ShowMessage(string message, float seconds, Action then)
        {
            locked = true;
            body.gameObject.SetActive(false);
            if (done != null)
                done.gameObject.SetActive(false);
            rect.sizeDelta = new Vector2(width, 220f);
            TMP_Text line = Label(transform, message, AppType.DisplayFont, 36f, Ink, new Vector2(Pad, -Pad), new Vector2(width - Pad * 2f, 220f - Pad * 2f), TextAlignmentOptions.MidlineLeft);
            UIMotion.Appear(line.gameObject);
            StartCoroutine(After(seconds, then));
        }

        static IEnumerator After(float seconds, Action then)
        {
            yield return new WaitForSecondsRealtime(seconds);
            then?.Invoke();
        }

        // ---- small builders shared with SurveyFlow ----

        public static Button Pill(Transform parent, string label, Vector2 size, Action onTap, float fontSize = 30f,
            TextAlignmentOptions align = TextAlignmentOptions.Center, Color? fill = null, Color? ink = null)
        {
            var item = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            item.transform.SetParent(parent, false);
            ((RectTransform)item.transform).sizeDelta = size;
            Image image = item.GetComponent<Image>();
            Paint(image, fill ?? Rose, 0.5f);
            Button button = item.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onTap?.Invoke());
            TMP_Text text = Label(item.transform, label, AppType.BodyFont, fontSize, ink ?? Color.white, Vector2.zero, Vector2.zero, align);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(align == TextAlignmentOptions.Center ? 12f : 32f, 8f);
            text.rectTransform.offsetMax = new Vector2(-24f, -8f);
            return button;
        }

        public static TMP_Text Label(Transform parent, string value, TMP_FontAsset font, float size, Color color,
            Vector2 at, Vector2 box, TextAlignmentOptions align)
        {
            var host = new GameObject("Text", typeof(RectTransform));
            host.transform.SetParent(parent, false);
            Place((RectTransform)host.transform, at, box);
            var text = host.AddComponent<TextMeshProUGUI>();
            if (font != null)
                text.font = font;
            text.text = value;
            text.fontSize = size;
            text.characterSpacing = AppType.Tracking(size);
            text.color = color;
            text.alignment = align;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        public static void Paint(Image image, Color color, float roundness)
        {
            image.sprite = UIShapes.Rounded();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = roundness;
            image.color = color;
        }

        static RectTransform Block(string name, Transform parent)
        {
            var host = new GameObject(name, typeof(RectTransform));
            host.transform.SetParent(parent, false);
            return (RectTransform)host.transform;
        }

        static void Place(RectTransform target, Vector2 at, Vector2 size)
        {
            target.anchorMin = target.anchorMax = new Vector2(0f, 1f);
            target.pivot = new Vector2(0f, 1f);
            target.anchoredPosition = at;
            target.sizeDelta = size;
        }
    }
}
