using System;
using UnityEngine;
using UnityEngine.UI;

namespace Capstone.Placement
{
    /// <summary>
    /// The first screen after the app opens: sketch stars with no words.
    /// Each crayon layer and each ink line drifts on its own.
    /// A tap leaves this screen and starts the session.
    /// </summary>
    public sealed class OpeningSequence : MonoBehaviour
    {
        static readonly Color Paper = new Color(0.965f, 0.965f, 0.969f, 1f);
        static readonly Color Pink = new Color(0.957f, 0.549f, 0.690f, 1f);
        static readonly Color Blue = new Color(0.494f, 0.729f, 0.910f, 1f);
        static readonly Color Ink = new Color(0.08f, 0.08f, 0.09f, 1f);

        Action finished;
        float clock;
        RectTransform[] layers;
        Vector2[] homes;
        float[] speeds;
        float[] phases;
        float[] spins;
        float[] drifts;

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

            layers = new RectTransform[7];
            homes = new Vector2[7];
            speeds = new[] { 0.37f, 0.91f, 0.52f, 1.15f, 0.74f, 0.44f, 0.83f };
            phases = new[] { 0.2f, 1.7f, 3.1f, 0.8f, 2.4f, 4.2f, 1.1f };
            spins = new[] { 4f, 6f, 3.5f, 7f, 5f, 3f, 6.5f };
            drifts = new[] { 16f, 22f, 14f, 20f, 11f, 18f, 15f };
            homes[0] = new Vector2(-220f, -50f);
            homes[1] = new Vector2(-160f, 30f);
            homes[2] = new Vector2(170f, -50f);
            homes[3] = new Vector2(240f, 30f);
            homes[4] = new Vector2(-295f, 105f);
            homes[5] = new Vector2(-195f, -15f);
            homes[6] = new Vector2(205f, -10f);

            layers[0] = Piece(canvasObject.transform, "Left pink", Crayon(155f, 60f, -8f, 3, Pink), homes[0]);
            layers[1] = Piece(canvasObject.transform, "Left blue", Crayon(105f, 40f, 14f, 9, Blue), homes[1]);
            layers[2] = Piece(canvasObject.transform, "Right pink", Crayon(155f, 60f, 2f, 5, Pink), homes[2]);
            layers[3] = Piece(canvasObject.transform, "Right blue", Crayon(110f, 42f, 18f, 12, Blue), homes[3]);
            layers[4] = Piece(canvasObject.transform, "Small ink", InkStar(58f, 22f, 16f, 7), homes[4]);
            layers[5] = Piece(canvasObject.transform, "Left ink", InkStar(145f, 55f, -6f, 4), homes[5]);
            layers[6] = Piece(canvasObject.transform, "Right ink", InkStar(145f, 55f, 0f, 8), homes[6]);
        }

        void Update()
        {
            clock += Time.deltaTime;
            for (int i = 0; i < layers.Length; i++)
            {
                float time = (clock * speeds[i]) + phases[i];
                layers[i].anchoredPosition = homes[i] + new Vector2(
                    Mathf.Sin(time) * drifts[i],
                    Mathf.Cos(time * 0.67f) * drifts[i] * 0.85f);
                layers[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 0.55f) * spins[i]);
            }
        }

        void Finish()
        {
            Action done = finished;
            finished = null;
            if (done != null)
                done();
            Destroy(gameObject);
        }

        static RectTransform Piece(Transform parent, string name, Sprite sprite, Vector2 home)
        {
            Image image = CreateImage(
                parent,
                name,
                Color.white,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                home,
                new Vector2(sprite.rect.width, sprite.rect.height));
            image.sprite = sprite;
            image.raycastTarget = false;
            return image.rectTransform;
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

        static Sprite Crayon(float outer, float inner, float rotation, int seed, Color color)
        {
            return DrawStar(outer, inner, rotation, seed, color, false);
        }

        static Sprite InkStar(float outer, float inner, float rotation, int seed)
        {
            return DrawStar(outer, inner, rotation, seed, Ink, true);
        }

        static Sprite DrawStar(float outer, float inner, float rotation, int seed, Color color, bool ink)
        {
            int size = Mathf.CeilToInt((outer + 36f) * 2f);
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            Color clear = new Color(1f, 1f, 1f, 0f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                    texture.SetPixel(x, y, clear);
            }

            float centre = size * 0.5f;
            Vector2[] shape = Wobble(Star(centre, centre, outer, inner, rotation, seed), seed);
            if (ink)
                Stroke(texture, shape, seed);
            else
                Shade(texture, shape, color, seed);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }

        static Vector2[] Star(float centreX, float centreY, float outer, float inner, float rotationDegrees, int seed)
        {
            Vector2[] points = new Vector2[10];
            float rotation = rotationDegrees * Mathf.Deg2Rad;
            for (int i = 0; i < 10; i++)
            {
                float angle = rotation - (Mathf.PI * 0.5f) + (i * Mathf.PI / 5f);
                float radius = i % 2 == 0 ? outer : inner;
                radius *= 0.86f + (Hash(i, seed, 1) * 0.28f);
                points[i] = new Vector2(
                    centreX + (Mathf.Cos(angle) * radius),
                    centreY + (Mathf.Sin(angle) * radius));
            }

            return points;
        }

        static Vector2[] Wobble(Vector2[] corners, int seed)
        {
            const int steps = 4;
            Vector2[] points = new Vector2[corners.Length * steps];
            int index = 0;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector2 start = corners[i];
                Vector2 end = corners[(i + 1) % corners.Length];
                Vector2 side = end - start;
                float length = side.magnitude;
                Vector2 normal = length < 0.001f ? Vector2.up : new Vector2(-side.y / length, side.x / length);
                for (int step = 0; step < steps; step++)
                {
                    float along = step / (float)steps;
                    Vector2 point = Vector2.Lerp(start, end, along);
                    if (step > 0)
                        point += normal * ((Hash(index, seed, 2) - 0.5f) * 18f);
                    points[index] = point;
                    index += 1;
                }
            }

            return points;
        }

        static void Shade(Texture2D texture, Vector2[] points, Color color, int seed)
        {
            BoundsOf(points, out int minX, out int maxX, out int minY, out int maxY);
            maxX = Mathf.Min(texture.width - 1, maxX);
            maxY = Mathf.Min(texture.height - 1, maxY);
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (!Inside(x, y, points))
                        continue;

                    float grain = Hash(x / 3, y / 3, seed);
                    float streak = Mathf.Sin((x * 0.28f) + (y * 0.11f) + (seed * 0.7f));
                    if (grain < 0.16f || streak < -0.72f)
                        continue;

                    Color crayon = Color.Lerp(color, Color.white, (grain - 0.35f) * 0.45f);
                    crayon.a = 0.42f + (grain * 0.58f);
                    texture.SetPixel(x, texture.height - 1 - y, crayon);
                }
            }
        }

        static void Stroke(Texture2D texture, Vector2[] points, int seed)
        {
            BoundsOf(points, out int minX, out int maxX, out int minY, out int maxY);
            minX = Mathf.Max(0, minX - 10);
            maxX = Mathf.Min(texture.width - 1, maxX + 10);
            minY = Mathf.Max(0, minY - 10);
            maxY = Mathf.Min(texture.height - 1, maxY + 10);
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float width = 2.4f + (Hash(x / 5, y / 5, seed) * 5.2f);
                    if (EdgeDistance(x, y, points) > width)
                        continue;
                    if (Hash(x / 8, y / 8, seed + 3) < 0.07f)
                        continue;

                    Color line = Ink;
                    line.a = 0.72f + (Hash(x, y, seed) * 0.28f);
                    texture.SetPixel(x, texture.height - 1 - y, line);
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

            minX = Mathf.Max(0, minX);
            minY = Mathf.Max(0, minY);
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

        static float Hash(int x, int y, int seed)
        {
            int value = (x * 374761393) + (y * 668265263) + (seed * 1442695041);
            value = (value ^ (value >> 13)) * 1274126177;
            return (value & 0x7fffffff) / 2147483647f;
        }
    }
}
