using System;
using UnityEngine;
using UnityEngine.UI;

namespace Capstone.Placement
{
    /// <summary>
    /// The first screen starts white. A pink ball drops, bounces to the top,
    /// then turns each word of one sentence pink. It then comes toward the camera until the page is pink.
    /// </summary>
    public sealed class OpeningSequence : MonoBehaviour
    {
        static readonly string[][] Lines =
        {
            new[] { "See", "it", "on", "your", "wall", "at", "its" },
            new[] { "real", "size,", "and", "ask", "one", "thing." }
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
        float horizontalVelocity;
        int nextWord;
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

            words = CreateWords(canvasObject.transform, typeface);

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
            if (aimed)
                position.x += horizontalVelocity * step;
            ball.anchoredPosition = position;
            ball.localScale = Vector3.Lerp(ball.localScale, Vector3.one, 1f - Mathf.Exp(-10f * step));

            if (!towardWords)
            {
                if (velocity < 0f && position.y <= Middle)
                    Kick(0f, Middle, 1000f);
                else if (velocity > 0f && position.y >= 980f)
                    towardWords = true;
                return;
            }

            if (!aimed && velocity <= 0f)
            {
                Aim(nextWord);
                aimed = true;
            }

            if (!aimed || velocity >= 0f || nextWord >= words.Length)
                return;

            float contact = words[nextWord].rectTransform.anchoredPosition.y + 58f;
            if (position.y > contact)
                return;

            words[nextWord].color = Pink;
            if (nextWord == words.Length - 1)
            {
                covering = true;
                cover = 0f;
                return;
            }

            nextWord++;
            Kick(words[nextWord - 1].rectTransform.anchoredPosition.x, contact, 120f);
        }

        void Aim(int index)
        {
            Vector2 word = words[index].rectTransform.anchoredPosition;
            float contact = word.y + 58f;
            float drop = ball.anchoredPosition.y - contact;
            if (drop < 12f)
            {
                horizontalVelocity = 0f;
                return;
            }

            float a = 0.5f * Gravity;
            float b = -velocity;
            float disc = (b * b) + (2f * Gravity * drop);
            float time = (-b + Mathf.Sqrt(Mathf.Max(0f, disc))) / (2f * a);
            if (time < 0.05f)
                time = 0.05f;
            horizontalVelocity = (word.x - ball.anchoredPosition.x) / time;
        }

        void Kick(float x, float height, float rise)
        {
            ball.anchoredPosition = new Vector2(x, height);
            velocity = Mathf.Sqrt(2f * Gravity * rise);
            horizontalVelocity = 0f;
            aimed = false;
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

        static Text[] CreateWords(Transform parent, Font typeface)
        {
            int count = 0;
            for (int row = 0; row < Lines.Length; row++)
                count += Lines[row].Length;

            Text[] words = new Text[count];
            int index = 0;
            for (int row = 0; row < Lines.Length; row++)
            {
                string[] line = Lines[row];
                float width = 0f;
                Text[] placed = new Text[line.Length];
                float[] widths = new float[line.Length];
                for (int column = 0; column < line.Length; column++)
                {
                    placed[column] = CreateWord(parent, typeface, line[column]);
                    widths[column] = placed[column].preferredWidth;
                    if (widths[column] < 8f)
                        widths[column] = line[column].Length * 34f;
                    width += widths[column];
                }

                const float gap = 22f;
                width += gap * (line.Length - 1);
                float x = -width * 0.5f;
                float y = -210f - (row * 108f);
                for (int column = 0; column < line.Length; column++)
                {
                    RectTransform rect = placed[column].rectTransform;
                    rect.sizeDelta = new Vector2(widths[column] + 8f, 80f);
                    rect.anchoredPosition = new Vector2(x + (widths[column] * 0.5f), y);
                    x += widths[column] + gap;
                    words[index] = placed[column];
                    index++;
                }
            }

            return words;
        }

        static Text CreateWord(Transform parent, Font typeface, string message)
        {
            GameObject host = new GameObject(message);
            host.transform.SetParent(parent, false);
            Text text = host.AddComponent<Text>();
            text.font = typeface;
            text.fontSize = 58;
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
            rect.sizeDelta = new Vector2(400f, 80f);
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
