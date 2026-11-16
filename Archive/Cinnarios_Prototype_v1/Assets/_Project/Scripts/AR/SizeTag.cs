using Capstone.App.UI;
using TMPro;
using UnityEngine;

namespace Capstone.App.AR
{
    /// <summary>
    /// A small label on the wall just under a work, e.g. "53 × 60 cm", so the buyer can see that what they
    /// are looking at is the real size. It sits beside the painting (not inside it, because the painting
    /// quad is stretched to the work's proportions) and follows it every frame. Copies made when a work is
    /// locked carry the component and build their own label.
    /// </summary>
    public sealed class SizeTag : MonoBehaviour
    {
        const float Gap = 0.035f;         // metres between the work and the tag
        const float TextHeight = 0.03f;   // metres
        static readonly Color Ink = new Color32(0x2B, 0x21, 0x24, 0xFF);

        [SerializeField] string label = "";
        Transform tag;
        TextMeshPro text;
        SpriteRenderer pill;

        /// <summary>Adds or updates the tag on a work.</summary>
        public static SizeTag Show(Transform work, string sizeLabel)
        {
            if (work == null)
                return null;
            SizeTag tagged = work.GetComponent<SizeTag>();
            if (tagged == null)
                tagged = work.gameObject.AddComponent<SizeTag>();
            tagged.label = sizeLabel ?? "";
            tagged.Refresh();
            return tagged;
        }

        void OnEnable()
        {
            Refresh();
        }

        void OnDisable()
        {
            if (tag != null)
                tag.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (tag != null)
                Destroy(tag.gameObject);
        }

        void Refresh()
        {
            if (string.IsNullOrEmpty(label))
                return;
            if (tag == null)
                Build();
            tag.gameObject.SetActive(isActiveAndEnabled);
            text.text = label;
            text.ForceMeshUpdate();
            Vector2 size = text.GetRenderedValues(false);
            pill.size = new Vector2(Mathf.Max(0.05f, size.x + 0.03f), TextHeight + 0.018f);
            LateUpdate();
        }

        void Build()
        {
            tag = new GameObject("Size tag").transform;
            var background = new GameObject("Pill");
            background.transform.SetParent(tag, false);
            pill = background.AddComponent<SpriteRenderer>();
            pill.sprite = TinyRounded();
            pill.drawMode = SpriteDrawMode.Sliced;
            pill.color = new Color(1f, 1f, 1f, 0.88f);
            pill.sortingOrder = 10;

            var words = new GameObject("Text");
            words.transform.SetParent(tag, false);
            words.transform.localPosition = new Vector3(0f, 0f, -0.001f);
            text = words.AddComponent<TextMeshPro>();
            if (AppType.BodyFont != null)
                text.font = AppType.BodyFont;
            text.fontSize = 0.28f;
            text.color = Ink;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.rectTransform.sizeDelta = new Vector2(0.6f, TextHeight * 1.6f);
            text.sortingOrder = 11;
        }

        static Sprite tiny;

        /// <summary>The shared rounded shape at a pixel density that gives 1.2 cm corners in world space.</summary>
        static Sprite TinyRounded()
        {
            if (tiny != null)
                return tiny;
            Sprite source = UIShapes.Rounded();
            tiny = Sprite.Create(source.texture, source.rect, new Vector2(0.5f, 0.5f), 2000f, 0, SpriteMeshType.FullRect, source.border);
            return tiny;
        }

        void LateUpdate()
        {
            if (tag == null)
                return;
            // The painting's forward points out of the wall, towards the buyer; text reads facing back along it.
            Vector3 outward = transform.forward;
            float halfHeight = transform.lossyScale.y * 0.5f;
            tag.position = transform.position - transform.up * (halfHeight + Gap + TextHeight * 0.5f) + outward * 0.004f;
            tag.rotation = Quaternion.LookRotation(-outward, transform.up);
        }
    }
}
