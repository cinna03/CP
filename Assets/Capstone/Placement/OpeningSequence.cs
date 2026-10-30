using System;
using UnityEngine;
using UnityEngine.UI;

namespace Capstone.Placement
{
    /// <summary>
    /// The first screen starts white. A pink ball drops to the centre of the canvas,
    /// bounces high, then turns each word of one centred line pink.
    /// It then comes toward the camera until the page is pink.
    /// </summary>
    public sealed class OpeningSequence : MonoBehaviour
    {
        static readonly string[] Line =
        {
            "See", "it", "on", "your", "wall", "at", "its", "real", "size,", "and", "ask", "one", "thing."
        };

        static readonly Color Paper = Color.white;
        static readonly Color Pink = new Color(0.93f, 0.33f, 0.58f, 1f);
        static readonly Color DeepPink = new Color(0.72f, 0.14f, 0.38f, 1f);
        const float Gravity = 10800f;
        const float Centre = 0f;
        const float BallSize = 58f;

        Action finished;
        Image ground;
        Image shadow;
        RectTransform ball;
        Text[] words;
        float velocity;
        float horizontalVelocity;
        float apex;
        float hop;
        int nextWord;
        bool risen;
        bool towardWords;
        bool aimed;
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

            float half = VisibleHalfHeight(canvas);
            apex = half * 0.9f;
            hop = half * 0.48f;
            words = CreateWords(canvasObject.transform, typeface);

            shadow = CreateImage(
                canvasObject.transform,
                "Shadow",
                new Color(0.45f, 0.18f, 0.28f, 0.16f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -8f),
                new Vector2(54f, 16f));
            shadow.sprite = ShadowSprite();
            shadow.raycastTarget = false;

            Image ballImage = CreateImage(
                canvasObject.transform,
                "Pink ball",
                Color.white,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, half + BallSize),
                new Vector2(BallSize, BallSize));
            ballImage.sprite = BallSprite();
            ballImage.raycastTarget = false;
            ball = ballImage.rectTransform;
            velocity = 0f;
        }

        void Update()
        {
            if (done)
                return;

            float step = Mathf.Min(Time.deltaTime, 0.033f);
            if (covering)
            {
                Cover(step);
                return;
            }

            velocity -= Gravity * step;
            Vector2 position = ball.anchoredPosition;
            position.y += velocity * step;
            if (aimed)
                position.x += horizontalVelocity * step;
            ball.anchoredPosition = position;
            ball.localScale = Vector3.Lerp(ball.localScale, Vector3.one, 1f - Mathf.Exp(-28f * step));
            PlaceShadow();

            if (!towardWords)
            {
                if (!risen && velocity < 0f && position.y <= Centre)
                {
                    Kick(0f, Centre, apex);
                    risen = true;
                    return;
                }

                if (!risen || velocity > 0f)
                    return;

                towardWords = true;
            }

            if (!aimed && velocity <= 0f)
            {
                Aim(nextWord);
                aimed = true;
            }

            if (!aimed || velocity >= 0f || nextWord >= words.Length)
                return;

            if (position.y > Centre)
                return;

            words[nextWord].color = Pink;
            if (nextWord == words.Length - 1)
            {
                covering = true;
                cover = 0f;
                return;
            }

            float landedX = words[nextWord].rectTransform.anchoredPosition.x;
            nextWord++;
            Kick(landedX, Centre, hop);
        }

        void Aim(int index)
        {
            float drop = ball.anchoredPosition.y - Centre;
            float wordX = words[index].rectTransform.anchoredPosition.x;
            if (drop < 12f)
            {
                horizontalVelocity = 0f;
                return;
            }

            float a = 0.5f * Gravity;
            float b = -velocity;
            float disc = (b * b) + (2f * Gravity * drop);
            float time = (-b + Mathf.Sqrt(Mathf.Max(0f, disc))) / (2f * a);
            if (time < 0.04f)
                time = 0.04f;
            horizontalVelocity = (wordX - ball.anchoredPosition.x) / time;
        }

        void Kick(float x, float height, float rise)
        {
            ball.anchoredPosition = new Vector2(x, height);
            velocity = Mathf.Sqrt(2f * Gravity * rise);
            horizontalVelocity = 0f;
            aimed = false;
            ball.localScale = new Vector3(1.38f, 0.62f, 1f);
        }

        void PlaceShadow()
        {
            float height = Mathf.Max(0f, ball.anchoredPosition.y - Centre);
            float travel = Mathf.Clamp01(height / apex);
            float width = Mathf.Lerp(1.15f, 0.42f, travel);
            shadow.rectTransform.anchoredPosition = new Vector2(ball.anchoredPosition.x, -10f);
            shadow.rectTransform.localScale = new Vector3(width, Mathf.Lerp(1f, 0.55f, travel), 1f);
            Color color = shadow.color;
            color.a = Mathf.Lerp(0.2f, 0.04f, travel);
            shadow.color = color;
        }

        void Cover(float step)
        {
            cover += step;
            float rush = Mathf.Clamp01(cover / 0.48f);
            rush = rush * rush;
            ball.anchoredPosition = Vector2.Lerp(ball.anchoredPosition, Vector2.zero, 1f - Mathf.Exp(-10f * step));
            float scale = Mathf.Lerp(1f, 48f, rush);
            ball.localScale = new Vector3(scale, scale, 1f);
            ground.color = Color.Lerp(Paper, Pink, rush);
            Color shade = shadow.color;
            shade.a = Mathf.Lerp(shade.a, 0f, rush);
            shadow.color = shade;
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

        static float VisibleHalfHeight(Canvas canvas)
        {
            Canvas.ForceUpdateCanvases();
            float scale = canvas.scaleFactor;
            if (scale < 0.01f)
                scale = 1f;

            float height = canvas.pixelRect.height / scale;
            if (height < 400f)
                height = 1920f;
            return height * 0.5f;
        }

        static Text[] CreateWords(Transform parent, Font typeface)
        {
            const float maxWidth = 980f;
            int fontSize = 46;
            float gap = 16f;
            Text[] placed = new Text[Line.Length];
            for (int i = 0; i < Line.Length; i++)
                placed[i] = CreateWord(parent, typeface, Line[i], fontSize);

            float[] widths = new float[Line.Length];
            for (int attempt = 0; attempt < 20; attempt++)
            {
                float width = gap * (Line.Length - 1);
                for (int i = 0; i < Line.Length; i++)
                {
                    placed[i].fontSize = fontSize;
                    widths[i] = placed[i].preferredWidth;
                    if (widths[i] < 8f)
                        widths[i] = Line[i].Length * fontSize * 0.56f;
                    width += widths[i];
                }

                if (width <= maxWidth || fontSize <= 30)
                    break;

                if (gap > 10f)
                    gap -= 2f;
                else
                    fontSize -= 2;
            }

            float total = gap * (Line.Length - 1);
            for (int i = 0; i < Line.Length; i++)
                total += widths[i];

            float x = -total * 0.5f;
            float y = -40f;
            for (int i = 0; i < Line.Length; i++)
            {
                RectTransform rect = placed[i].rectTransform;
                rect.sizeDelta = new Vector2(widths[i] + 4f, fontSize + 18f);
                rect.anchoredPosition = new Vector2(x + (widths[i] * 0.5f), y);
                x += widths[i] + gap;
            }

            return placed;
        }

        static Text CreateWord(Transform parent, Font typeface, string message, int fontSize)
        {
            GameObject host = new GameObject(message);
            host.transform.SetParent(parent, false);
            Text text = host.AddComponent<Text>();
            text.font = typeface;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = message;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(240f, 72f);
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
            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            float centre = (size - 1) * 0.5f;
            float radius = centre - 1.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - centre;
                    float dy = y - centre;
                    float distance = Mathf.Sqrt((dx * dx) + (dy * dy));
                    float edge = Mathf.Clamp01((radius + 1.4f - distance) / 2.6f);
                    if (edge <= 0f)
                    {
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, 0f));
                        continue;
                    }

                    float nx = dx / radius;
                    float ny = dy / radius;
                    float light = Mathf.Clamp01(0.42f + (ny * 0.34f) - (nx * 0.16f));
                    float highlight = Mathf.Clamp01(1f - (Mathf.Sqrt(((nx + 0.28f) * (nx + 0.28f)) + ((ny - 0.34f) * (ny - 0.34f))) / 0.46f));
                    highlight = highlight * highlight;
                    Color color = Color.Lerp(DeepPink, Pink, light);
                    color = Color.Lerp(color, Color.white, highlight * 0.9f);
                    color.a = edge;
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }

        static Sprite ShadowSprite()
        {
            const int width = 64;
            const int height = 24;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nx = (x - (width * 0.5f)) / (width * 0.5f);
                    float ny = (y - (height * 0.5f)) / (height * 0.5f);
                    float falloff = Mathf.Clamp01(1f - ((nx * nx) + (ny * ny)));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, falloff * falloff));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f));
        }
    }
}
