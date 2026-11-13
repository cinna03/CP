using System.Collections.Generic;
using System.Text;
using Capstone.Catalogue;
using Capstone.Dialogue;
using Capstone.Ranking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Capstone.Placement
{
    /// <summary>
    /// Editor-only visit. Choosing a work places it at the stored metre size.
    /// </summary>
    public sealed class PracticeVisit : MonoBehaviour
    {
        const int SortOrder = 900;

        Transform quad;
        TMP_Text detail;
        TMP_Text reply;
        TMP_Text others;
        TMP_InputField question;
        Camera view;
        Artwork current;
        readonly VisitWeights weights = new VisitWeights();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OpenInEditor()
        {
            if (!Application.isEditor || FindAnyObjectByType<PracticeVisit>() != null)
                return;

            var host = new GameObject("Practice visit");
            host.AddComponent<PracticeVisit>();
        }

        void Start()
        {
            view = Camera.main;
            Build();
            Show(SampleWorks.All()[0]);
        }

        void Show(Artwork work)
        {
            if (quad == null)
            {
                var face = GameObject.CreatePrimitive(PrimitiveType.Quad);
                face.name = "Practice painting";
                quad = face.transform;
                var blocker = face.GetComponent<Collider>();
                if (blocker != null)
                    Destroy(blocker);
                var renderer = face.GetComponent<Renderer>();
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                    shader = Shader.Find("Unlit/Color");
                if (shader != null)
                {
                    var material = new Material(shader);
                    if (material.HasProperty("_BaseColor"))
                        material.SetColor("_BaseColor", new Color32(247, 236, 232, 255));
                    material.color = new Color32(247, 236, 232, 255);
                    renderer.material = material;
                }
            }

            WorkQuad.Fit(quad, work);
            if (view != null)
            {
                Transform camera = view.transform;
                quad.SetPositionAndRotation(camera.position + camera.forward * 1.5f, Quaternion.LookRotation(camera.forward, camera.up));
            }

            current = work;
            string note = string.IsNullOrEmpty(work.StoryNote) ? "No story note is stored." : work.StoryNote;
            detail.text = work.Title + "\n" + work.WidthCentimetres.ToString("0.##") + " cm wide, " + work.HeightCentimetres.ToString("0.##") + " cm tall\nWall size " + work.WidthMetres.ToString("0.00") + " m by " + work.HeightMetres.ToString("0.00") + " m\n" + note;
            reply.text = "";
            others.text = "No decision yet.";
        }

        void Ask()
        {
            if (current == null)
                return;

            reply.text = RuleTable.Answer(current, question.text);
        }

        void Choose(Decision decision)
        {
            if (current == null)
                return;

            weights.Apply(current, decision);
            IReadOnlyList<Artwork> three = Ranker.Three(SampleWorks.All(), current, weights);
            var text = new StringBuilder();
            text.Append(decision.ToString());
            text.Append(" recorded for ");
            text.Append(current.Title);
            text.Append(". Three other works:");
            for (int i = 0; i < three.Count; i++)
            {
                text.Append("\n");
                text.Append(i + 1);
                text.Append(". ");
                text.Append(three[i].Title);
            }

            others.text = text.ToString();
        }

        void Build()
        {
            var canvasObject = new GameObject("Practice canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortOrder;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasObject.transform, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 0.5f);
            panelRect.sizeDelta = new Vector2(520f, 0f);
            panelRect.anchoredPosition = Vector2.zero;
            panel.GetComponent<Image>().color = new Color32(247, 246, 243, 230);

            Artwork[] works = SampleWorks.All();
            for (int i = 0; i < works.Length; i++)
            {
                Artwork work = works[i];
                var button = new GameObject(work.Id, typeof(RectTransform), typeof(Image), typeof(Button));
                button.transform.SetParent(panel.transform, false);
                var rect = button.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.sizeDelta = new Vector2(460f, 64f);
                rect.anchoredPosition = new Vector2(24f, -16f - i * 72f);
                button.GetComponent<Image>().color = new Color32(255, 255, 255, 255);
                button.GetComponent<Button>().onClick.AddListener(() => Show(work));
                Label(button.transform, work.Title + "  " + work.WidthCentimetres.ToString("0.##") + " x " + work.HeightCentimetres.ToString("0.##") + " cm", 28f);
            }

            var detailObject = new GameObject("Detail", typeof(RectTransform));
            detailObject.transform.SetParent(panel.transform, false);
            var detailRect = detailObject.GetComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(0f, 0f);
            detailRect.anchorMax = new Vector2(1f, 1f);
            detailRect.offsetMin = new Vector2(30f, 40f);
            detailRect.offsetMax = new Vector2(-24f, -310f);
            detail = detailObject.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
                detail.font = TMP_Settings.defaultFontAsset;
            detail.fontSize = 32f;
            detail.color = new Color32(28, 28, 30, 255);
            detail.alignment = TextAlignmentOptions.TopLeft;

            var askPanel = new GameObject("Ask", typeof(RectTransform), typeof(Image));
            askPanel.transform.SetParent(canvasObject.transform, false);
            var askRect = askPanel.GetComponent<RectTransform>();
            askRect.anchorMin = new Vector2(1f, 0f);
            askRect.anchorMax = new Vector2(1f, 1f);
            askRect.pivot = new Vector2(1f, 0.5f);
            askRect.sizeDelta = new Vector2(560f, 0f);
            askRect.anchoredPosition = Vector2.zero;
            askPanel.GetComponent<Image>().color = new Color32(247, 246, 243, 230);

            question = QuestionField(askPanel.transform);
            ButtonAt(askPanel.transform, "Ask", new Vector2(24f, -96f), new Vector2(160f, 64f), Ask);
            reply = Block(askPanel.transform, new Vector2(24f, -176f), new Vector2(510f, 180f), 28f, TextAlignmentOptions.TopLeft);

            ButtonAt(askPanel.transform, "Keep", new Vector2(24f, -370f), new Vector2(150f, 64f), () => Choose(Decision.Keep));
            ButtonAt(askPanel.transform, "Save", new Vector2(190f, -370f), new Vector2(150f, 64f), () => Choose(Decision.Save));
            ButtonAt(askPanel.transform, "Dismiss", new Vector2(356f, -370f), new Vector2(170f, 64f), () => Choose(Decision.Dismiss));
            others = Block(askPanel.transform, new Vector2(24f, -450f), new Vector2(510f, 240f), 28f, TextAlignmentOptions.TopLeft);
            others.text = "No decision yet.";
        }

        static TMP_InputField QuestionField(Transform parent)
        {
            var root = new GameObject("Question", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(510f, 64f);
            rect.anchoredPosition = new Vector2(24f, -16f);
            root.GetComponent<Image>().color = new Color32(255, 255, 255, 255);

            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(root.transform, false);
            var viewRect = viewport.GetComponent<RectTransform>();
            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.offsetMin = new Vector2(16f, 12f);
            viewRect.offsetMax = new Vector2(-16f, -12f);

            var textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(viewport.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textObject.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
                text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = 30f;
            text.color = new Color32(28, 28, 30, 255);
            text.alignment = TextAlignmentOptions.MidlineLeft;

            var field = root.GetComponent<TMP_InputField>();
            field.textViewport = viewRect;
            field.textComponent = text;
            field.fontAsset = text.font;
            field.pointSize = 30f;
            field.lineType = TMP_InputField.LineType.SingleLine;
            return field;
        }

        static void ButtonAt(Transform parent, string title, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            var button = new GameObject(title, typeof(RectTransform), typeof(Image), typeof(Button));
            button.transform.SetParent(parent, false);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            button.GetComponent<Image>().color = new Color32(255, 255, 255, 255);
            button.GetComponent<Button>().onClick.AddListener(action);
            Label(button.transform, title, 28f);
        }

        static TMP_Text Block(Transform parent, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            var block = new GameObject("Block", typeof(RectTransform));
            block.transform.SetParent(parent, false);
            var rect = block.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var tmp = block.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
                tmp.font = TMP_Settings.defaultFontAsset;
            tmp.fontSize = fontSize;
            tmp.color = new Color32(28, 28, 30, 255);
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            return tmp;
        }

        static void Label(Transform parent, string text, float size)
        {
            var label = new GameObject("Label", typeof(RectTransform));
            label.transform.SetParent(parent, false);
            var rect = label.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(16f, 8f);
            rect.offsetMax = new Vector2(-16f, -8f);
            var tmp = label.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
                tmp.font = TMP_Settings.defaultFontAsset;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = new Color32(28, 28, 30, 255);
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
        }
    }
}
