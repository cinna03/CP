using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Capstone.App.Data
{
    public enum Orientation
    {
        Portrait,
        Landscape,
        Square
    }

    public enum SizeTier
    {
        Small,
        Medium,
        Large
    }

    /// <summary>
    /// One artwork stored in the app. Width is x and height is y, both in centimetres.
    /// Index 0 is the default size. A chosen size is still this record, divided by 100 for the wall.
    /// The picture, the facts, and the story note travel with the asset, so the visit does not fetch them.
    /// </summary>
    [CreateAssetMenu(fileName = "Artwork", menuName = "Capstone/Artwork")]
    public sealed class ArtworkData : ScriptableObject
    {
        [SerializeField] string identifier = "";
        [SerializeField] string title = "";
        [SerializeField] string artist = "";
        [SerializeField] int year;
        [SerializeField] string medium = "";
        [SerializeField] List<Vector2> availableSizes = new List<Vector2>();
        [SerializeField] string style = "";
        [SerializeField] string mood = "";
        [SerializeField] string colour = "";
        [SerializeField] string priceBand = "";
        [SerializeField] Sprite artworkSprite;
        [TextArea(2, 5)]
        [SerializeField] string storyNote = "";

        [Header("Storytelling")]
        [Tooltip("The curator's interpretation of the work. Labelled as a curator's note wherever it is shown.")]
        [TextArea(2, 6)]
        [SerializeField] string curatorNote = "";
        [Tooltip("The artist's own background, in their words. Leave empty until the artist provides it.")]
        [TextArea(2, 6)]
        [SerializeField] string artistNote = "";
        [Tooltip("What inspired the work, from the artist. Leave empty until the artist provides it.")]
        [TextArea(2, 6)]
        [SerializeField] string inspirationNote = "";

        public string Identifier => identifier == null ? "" : identifier.Trim();
        public string Title => title ?? "";
        public string Artist => artist ?? "";
        public int Year => year;
        public string Medium => medium ?? "";
        public IReadOnlyList<Vector2> AvailableSizes => availableSizes;
        public string Style => style ?? "";
        public string Mood => mood ?? "";
        public string Colour => colour ?? "";
        public string PriceBand => priceBand ?? "";
        public Sprite ArtworkSprite => artworkSprite;
        public string StoryNote => storyNote ?? "";
        public string CuratorNote => curatorNote ?? "";
        public string ArtistNote => artistNote ?? "";
        public string InspirationNote => inspirationNote ?? "";

        /// <summary>Portrait, landscape, or square from the default size.</summary>
        public Orientation Orientation => OrientationAt(0);

        /// <summary>
        /// Small is a longest side under 50 cm, Medium is 50 to 90 cm, Large is over 90 cm.
        /// The tier follows the default size, so filtering the list does not change while a buyer tries another size.
        /// </summary>
        public SizeTier SizeTier => SizeTierAt(0);

        public Orientation OrientationAt(int sizeIndex)
        {
            if (!TryGetSizeCentimetres(sizeIndex, out Vector2 size))
                return Orientation.Square;

            if (size.x > size.y)
                return Orientation.Landscape;
            if (size.y > size.x)
                return Orientation.Portrait;
            return Orientation.Square;
        }

        public SizeTier SizeTierAt(int sizeIndex)
        {
            if (!TryGetSizeCentimetres(sizeIndex, out Vector2 size))
                return SizeTier.Small;

            float longest = Mathf.Max(size.x, size.y);
            if (longest < 50f)
                return SizeTier.Small;
            if (longest <= 90f)
                return SizeTier.Medium;
            return SizeTier.Large;
        }

        /// <summary>True-to-scale metres for the wall. A missing size returns zero so the quad is left unchanged.</summary>
        public Vector2 GetDimensionsInMeters(int sizeIndex)
        {
            if (!TryGetSizeCentimetres(sizeIndex, out Vector2 centimetres))
                return Vector2.zero;

            return centimetres / 100f;
        }

        public bool TryGetSizeCentimetres(int sizeIndex, out Vector2 centimetres)
        {
            centimetres = Vector2.zero;
            if (availableSizes == null || availableSizes.Count == 0)
                return false;

            if (sizeIndex < 0 || sizeIndex >= availableSizes.Count)
                sizeIndex = 0;

            Vector2 size = availableSizes[sizeIndex];
            if (size.x <= 0f || size.y <= 0f)
                return false;

            centimetres = size;
            return true;
        }

        public string FormatSizeLabel(int sizeIndex)
        {
            if (!TryGetSizeCentimetres(sizeIndex, out Vector2 size))
                return "";

            return Centimetres(size.x) + " x " + Centimetres(size.y) + " cm";
        }

        public static string Centimetres(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        void OnValidate()
        {
            identifier = identifier == null ? "" : identifier.Trim();
            title = title ?? "";
            artist = artist ?? "";
            medium = medium ?? "";
            style = style ?? "";
            mood = mood ?? "";
            colour = colour ?? "";
            priceBand = priceBand ?? "";
            storyNote = storyNote ?? "";

            if (string.IsNullOrEmpty(identifier))
                Debug.LogWarning("A work needs an identifier.", this);

            if (availableSizes == null)
                availableSizes = new List<Vector2>();

            for (int i = availableSizes.Count - 1; i >= 0; i--)
            {
                Vector2 size = availableSizes[i];
                if (size.x <= 0f || size.y <= 0f)
                    availableSizes.RemoveAt(i);
            }

            if (availableSizes.Count == 0)
                Debug.LogWarning("A work needs a width and a height above zero.", this);
        }
    }
}
