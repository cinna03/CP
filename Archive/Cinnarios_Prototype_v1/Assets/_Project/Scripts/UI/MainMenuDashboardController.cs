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
    /// The home screen, laid out like a magazine page from top to bottom: a short welcome that says what
    /// the app is for, one large "continue with" card for the last work the buyer looked at, the taste
    /// the visit has learned so far, the other recent works, and a quiet line of numbers. Every section
    /// fills the space it is given, so the page never ends in an empty white block. Numbers come from
    /// StudioDigest.
    /// </summary>
    public sealed class MainMenuDashboardController : MonoBehaviour
    {
        static readonly Color Ink = new Color32(0x2B, 0x21, 0x24, 0xFF);
        static readonly Color Stone = new Color32(0x6B, 0x53, 0x59, 0xFF);
        static readonly Color Rose = new Color32(0xB8, 0x5C, 0x74, 0xFF);
        static readonly Color Petal = new Color32(0xF7, 0xEC, 0xEF, 0xFF);
        static readonly Color Line = new Color32(0xEF, 0xE3, 0xE6, 0xFF);

        const float Pad = 8f;

        RectTransform page;
        TMP_Text kicker;
        TMP_Text headline;
        TMP_Text subline;
        RectTransform hero;
        Image heroPicture;
        TMP_Text heroLabel;
        TMP_Text heroTitle;
        TMP_Text heroDetail;
        TMP_Text heroAction;
        TMP_Text tasteLabel;
        readonly List<GameObject> tasteChips = new List<GameObject>();
        RectTransform tasteRow;
        TMP_Text recentLabel;
        readonly Image[] thumbs = new Image[3];
        readonly TMP_Text[] thumbTitles = new TMP_Text[3];
        readonly ArtworkData[] recent = new ArtworkData[3];
        TMP_Text stats;
        ArtworkData heroWork;
        Action<ArtworkData> openWork;

        public void Build(TMP_FontAsset font)
        {
            if (page != null)
                return;

            Image backdrop = GetComponent<Image>();
            if (backdrop == null)
                backdrop = gameObject.AddComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0f);
            backdrop.raycastTarget = false;
            page = (RectTransform)transform;

            kicker = Text(page, "YOUR STUDIO  ·  KIGALI", AppType.LabelFont, 20f, Stone, TextAlignmentOptions.Left);
            headline = Text(page, "", AppType.DisplayFont, 60f, Ink, TextAlignmentOptions.TopLeft);
            headline.lineSpacing = AppType.Leading(60f);
            subline = Text(page, "Browse the collection, then see any work at its real size on your own wall.", AppType.BodyFont, 26f, Stone, TextAlignmentOptions.TopLeft);

            hero = Block("Continue", page);
            var heroImage = hero.gameObject.AddComponent<Image>();
            Paint(heroImage, Color.white, 0.5f);
            Button heroButton = hero.gameObject.AddComponent<Button>();
            heroButton.targetGraphic = heroImage;
            heroButton.onClick.AddListener(() => { if (heroWork != null) openWork?.Invoke(heroWork); });
            heroPicture = Block("Picture", hero).gameObject.AddComponent<Image>();
            heroPicture.preserveAspect = true;
            heroPicture.raycastTarget = false;
            heroLabel = Text(hero, "CONTINUE WITH", AppType.LabelFont, 18f, Stone, TextAlignmentOptions.Left);
            heroTitle = Text(hero, "", AppType.DisplayFont, 40f, Ink, TextAlignmentOptions.TopLeft);
            heroDetail = Text(hero, "", AppType.BodyFont, 24f, Stone, TextAlignmentOptions.TopLeft);
            heroAction = Text(hero, "Open the work  →", AppType.BodyFont, 26f, Rose, TextAlignmentOptions.Left);

            tasteLabel = Text(page, "YOUR TASTE SO FAR", AppType.LabelFont, 18f, Stone, TextAlignmentOptions.Left);
            tasteRow = Block("Taste", page);
            var flow = tasteRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            flow.spacing = 12f;
            flow.childAlignment = TextAnchor.MiddleLeft;
            flow.childControlWidth = false;
            flow.childControlHeight = false;
            flow.childForceExpandWidth = false;
            flow.childForceExpandHeight = false;

            recentLabel = Text(page, "RECENTLY VIEWED", AppType.LabelFont, 18f, Stone, TextAlignmentOptions.Left);
            for (int i = 0; i < 3; i++)
            {
                RectTransform tile = Block("Recent " + (i + 1), page);
                Image frame = tile.gameObject.AddComponent<Image>();
                Paint(frame, Petal, 0.75f);
                Button tap = tile.gameObject.AddComponent<Button>();
                tap.targetGraphic = frame;
                int index = i;
                tap.onClick.AddListener(() => Choose(index));
                thumbs[i] = Block("Picture", tile).gameObject.AddComponent<Image>();
                thumbs[i].preserveAspect = true;
                thumbs[i].raycastTarget = false;
                thumbTitles[i] = Text(tile, "", AppType.BodyFont, 20f, Ink, TextAlignmentOptions.Left);
            }

            stats = Text(page, "", AppType.BodyFont, 22f, Stone, TextAlignmentOptions.Left);
            Divider("Rule", page);
        }

        public void Show(ArtworkSessionState session, IReadOnlyList<WallSnapshotRecord> photos, TMP_FontAsset font, Action<ArtworkData> onOpen)
        {
            Build(font);
            openWork = onOpen;
            StudioDigest digest = StudioDigest.Read(session, photos);
            bool returning = digest.Recent.Count > 0;

            headline.text = returning ? "Welcome back.\nLet's find its wall." : "Find the piece that\nbelongs on your wall.";
            heroWork = returning ? digest.Recent[0] : null;
            hero.gameObject.SetActive(true);
            if (heroWork != null)
            {
                heroPicture.sprite = heroWork.ArtworkSprite;
                heroPicture.color = heroWork.ArtworkSprite != null ? Color.white : Petal;
                heroLabel.text = "CONTINUE WITH";
                heroTitle.text = heroWork.Title;
                string artist = string.IsNullOrWhiteSpace(heroWork.Artist) ? "" : heroWork.Artist + "\n";
                heroDetail.text = artist + heroWork.FormatSizeLabel(0);
                heroAction.text = "Open the work  →";
            }
            else
            {
                heroPicture.sprite = null;
                heroPicture.color = Petal;
                heroLabel.text = "START HERE";
                heroTitle.text = "Open the catalogue";
                heroDetail.text = "Pick a work you like, then tap Try on My Wall to see it at its real size.";
                heroAction.text = "";
            }

            foreach (GameObject chip in tasteChips)
                Destroy(chip);
            tasteChips.Clear();
            for (int i = 0; i < digest.Bars.Count && i < 4; i++)
                tasteChips.Add(Chip(digest.Bars[i].Label));
            bool hasTaste = tasteChips.Count > 0;
            if (!hasTaste)
                tasteChips.Add(Chip("Keep or save a few works and your taste appears here"));

            int shown = 0;
            for (int i = 0; i < 3; i++)
            {
                int source = i + 1;
                ArtworkData work = source < digest.Recent.Count ? digest.Recent[source] : null;
                recent[i] = work;
                thumbs[i].transform.parent.gameObject.SetActive(work != null);
                if (work == null)
                    continue;
                shown++;
                thumbs[i].sprite = work.ArtworkSprite;
                thumbs[i].color = work.ArtworkSprite != null ? Color.white : Petal;
                thumbTitles[i].text = work.Title;
            }

            recentLabel.gameObject.SetActive(shown > 0);
            stats.text = digest.Explored + " explored   ·   " + digest.Hearts + " saved   ·   " + digest.Photos + (digest.Photos == 1 ? " wall photo" : " wall photos");
            Arrange(shown);
            UIMotion.AppearInList(hero.gameObject);
        }

        /// <summary>Stacks the sections from the top. The hero card takes whatever height is left.</summary>
        void Arrange(int recentCount)
        {
            float width = page.rect.width > 100f ? page.rect.width : 1000f;
            float height = page.rect.height > 200f ? page.rect.height : 1500f;
            float inner = width - Pad * 2f;

            float recentH = recentCount > 0 ? 260f : 0f;
            float fixedH = 40f + 170f + 90f + 40f + 70f + (recentCount > 0 ? 50f : 0f) + recentH + 60f + 80f;
            float heroH = Mathf.Clamp(height - fixedH, 300f, 760f);

            float y = -24f;
            Place(kicker.rectTransform, Pad, y, inner, 30f); y -= 44f;
            Place(headline.rectTransform, Pad, y, inner, 160f); y -= 170f;
            Place(subline.rectTransform, Pad, y, inner * 0.9f, 80f); y -= 100f;

            Place(hero, Pad, y, inner, heroH);
            float pictureW = Mathf.Min(inner * 0.46f, heroH - 48f);
            Place((RectTransform)heroPicture.transform, 24f, -24f, pictureW, heroH - 48f);
            float textX = pictureW + 56f;
            float textW = inner - textX - 32f;
            Place(heroLabel.rectTransform, textX, -40f, textW, 26f);
            Place(heroTitle.rectTransform, textX, -76f, textW, 120f);
            Place(heroDetail.rectTransform, textX, -200f, textW, 120f);
            heroAction.rectTransform.anchorMin = heroAction.rectTransform.anchorMax = new Vector2(0f, 0f);
            heroAction.rectTransform.pivot = new Vector2(0f, 0f);
            heroAction.rectTransform.anchoredPosition = new Vector2(textX, 32f);
            heroAction.rectTransform.sizeDelta = new Vector2(textW, 40f);
            y -= heroH + 40f;

            Place(tasteLabel.rectTransform, Pad, y, inner, 26f); y -= 40f;
            Place(tasteRow, Pad, y, inner, 56f); y -= 80f;

            if (recentCount > 0)
            {
                Place(recentLabel.rectTransform, Pad, y, inner, 26f); y -= 40f;
                float tileW = (inner - 32f) / 3f;
                for (int i = 0; i < 3; i++)
                {
                    var tile = (RectTransform)thumbs[i].transform.parent;
                    Place(tile, Pad + i * (tileW + 16f), y, tileW, recentH - 20f);
                    Place((RectTransform)thumbs[i].transform, 12f, -12f, tileW - 24f, recentH - 90f);
                    Place(thumbTitles[i].rectTransform, 16f, -(recentH - 72f), tileW - 32f, 44f);
                }

                y -= recentH;
            }

            Transform rule = page.Find("Rule");
            if (rule != null)
                Place((RectTransform)rule, Pad, y - 8f, inner, 2f);
            Place(stats.rectTransform, Pad, y - 24f, inner, 40f);
        }

        GameObject Chip(string label)
        {
            var chip = new GameObject(label, typeof(RectTransform), typeof(Image));
            chip.transform.SetParent(tasteRow, false);
            Paint(chip.GetComponent<Image>(), Petal, 0.5f);
            TMP_Text text = Text(chip.transform, label.ToLowerInvariant(), AppType.BodyFont, 22f, Ink, TextAlignmentOptions.Center);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            float w = Mathf.Max(110f, text.GetPreferredValues(text.text).x + 48f);
            ((RectTransform)chip.transform).sizeDelta = new Vector2(w, 52f);
            return chip;
        }

        void Choose(int index)
        {
            if (index >= 0 && index < recent.Length && recent[index] != null)
                openWork?.Invoke(recent[index]);
        }

        void Divider(string name, Transform parent)
        {
            RectTransform rule = Block(name, parent);
            rule.gameObject.AddComponent<Image>().color = Line;
        }

        static RectTransform Block(string name, Transform parent)
        {
            var host = new GameObject(name, typeof(RectTransform));
            host.transform.SetParent(parent, false);
            return (RectTransform)host.transform;
        }

        static void Place(RectTransform target, float x, float y, float w, float h)
        {
            target.anchorMin = target.anchorMax = new Vector2(0f, 1f);
            target.pivot = new Vector2(0f, 1f);
            target.anchoredPosition = new Vector2(x, y);
            target.sizeDelta = new Vector2(w, h);
        }

        static void Paint(Image image, Color color, float roundness)
        {
            image.sprite = UIShapes.Rounded();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = roundness;
            image.color = color;
        }

        static TMP_Text Text(Transform parent, string value, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align)
        {
            var host = new GameObject("Text", typeof(RectTransform));
            host.transform.SetParent(parent, false);
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
    }
}
