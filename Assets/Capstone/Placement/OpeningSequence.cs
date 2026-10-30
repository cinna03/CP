using System;
using UnityEngine;
using UnityEngine.UI;

namespace Capstone.Placement
{
    /// <summary>
    /// The first screen starts white. A pink ball drops, bounces to the top,
    /// then turns each line pink. It then comes toward the camera until the page is pink.
    /// </summary>
    public sealed class OpeningSequence : MonoBehaviour
    {
        static readonly string[] Lines =
        {
            "See it on your wall.",
            "It keeps its real size.",
            "Ask one thing."
        };

        static readonly Color Paper = Color.white;
        static readonly Color Pink = new Color(0.93f, 0.33f, 0.58f, 1f);
        const float Gravity = 3400f;
        const float Middle = 0f;

        Action finished;
        Image ground;
        RectTransform ball;
        Text[] words;
        float velocity;
        int nextWord;
        bool covering;
        float cover;
        bool done;

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

            ground = CreateImage(canvasObject.transform, "Paper", Paper, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Button screenTap = ground.gameObject.AddComponent<Button>();
            screenTap.targetGraphic = ground;
            screenTap.onClick.AddListener(Finish);

            words = new Text[Lines.Length];
            for (int i = 0; i < Lines.Length; i++)
            {
                words[i] = CreateLine(canvasObject.transform, typeface, Lines[i], -80f - (i * 150f));
                words[i].color = Color.white;
            }

            Image ballImage = CreateImage(
                canvasObject.transform,
                "Pink ball",
                Color.white,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 1100f),
                new Vector2(78f, 78f));
            ballImage.sprite = BallSprite();
            ballImage.raycastTarget = false;
            ball = ballImage.rectTransform;
            velocity = 0f;
        }

        void Update()
        {
            if (done)
                return;

            float step = Time.deltaTime;
            if (covering)
            {
                Cover(step);
                return;
            }

            velocity -= Gravity * step;
            Vector2 position = ball.anchoredPosition;
            position.y += velocity * step;
            ball.anchoredPosition = position;
            ball.localScale = Vector3.Lerp(ball.localScale, Vector3.one, 1f - Mathf.Exp(-10f * step));

            if (nextWord == 0 && velocity < 0f && position.y <= Middle)
            {
                Kick(Middle, 1000f);
                return;
            }

            if (nextWord == 0 && velocity > 0f && position.y >= 980f)
            {
                nextWord = 1;
                return;
            }

            int word = nextWord - 1;
            if (word < 0 || word >= words.Length || velocity >= 0f)
                return;

            float target = words[word].rectTransform.anchoredPosition.y;
            if (position.y > target + 28f)
                return;

            words[word].color = Pink;
            if (word == words.Length - 1)
            {
                covering = true;
                cover = 0f;
                return;
            }

            Kick(target, 170f);
            nextWord = word + 2;
        }

        void Kick(float height, float rise)
        {
            Vector2 position = ball.anchoredPosition;
            position.y = height;
            ball.anchoredPosition = position;
            velocity = Mathf.Sqrt(2f * Gravity * rise);
            ball.localScale = new Vector3(1.28f, 0.72f, 1f);
        }

        void Cover(float step)
        {
            cover += step;
            float rush = Mathf.Clamp01(cover / 0.85f);
            rush = rush * rush;
            ball.anchoredPosition = Vector2.Lerp(ball.anchoredPosition, Vector2.zero, 1f - Mathf.Exp(-6f * step));
            float scale = Mathf.Lerp(1f, 46f, rush);
            ball.localScale = new Vector3(scale, scale, 1f);
            ground.color = Color.Lerp(Paper, Pink, rush);
            if (rush >= 1f)
                Finish();
        }

        void Finish()
        {
            if (done)
                return;

            done = true;
            Action complete = finished;
            finished = null;
            if (complete != null)
                complete();
            Destroy(gameObject);
        }

        static Text CreateLine(Transform parent, Font typeface, string message, float height)
        {
            GameObject host = new GameObject(message);
            host.transform.SetParent(parent, false);
            Text text = host.AddComponent<Text>();
            text.font = typeface;
            text.fontSize = 64;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = message;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(900f, 120f);
            rect.anchoredPosition = new Vector2(0f, height);
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

        static Sprite BallSprite()
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            float centre = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - centre;
                    float dy = y - centre;
                    float distance = Mathf.Sqrt((dx * dx) + (dy * dy)) / centre;
                    if (distance > 1f)
                    {
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, 0f));
                        continue;
                    }

                    float edge = Mathf.Clamp01((1f - distance) * size);
                    float highlight = Mathf.Clamp01(1f - (Mathf.Sqrt(((dx + 10f) * (dx + 10f)) + ((dy - 12f) * (dy - 12f))) / 18f));
                    Color color = Color.Lerp(Pink, Color.white, highlight * 0.85f);
                    color.a = edge;
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
