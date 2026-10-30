using System;
using UnityEngine;
using UnityEngine.UI;

namespace Capstone.Placement
{
    /// <summary>
    /// The first screen after the app opens: two drawn stars, floating, with no words.
    /// Pink and blue sit off the black outline. A tap leaves this screen and starts the session.
    /// </summary>
    public sealed class OpeningSequence : MonoBehaviour
    {
        static readonly Color Paper = new Color(0.965f, 0.965f, 0.969f, 1f);
        static readonly Color Pink = new Color(0.957f, 0.549f, 0.690f, 1f);
        static readonly Color Blue = new Color(0.494f, 0.729f, 0.910f, 1f);
        static readonly Color Ink = new Color(0.08f, 0.08f, 0.09f, 1f);

        Action finished;
        float clock;
        RectTransform body;

        public static void Play(Font typeface, Action onFinished)
        {
            GameObject host = new GameObject("Opening");
            OpeningSequence sequence = host.AddComponent<OpeningSequence>();
            sequence.finished = onFinished;
            sequence.Build();
        }

        void Build()
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

            Image ground = CreateImage(canvasObject.transform, "Ground", Paper, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Button screenTap = ground.gameObject.AddComponent<Button>();
            screenTap.targetGraphic = ground;
            screenTap.onClick.AddListener(Finish);

            Image stars = CreateImage(
                canvasObject.transform,
                "Drawn stars",
                Color.white,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(900f, 520f));
            stars.sprite = DrawnStars();
            stars.raycastTarget = false;
            body = stars.rectTransform;
        }

        void Update()
        {
            clock += Time.deltaTime;
            body.anchoredPosition = new Vector2(Mathf.Sin(clock * 0.45f) * 18f, Mathf.Sin(clock * 0.7f) * 28f);
        }

        void Finish()
        {
            Action done = finished;
            finished = null;
            if (done != null)
                done();
            Destroy(gameObject);
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

        static Sprite DrawnStars()
        {
            const int width = 900;
            const int height = 520;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            Color clear = new Color(1f, 1f, 1f, 0f);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                    texture.SetPixel(x, y, clear);
            }

            Fill(texture, Star(230f, 310f, 155f, 60f, -8f), Pink);
            Fill(texture, Star(290f, 230f, 105f, 40f, 14f), Blue);
            Fill(texture, Star(620f, 310f, 155f, 60f, 2f), Pink);
            Fill(texture, Star(690f, 230f, 110f, 42f, 18f), Blue);
            Outline(texture, Star(155f, 155f, 58f, 22f, 16f), 5f);
            Outline(texture, Star(255f, 275f, 145f, 55f, -6f), 6f);
            Outline(texture, Star(655f, 270f, 145f, 55f, 0f), 6f);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f));
        }

        static Vector2[] Star(float centreX, float centreY, float outer, float inner, float rotationDegrees)
        {
            Vector2[] points = new Vector2[10];
            float rotation = rotationDegrees * Mathf.Deg2Rad;
            for (int i = 0; i < 10; i++)
            {
                float angle = rotation - (Mathf.PI * 0.5f) + (i * Mathf.PI / 5f);
                float radius = i % 2 == 0 ? outer : inner;
                points[i] = new Vector2(
                    centreX + (Mathf.Cos(angle) * radius),
                    centreY + (Mathf.Sin(angle) * radius));
            }

            return points;
        }

        static void Fill(Texture2D texture, Vector2[] points, Color color)
        {
            BoundsOf(points, out int minX, out int maxX, out int minY, out int maxY);
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (Inside(x, y, points))
                        texture.SetPixel(x, texture.height - 1 - y, color);
                }
            }
        }

        static void Outline(Texture2D texture, Vector2[] points, float thickness)
        {
            BoundsOf(points, out int minX, out int maxX, out int minY, out int maxY);
            int pad = Mathf.CeilToInt(thickness) + 1;
            minX = Mathf.Max(0, minX - pad);
            maxX = Mathf.Min(texture.width - 1, maxX + pad);
            minY = Mathf.Max(0, minY - pad);
            maxY = Mathf.Min(texture.height - 1, maxY + pad);
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (EdgeDistance(x, y, points) <= thickness)
                        texture.SetPixel(x, texture.height - 1 - y, Ink);
                }
            }
        }

        static void BoundsOf(Vector2[] points, out int minX, out int maxX, out int minY, out int maxY)
        {
            minX = int.MaxValue;
            maxX = int.MinValue;
            minY = int.MaxValue;
            maxY = int.MinValue;
            for (int i = 0; i < points.Length; i++)
            {
                int x = Mathf.RoundToInt(points[i].x);
                int y = Mathf.RoundToInt(points[i].y);
                if (x < minX)
                    minX = x;
                if (x > maxX)
                    maxX = x;
                if (y < minY)
                    minY = y;
                if (y > maxY)
                    maxY = y;
            }
        }

        static bool Inside(float x, float y, Vector2[] points)
        {
            bool inside = false;
            int previous = points.Length - 1;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 current = points[i];
                Vector2 before = points[previous];
                if ((current.y > y) != (before.y > y)
                    && x < (((before.x - current.x) * (y - current.y)) / (before.y - current.y)) + current.x)
                    inside = !inside;
                previous = i;
            }

            return inside;
        }

        static float EdgeDistance(float x, float y, Vector2[] points)
        {
            float best = float.MaxValue;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 start = points[i];
                Vector2 end = points[(i + 1) % points.Length];
                float dx = end.x - start.x;
                float dy = end.y - start.y;
                float length = (dx * dx) + (dy * dy);
                float along = length < 0.001f ? 0f : (((x - start.x) * dx) + ((y - start.y) * dy)) / length;
                along = Mathf.Clamp01(along);
                float px = start.x + (dx * along);
                float py = start.y + (dy * along);
                float distance = Mathf.Sqrt(((x - px) * (x - px)) + ((y - py) * (y - py)));
                if (distance < best)
                    best = distance;
            }

            return best;
        }
    }
}
