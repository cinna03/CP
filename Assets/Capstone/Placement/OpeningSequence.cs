using System;
using UnityEngine;
using UnityEngine.UI;

namespace Capstone.Placement
{
    /// <summary>
    /// The first screen after the app opens: a blue field and one white line at a time.
    /// A tap leaves this screen and starts the session.
    /// </summary>
    public sealed class OpeningSequence : MonoBehaviour
    {
        static readonly string[] Lines =
        {
            "See it on your wall.",
            "It keeps its real size.",
            "Ask one thing."
        };

        const float Hold = 1.7f;

        Action finished;
        float clock;
        Text line;
        int shown = -1;

        public static void Play(Font typeface, Action onFinished)
        {
            GameObject host = new GameObject("Opening");
            OpeningSequence sequence = host.AddComponent<OpeningSequence>();
            sequence.finished = onFinished;
            sequence.Build(typeface);
        }

        void Build(Font typeface)
        {
            GameObject canvasObject = new GameObject("Opening canvas");
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 800;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0f;
            canvasObject.AddComponent<GraphicRaycaster>();

            Image ground = CreateImage(canvasObject.transform, "Blue field", Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ground.sprite = BlueField();
            Button screenTap = ground.gameObject.AddComponent<Button>();
            screenTap.targetGraphic = ground;
            screenTap.onClick.AddListener(Finish);

            line = CreateLine(canvasObject.transform, typeface);
            Show(0);
        }

        void Update()
        {
            clock += Time.deltaTime;
            float span = Hold;
            float local = clock % span;
            int index = Mathf.FloorToInt(clock / span) % Lines.Length;
            if (index != shown)
                Show(index);

            float fade = 1f;
            if (local < 0.28f)
                fade = local / 0.28f;
            else if (local > span - 0.28f)
                fade = (span - local) / 0.28f;
            Color color = line.color;
            color.a = Mathf.Clamp01(fade);
            line.color = color;
        }

        void Show(int index)
        {
            shown = index;
            line.text = Lines[index];
        }

        void Finish()
        {
            Action done = finished;
            finished = null;
            if (done != null)
                done();
            Destroy(gameObject);
        }

        static Text CreateLine(Transform parent, Font typeface)
        {
            GameObject host = new GameObject("Line");
            host.transform.SetParent(parent, false);
            Text text = host.AddComponent<Text>();
            text.font = typeface;
            text.fontSize = 72;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(860f, 220f);
            rect.anchoredPosition = Vector2.zero;
            return text;
        }

        static Image CreateImage(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            GameObject host = new GameObject(name, typeof(RectTransform));
            host.transform.SetParent(parent, false);
            Image image = host.AddComponent<Image>();
            image.color = color;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return image;
        }

        static Sprite BlueField()
        {
            const int height = 64;
            Texture2D texture = new Texture2D(4, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            Color top = new Color(0.05f, 0.28f, 0.92f, 1f);
            Color bottom = new Color(0.16f, 0.52f, 1f, 1f);
            for (int y = 0; y < height; y++)
            {
                Color band = Color.Lerp(bottom, top, y / (float)(height - 1));
                for (int x = 0; x < 4; x++)
                    texture.SetPixel(x, y, band);
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 4f, height), new Vector2(0.5f, 0.5f));
        }
    }
}
