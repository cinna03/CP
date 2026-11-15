using System;
using System.Collections.Generic;
using Capstone.App.Data;
using Capstone.App.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Capstone.App.UI
{
    /// <summary>
    /// Draws the studio card. The numbers come from StudioDigest.
    /// </summary>
    public sealed class MainMenuDashboardController : MonoBehaviour
    {
        static readonly Color Ink = new Color32(0x1A, 0x17, 0x18, 0xFF);
        static readonly Color Taupe = new Color32(0x8A, 0x75, 0x6C, 0xFF);
        static readonly Color Rose = new Color32(0xE3, 0x8F, 0xA3, 0xFF);
        static readonly Color Blush = new Color32(0xF8, 0xDC, 0xE3, 0xFF);
        static readonly Color White = new Color32(0xFF, 0xFF, 0xFF, 0xFF);

        readonly TMP_Text[] numbers = new TMP_Text[3];
        readonly TMP_Text[] captions = new TMP_Text[3];
        readonly Image[] fills = new Image[3];
        readonly TMP_Text[] barLabels = new TMP_Text[3];
        readonly RectTransform[] tracks = new RectTransform[3];
        readonly RectTransform[] chipRects = new RectTransform[3];
        readonly Image[] chips = new Image[3];
        readonly Button[] chipButtons = new Button[3];
        TMP_Text empty;
        Action<ArtworkData> openWork;
        readonly ArtworkData[] recent = new ArtworkData[3];

        public void Build(TMP_FontAsset font)
        {
            if (numbers[0] != null)
                return;

            Image card = GetComponent<Image>();
            if (card == null)
                card = gameObject.AddComponent<Image>();
            card.sprite = Pill(18);
            card.type = Image.Type.Sliced;
            card.color = White;
            card.raycastTarget = false;
            if (GetComponent<RectMask2D>() == null)
                gameObject.AddComponent<RectMask2D>();

            string[] labels = { "WORKS EXPLORED", "SAVED HEARTS", "WALL PHOTOS" };
            for (int i = 0; i < 3; i++)
            {
                numbers[i] = Word(transform, "0", font, 48f, Ink, FontStyles.Bold, new Vector2(0f, 0f), new Vector2(260f, 56f));
                captions[i] = Word(transform, labels[i], font, 16f, Taupe, FontStyles.Normal, new Vector2(0f, 0f), new Vector2(260f, 24f));
            }

            for (int i = 0; i < 3; i++)
            {
                float y = 48f - i * 36f;
                barLabels[i] = Word(transform, "", font, 16f, Taupe, FontStyles.Normal, new Vector2(-250f, y), new Vector2(160f, 28f));
                barLabels[i].alignment = TextAlignmentOptions.MidlineRight;
                var trackObject = new GameObject("Track", typeof(RectTransform), typeof(Image));
                trackObject.transform.SetParent(transform, false);
                var track = trackObject.GetComponent<RectTransform>();
                track.anchorMin = track.anchorMax = new Vector2(0.5f, 0.5f);
                track.sizeDelta = new Vector2(420f, 16f);
                track.anchoredPosition = new Vector2(80f, y);
                tracks[i] = track;
                Image trackImage = trackObject.GetComponent<Image>();
                trackImage.sprite = Pill(8);
                trackImage.type = Image.Type.Sliced;
                trackImage.color = Blush;
                trackImage.raycastTarget = false;
                var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                fillObject.transform.SetParent(track, false);
                var fillRect = fillObject.GetComponent<RectTransform>();
                fillRect.anchorMin = new Vector2(0f, 0f);
                fillRect.anchorMax = new Vector2(0f, 1f);
                fillRect.pivot = new Vector2(0f, 0.5f);
                fillRect.sizeDelta = new Vector2(8f, 0f);
                fillRect.anchoredPosition = Vector2.zero;
                fills[i] = fillObject.GetComponent<Image>();
                fills[i].sprite = Pill(8);
                fills[i].type = Image.Type.Sliced;
                fills[i].color = Rose;
                fills[i].raycastTarget = false;
            }

            empty = Word(transform, "No recent pieces yet — explore Catalogue", font, 18f, Taupe, FontStyles.Italic, Vector2.zero, new Vector2(760f, 40f));
            for (int i = 0; i < 3; i++)
            {
                var chip = new GameObject("Recent", typeof(RectTransform), typeof(Image), typeof(Button));
                chip.transform.SetParent(transform, false);
                var rect = chip.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(84f, 84f);
                chipRects[i] = rect;
                chips[i] = chip.GetComponent<Image>();
                chips[i].sprite = Pill(12);
                chips[i].type = Image.Type.Sliced;
                chips[i].color = Blush;
                chipButtons[i] = chip.GetComponent<Button>();
                chipButtons[i].targetGraphic = chips[i];
                int index = i;
                chipButtons[i].onClick.AddListener(() => Choose(index));
                chip.SetActive(false);
            }

            Arrange();
        }

        public void Show(ArtworkSessionState session, IReadOnlyList<WallSnapshotRecord> photos, TMP_FontAsset font, Action<ArtworkData> onOpen)
        {
            Build(font);
            openWork = onOpen;
            StudioDigest digest = StudioDigest.Read(session, photos);
            numbers[0].text = digest.Explored.ToString();
            numbers[1].text = digest.Hearts.ToString();
            numbers[2].text = digest.Photos.ToString();

            float peak = 1f;
            for (int i = 0; i < digest.Bars.Count; i++)
                peak = Mathf.Max(peak, digest.Bars[i].Weight);
            for (int i = 0; i < 3; i++)
                barLabels[i].text = i < digest.Bars.Count ? digest.Bars[i].Label.ToUpperInvariant() : "";

            bool any = digest.Recent.Count > 0;
            empty.gameObject.SetActive(!any);
            for (int i = 0; i < 3; i++)
            {
                bool show = i < digest.Recent.Count && digest.Recent[i] != null;
                chips[i].gameObject.SetActive(show);
                recent[i] = show ? digest.Recent[i] : null;
                if (!show)
                    continue;
                chips[i].sprite = digest.Recent[i].ArtworkSprite != null ? digest.Recent[i].ArtworkSprite : Pill(12);
                chips[i].type = digest.Recent[i].ArtworkSprite != null ? Image.Type.Simple : Image.Type.Sliced;
                chips[i].preserveAspect = digest.Recent[i].ArtworkSprite != null;
                chips[i].color = Color.white;
            }

            Arrange();
            for (int i = 0; i < 3; i++)
            {
                bool on = i < digest.Bars.Count && tracks[i] != null;
                float trackWidth = on ? tracks[i].rect.width : 0f;
                if (trackWidth < 8f && on)
                    trackWidth = tracks[i].sizeDelta.x;
                float width = on ? Mathf.Max(12f, trackWidth * (digest.Bars[i].Weight / peak)) : 0f;
                fills[i].rectTransform.sizeDelta = new Vector2(Mathf.Min(width, trackWidth), 0f);
            }
        }

        void Arrange()
        {
            RectTransform rect = transform as RectTransform;
            float height = rect != null ? rect.rect.height : 0f;
            float width = rect != null ? rect.rect.width : 0f;
            if (height < 32f)
                height = 360f;
            if (width < 32f)
                width = 900f;

            float top = height * 0.5f;
            float span = Mathf.Min(300f, width * 0.3f);
            float numberSize = height < 320f ? 36f : 48f;
            float y = top - 28f;
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * span;
                numbers[i].fontSize = numberSize;
                numbers[i].rectTransform.sizeDelta = new Vector2(span - 12f, numberSize + 8f);
                numbers[i].rectTransform.anchoredPosition = new Vector2(x, y - numberSize * 0.5f);
                captions[i].rectTransform.sizeDelta = new Vector2(span - 12f, 24f);
                captions[i].rectTransform.anchoredPosition = new Vector2(x, y - numberSize - 18f);
            }

            y -= numberSize + 48f;
            float rowGap = height < 320f ? 26f : 32f;
            float trackWidth = Mathf.Min(420f, width * 0.42f);
            for (int i = 0; i < 3; i++)
            {
                float row = y - i * rowGap;
                barLabels[i].rectTransform.anchoredPosition = new Vector2(-width * 0.28f, row);
                if (tracks[i] == null)
                    continue;
                tracks[i].sizeDelta = new Vector2(trackWidth, 16f);
                tracks[i].anchoredPosition = new Vector2(width * 0.06f, row);
            }

            float chip = height < 320f ? 64f : 84f;
            float chipsY = y - rowGap * 2f - 18f - chip * 0.5f;
            float lowest = -top + 24f + chip * 0.5f;
            if (chipsY < lowest)
                chipsY = lowest;
            for (int i = 0; i < 3; i++)
            {
                if (chipRects[i] == null)
                    continue;
                chipRects[i].sizeDelta = new Vector2(chip, chip);
                chipRects[i].anchoredPosition = new Vector2((i - 1) * (chip + 28f), chipsY);
            }

            if (empty != null)
            {
                empty.rectTransform.sizeDelta = new Vector2(width - 48f, 40f);
                empty.rectTransform.anchoredPosition = new Vector2(0f, chipsY);
            }
        }

        void Choose(int index)
        {
            if (index < 0 || index >= recent.Length || recent[index] == null)
                return;
            openWork?.Invoke(recent[index]);
        }

        static TMP_Text Word(Transform parent, string value, TMP_FontAsset font, float size, Color color, FontStyles style, Vector2 at, Vector2 box)
        {
            var item = new GameObject(value, typeof(RectTransform));
            item.transform.SetParent(parent, false);
            var text = item.AddComponent<TextMeshProUGUI>();
            if (font != null)
                text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            var rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = box;
            rect.anchoredPosition = at;
            return text;
        }

        static Sprite pill;

        static Sprite Pill(int radius)
        {
            if (pill != null)
                return pill;

            const int edge = 48;
            var texture = new Texture2D(edge, edge, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[edge * edge];
            for (int y = 0; y < edge; y++)
            {
                for (int x = 0; x < edge; x++)
                {
                    float dx = x < radius ? radius - x : x >= edge - radius ? x - (edge - radius - 1) : 0f;
                    float dy = y < radius ? radius - y : y >= edge - radius ? y - (edge - radius - 1) : 0f;
                    float outside = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                    byte alpha = (byte)(Mathf.Clamp01(1f - outside) * 255f);
                    pixels[y * edge + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            pill = Sprite.Create(texture, new Rect(0f, 0f, edge, edge), new Vector2(0.5f, 0.5f), edge, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            return pill;
        }
    }
}
