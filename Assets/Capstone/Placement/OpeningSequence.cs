using System;
using UnityEngine;
using UnityEngine.UI;

namespace Capstone.Placement
{
    /// <summary>
    /// The first screen is blue. A white ball bounces on each word and turns that word white.
    /// The ball then comes toward the camera until the page is white, and a translucent blue body floats.
    /// Soft blue cards then float in a row. A tap starts the session.
    /// </summary>
    public sealed class OpeningSequence : MonoBehaviour
    {
        static readonly string[] Line =
        {
            "See", "it", "on", "your", "wall", "at", "its", "real", "size,", "and", "ask", "one", "thing."
        };

        static readonly Color Paper = Color.white;
        static readonly Color BlueTop = new Color(0.22f, 0.52f, 1f, 1f);
        static readonly Color BlueDeep = new Color(0.04f, 0.22f, 0.86f, 1f);
        const float Gravity = 10800f;
        const float Centre = 0f;
        const float BallSize = 58f;

        Action finished;
        Image field;
        Image shadow;
        RectTransform ball;
        RectTransform orb;
        RectTransform ribbonA;
        RectTransform ribbonB;
        CanvasGroup orbFade;
        RectTransform[] cards;
        RectTransform[] blobs;
        CanvasGroup[] cardFade;
        float[] cardHomeX;
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
        float coverTime;
        Vector2 coverOrigin;
        bool resting;
        float restTime;
        bool dealing;
        float dealTime;
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

            Image ground = CreateImage(canvasObject.transform, "Paper", Paper, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Button screenTap = ground.gameObject.AddComponent<Button>();
            screenTap.targetGraphic = ground;
            screenTap.onClick.AddListener(Finish);

            field = CreateImage(canvasObject.transform, "Blue field", Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            field.sprite = VerticalGradient(BlueTop, BlueDeep);
            field.raycastTarget = false;

            float half = VisibleHalfHeight(canvas);
            apex = half * 0.9f;
            hop = half * 0.42f;
            words = CreateWords(canvasObject.transform, typeface);

            shadow = CreateImage(
                canvasObject.transform,
                "Shadow",
                new Color(0.02f, 0.08f, 0.28f, 0.2f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -18f),
                new Vector2(70f, 18f));
            shadow.sprite = SoftEllipse();
            shadow.raycastTarget = false;

            Image ballImage = CreateImage(
                canvasObject.transform,
                "Ball",
                Color.white,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, half + BallSize),
                new Vector2(BallSize, BallSize));
            ballImage.sprite = BallSprite();
            ballImage.raycastTarget = false;
            ball = ballImage.rectTransform;

            GameObject orbObject = new GameObject("Glass body", typeof(RectTransform));
            orbObject.transform.SetParent(canvasObject.transform, false);
            orb = orbObject.GetComponent<RectTransform>();
            orb.anchorMin = new Vector2(0.5f, 0.5f);
            orb.anchorMax = new Vector2(0.5f, 0.5f);
            orb.pivot = new Vector2(0.5f, 0.5f);
            orb.sizeDelta = new Vector2(640f, 640f);
            orb.anchoredPosition = new Vector2(0f, 80f);
            orb.localScale = Vector3.one * 0.1f;
            ribbonA = Ribbon(orb, "Ribbon", 0);
            ribbonB = Ribbon(orb, "Inner ribbon", 1);
            Image glass = CreateImage(orb, "Glass", Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            glass.sprite = GlassShell();
            glass.raycastTarget = false;
            orbFade = orbObject.AddComponent<CanvasGroup>();
            orb.gameObject.SetActive(false);
            CreateCards(canvasObject.transform, typeface);
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

            if (dealing)
            {
                Deal(step);
                return;
            }

            if (resting)
            {
                Rest(step);
                return;
            }

            velocity -= Gravity * step;
            Vector2 position = ball.anchoredPosition;
            position.y += velocity * step;
            if (aimed)
                position.x += horizontalVelocity * step;
            ball.anchoredPosition = position;
            ball.localScale = Vector3.Lerp(ball.localScale, Vector3.one, 1f - Mathf.Exp(-28f * step));
            PlaceBounceShadow();

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

            words[nextWord].color = Color.white;
            if (nextWord == words.Length - 1)
            {
                covering = true;
                coverTime = 0f;
                coverOrigin = new Vector2(words[nextWord].rectTransform.anchoredPosition.x, Centre);
                ball.anchoredPosition = coverOrigin;
                ball.localScale = Vector3.one;
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

        void PlaceBounceShadow()
        {
            float height = Mathf.Max(0f, ball.anchoredPosition.y - Centre);
            float travel = Mathf.Clamp01(height / apex);
            shadow.rectTransform.anchoredPosition = new Vector2(ball.anchoredPosition.x, -16f);
            shadow.rectTransform.sizeDelta = new Vector2(70f, 18f);
            float width = Mathf.Lerp(1.1f, 0.4f, travel);
            shadow.rectTransform.localScale = new Vector3(width, Mathf.Lerp(1f, 0.55f, travel), 1f);
            Color color = shadow.color;
            color.a = Mathf.Lerp(0.22f, 0.04f, travel);
            shadow.color = color;
        }

        void Cover(float step)
        {
            coverTime += step;
            float t = Mathf.Clamp01(coverTime / 0.95f);
            float rush = t * t;
            ball.anchoredPosition = Vector2.Lerp(coverOrigin, Vector2.zero, rush);
            float scale = Mathf.Lerp(1f, 56f, rush);
            ball.localScale = new Vector3(scale, scale, 1f);
            Color fieldColor = field.color;
            fieldColor.a = 1f - rush;
            field.color = fieldColor;
            for (int i = 0; i < words.Length; i++)
            {
                Color color = words[i].color;
                color.a = 1f - rush;
                words[i].color = color;
            }

            Color shade = shadow.color;
            shade.a = Mathf.Lerp(0.2f, 0f, rush);
            shadow.color = shade;
            if (t < 1f)
                return;

            covering = false;
            resting = true;
            restTime = 0f;
            ball.gameObject.SetActive(false);
            field.color = new Color(1f, 1f, 1f, 0f);
            shadow.color = new Color(shade.r, shade.g, shade.b, 0f);
        }

        void Rest(float step)
        {
            restTime += step;
            float arrive = Mathf.Clamp01((restTime - 0.12f) / 0.8f);
            float eased = 1f - ((1f - arrive) * (1f - arrive));
            if (!orb.gameObject.activeSelf)
            {
                orb.gameObject.SetActive(true);
                orb.anchoredPosition = new Vector2(0f, 40f);
                orb.localScale = Vector3.one * 0.08f;
            }

            float breathe = 1f + (Mathf.Sin(restTime * 1.4f) * 0.018f);
            orb.localScale = Vector3.one * Mathf.Lerp(0.08f, 1f, eased) * breathe;
            float lift = Mathf.Sin(restTime * 1.15f) * 22f;
            Vector2 home = new Vector2(Mathf.Sin(restTime * 0.7f) * 10f, 90f + lift);
            orb.anchoredPosition = Vector2.Lerp(new Vector2(0f, 40f), home, eased);
            ribbonA.localRotation = Quaternion.Euler(0f, 0f, restTime * 26f);
            ribbonB.localRotation = Quaternion.Euler(0f, 0f, -restTime * 17f);

            float height = Mathf.InverseLerp(-22f, 22f, lift);
            shadow.rectTransform.sizeDelta = new Vector2(420f, 90f);
            shadow.rectTransform.anchoredPosition = new Vector2(orb.anchoredPosition.x, -250f);
            shadow.rectTransform.localScale = new Vector3(Mathf.Lerp(1.05f, 0.72f, height), 1f, 1f);
            Color shade = shadow.color;
            shade.a = Mathf.Lerp(0f, 0.18f, eased) * Mathf.Lerp(1f, 0.55f, height);
            shadow.color = shade;
            if (restTime < 2.15f)
                return;

            resting = false;
            dealing = true;
            dealTime = 0f;
            for (int i = 0; i < cards.Length; i++)
                cards[i].gameObject.SetActive(true);
        }

        void Deal(float step)
        {
            dealTime += step;
            float leave = Mathf.Clamp01(dealTime / 0.45f);
            orb.localScale = Vector3.one * Mathf.Lerp(1f, 0.12f, leave);
            orbFade.alpha = 1f - leave;
            Color shade = shadow.color;
            shade.a = Mathf.Lerp(shade.a, 0f, leave);
            shadow.color = shade;

            for (int i = 0; i < cards.Length; i++)
            {
                float shown = Mathf.Clamp01((dealTime - 0.1f - (i * 0.07f)) / 0.45f);
                shown = 1f - ((1f - shown) * (1f - shown));
                float bob = Mathf.Sin((dealTime * 1.15f) + (i * 0.9f)) * 12f;
                cards[i].anchoredPosition = new Vector2(cardHomeX[i], Mathf.Lerp(80f, -16f + bob, shown));
                cards[i].localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, shown);
                cardFade[i].alpha = shown;
                float driftX = Mathf.Sin((dealTime * 0.8f) + (i * 1.3f)) * 16f;
                float driftY = Mathf.Cos((dealTime * 1.05f) + i) * 12f;
                blobs[i].anchoredPosition = new Vector2(driftX, 6f + driftY);
            }
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
            for (int i = 0; i < Line.Length; i++)
            {
                RectTransform rect = placed[i].rectTransform;
                rect.sizeDelta = new Vector2(widths[i] + 4f, fontSize + 18f);
                rect.anchoredPosition = new Vector2(x + (widths[i] * 0.5f), -42f);
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
            text.color = new Color(1f, 1f, 1f, 0f);
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

        void CreateCards(Transform parent, Font typeface)
        {
            string[] titles =
            {
                "Choose\na work",
                "Place it\non your wall",
                "Ask\none thing",
                "See\nthree"
            };
            Color[] tops =
            {
                new Color(0.78f, 0.9f, 1f, 0.62f),
                new Color(0.62f, 0.8f, 1f, 0.58f),
                new Color(0.7f, 0.86f, 1f, 0.6f),
                new Color(0.55f, 0.76f, 1f, 0.56f)
            };
            Color[] bottoms =
            {
                new Color(0.45f, 0.68f, 0.98f, 0.34f),
                new Color(0.36f, 0.58f, 0.96f, 0.32f),
                new Color(0.42f, 0.64f, 0.98f, 0.32f),
                new Color(0.3f, 0.52f, 0.94f, 0.3f)
            };
            cards = new RectTransform[titles.Length];
            blobs = new RectTransform[titles.Length];
            cardFade = new CanvasGroup[titles.Length];
            cardHomeX = new float[titles.Length];
            Sprite blobSprite = SoftBlob();
            const float width = 228f;
            const float gap = 16f;
            float total = (width * titles.Length) + (gap * (titles.Length - 1));
            float x = (-total * 0.5f) + (width * 0.5f);
            for (int i = 0; i < titles.Length; i++)
            {
                GameObject host = new GameObject("Card", typeof(RectTransform));
                host.transform.SetParent(parent, false);
                RectTransform rect = host.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(width, 300f);
                rect.anchoredPosition = new Vector2(x, 80f);
                Image face = host.AddComponent<Image>();
                face.sprite = RoundedGradient(tops[i], bottoms[i]);
                face.raycastTarget = false;
                cardFade[i] = host.AddComponent<CanvasGroup>();
                cardFade[i].alpha = 0f;
                Image blob = CreateImage(
                    rect,
                    "Blob",
                    Color.white,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 6f),
                    new Vector2(108f, 108f));
                blob.sprite = blobSprite;
                blob.raycastTarget = false;
                Text title = CreateWord(rect, typeface, titles[i], 22);
                title.color = new Color(0.1f, 0.28f, 0.62f, 1f);
                title.rectTransform.anchoredPosition = new Vector2(0f, 92f);
                title.rectTransform.sizeDelta = new Vector2(200f, 80f);
                host.SetActive(false);
                cards[i] = rect;
                blobs[i] = blob.rectTransform;
                cardHomeX[i] = x;
                x += width + gap;
            }
        }

        static Sprite SoftBlob()
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
                    float dx = (x - centre) / centre;
                    float dy = (y - centre) / centre;
                    float falloff = Mathf.Clamp01(1f - Mathf.Sqrt((dx * dx) + (dy * dy)));
                    falloff = falloff * falloff;
                    texture.SetPixel(x, y, new Color(0.72f, 0.88f, 1f, falloff * 0.9f));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }

        static Sprite RoundedGradient(Color top, Color bottom)
        {
            const int width = 140;
            const int height = 208;
            const float radius = 28f;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            float halfWidth = (width * 0.5f) - radius;
            float halfHeight = (height * 0.5f) - radius;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float px = x - ((width - 1) * 0.5f);
                    float py = y - ((height - 1) * 0.5f);
                    float qx = Mathf.Abs(px) - halfWidth;
                    float qy = Mathf.Abs(py) - halfHeight;
                    float outside = Mathf.Sqrt((Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f)) + (Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f)));
                    float box = Mathf.Min(Mathf.Max(qx, qy), 0f);
                    float distance = outside + box - radius;
                    float edge = Mathf.Clamp01((1.4f - distance) / 2.4f);
                    Color color = Color.Lerp(bottom, top, y / (height - 1f));
                    color.a *= edge;
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f));
        }

        static RectTransform Ribbon(Transform parent, string name, int band)
        {
            Image image = CreateImage(parent, name, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            image.sprite = GlassRibbon(band);
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

        static Sprite VerticalGradient(Color top, Color bottom)
        {
            Texture2D texture = new Texture2D(4, 64, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < 64; y++)
            {
                Color color = Color.Lerp(bottom, top, y / 63f);
                for (int x = 0; x < 4; x++)
                    texture.SetPixel(x, y, color);
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 4f, 64f), new Vector2(0.5f, 0.5f));
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
                    float edge = Mathf.Clamp01((radius + 1.4f - distance) / 2.4f);
                    if (edge <= 0f)
                    {
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, 0f));
                        continue;
                    }

                    float nx = dx / radius;
                    float ny = dy / radius;
                    float light = Mathf.Clamp01(0.72f + (ny * 0.22f) - (nx * 0.08f));
                    float highlight = Mathf.Clamp01(1f - (Mathf.Sqrt(((nx + 0.28f) * (nx + 0.28f)) + ((ny - 0.34f) * (ny - 0.34f))) / 0.42f));
                    highlight = highlight * highlight;
                    Color color = Color.Lerp(new Color(0.78f, 0.88f, 1f, 1f), Color.white, light);
                    color = Color.Lerp(color, Color.white, highlight * 0.95f);
                    color.a = edge;
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }

        static Sprite GlassShell()
        {
            const int size = 256;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            float centre = (size - 1) * 0.5f;
            float radius = centre - 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - centre) / radius;
                    float dy = (y - centre) / radius;
                    float distance = Mathf.Sqrt((dx * dx) + (dy * dy));
                    if (distance > 1.02f)
                    {
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, 0f));
                        continue;
                    }

                    float edge = Mathf.Clamp01((1.01f - distance) / 0.035f);
                    float rim = Mathf.Clamp01((distance - 0.78f) / 0.18f) * Mathf.Clamp01((1f - distance) / 0.08f);
                    float body = Mathf.Clamp01(1f - distance) * 0.1f;
                    float highlight = Mathf.Clamp01(1f - (Mathf.Sqrt(((dx + 0.32f) * (dx + 0.32f)) + ((dy - 0.38f) * (dy - 0.38f))) / 0.42f));
                    highlight = highlight * highlight;
                    Color color = Color.Lerp(new Color(0.35f, 0.62f, 1f, 1f), new Color(0.78f, 0.9f, 1f, 1f), highlight);
                    color.a = Mathf.Clamp01((body + (rim * 0.42f) + (highlight * 0.28f)) * edge);
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }

        static Sprite GlassRibbon(int band)
        {
            const int size = 256;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            float centre = (size - 1) * 0.5f;
            float radius = centre - 2f;
            Color inner = band == 0
                ? new Color(0.45f, 0.72f, 1f, 1f)
                : new Color(0.12f, 0.38f, 0.92f, 1f);
            Color outer = band == 0
                ? new Color(0.7f, 0.86f, 1f, 1f)
                : new Color(0.28f, 0.58f, 1f, 1f);
            float near = band == 0 ? 0.34f : 0.5f;
            float far = band == 0 ? 0.62f : 0.78f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - centre) / radius;
                    float dy = (y - centre) / radius;
                    float distance = Mathf.Sqrt((dx * dx) + (dy * dy));
                    float angle = Mathf.Atan2(dy, dx);
                    float across = Mathf.Clamp01((distance - near) / 0.08f) * Mathf.Clamp01((far - distance) / 0.08f);
                    float sweep = Mathf.Clamp01(0.5f + (0.5f * Mathf.Sin((angle * (band == 0 ? 1.4f : 2f)) + (band * 1.2f))));
                    float alpha = across * sweep * (band == 0 ? 0.34f : 0.26f);
                    if (distance > 0.96f)
                        alpha = 0f;
                    float mix = Mathf.Clamp01(0.5f + (0.5f * Mathf.Sin(angle + distance)));
                    Color color = Color.Lerp(inner, outer, mix);
                    color.a = alpha;
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }

        static Sprite SoftEllipse()
        {
            const int width = 128;
            const int height = 48;
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
