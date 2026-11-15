using TMPro;
using UnityEngine;

namespace Capstone.App.Data
{
    /// <summary>
    /// Pink-and-white gallery colours, kept on an asset so view scripts never store a colour of their own.
    /// The artwork sprite stays untinted. These colours are for the canvas, the card, the type, and the actions.
    /// </summary>
    [CreateAssetMenu(fileName = "UITheme", menuName = "Capstone/UI Theme")]
    public sealed class UIThemeData : ScriptableObject
    {
        [SerializeField] Color canvasBackground = new Color32(0xFB, 0xF9, 0xFA, 0xFF);
        [SerializeField] Color cardSurface = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        [SerializeField] Color cardBorder = new Color32(0xF2, 0xDC, 0xE3, 0xFF);
        [SerializeField] Color primaryAccent = new Color32(0xE9, 0xA8, 0xBC, 0xFF);
        [SerializeField] Color secondaryAccent = new Color32(0xF9, 0xEB, 0xF0, 0xFF);
        [SerializeField] Color primaryText = new Color32(0x2A, 0x22, 0x25, 0xFF);
        [SerializeField] Color secondaryText = new Color32(0x7D, 0x6E, 0x73, 0xFF);
        [SerializeField] Color divider = new Color32(0xE5, 0xDE, 0xE1, 0xFF);
        [SerializeField] float cardCornerRadius = 16f;
        [SerializeField] float pillCornerRadius = 24f;
        [SerializeField] TMP_FontAsset font;
        [SerializeField] TMP_FontAsset nightyFont;

        public Color CanvasBackground => canvasBackground;
        public Color CardSurface => cardSurface;
        public Color CardBorder => cardBorder;
        public Color PrimaryAccent => primaryAccent;
        public Color SecondaryAccent => secondaryAccent;
        public Color PrimaryText => primaryText;
        public Color SecondaryText => secondaryText;
        public Color Divider => divider;

        /// <summary>Art-card corner radius, in pixels of the source sprite.</summary>
        public float CardCornerRadius => cardCornerRadius;

        /// <summary>Filter and button corner radius, in pixels of the source sprite.</summary>
        public float PillCornerRadius => pillCornerRadius;

        /// <summary>Optional sans-serif. When empty, a view keeps the font already on its text.</summary>
        public TMP_FontAsset Font => font;

        /// <summary>Nighty, for the entrance line. When empty, the entrance looks the font up itself.</summary>
        public TMP_FontAsset NightyFont => nightyFont;
    }
}
