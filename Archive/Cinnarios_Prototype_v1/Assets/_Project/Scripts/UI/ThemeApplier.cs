using Capstone.App.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Capstone.App.UI
{
    public enum ThemeRole
    {
        CanvasBackground,
        CardSurface,
        CardBorder,
        PrimaryAccent,
        SecondaryAccent,
        PrimaryText,
        SecondaryText,
        Divider
    }

    /// <summary>
    /// Paints one graphic from the theme asset. View scripts bind words and pictures, and leave colour to this component.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ThemeApplier : MonoBehaviour
    {
        [SerializeField] UIThemeData theme;
        [SerializeField] ThemeRole role;

        public UIThemeData Theme => theme;
        public ThemeRole Role => role;

        void OnEnable()
        {
            Apply();
        }

        void OnValidate()
        {
            Apply();
        }

        public void Apply()
        {
            if (theme == null)
                return;

            Color color = ColorFor(theme, role);
            Image image = GetComponent<Image>();
            if (image != null)
                image.color = color;

            TMP_Text text = GetComponent<TMP_Text>();
            if (text != null)
            {
                text.color = color;
                if (theme.Font != null)
                    text.font = theme.Font;
            }
        }

        public static Color ColorFor(UIThemeData theme, ThemeRole role)
        {
            switch (role)
            {
                case ThemeRole.CanvasBackground:
                    return theme.CanvasBackground;
                case ThemeRole.CardSurface:
                    return theme.CardSurface;
                case ThemeRole.CardBorder:
                    return theme.CardBorder;
                case ThemeRole.PrimaryAccent:
                    return theme.PrimaryAccent;
                case ThemeRole.SecondaryAccent:
                    return theme.SecondaryAccent;
                case ThemeRole.PrimaryText:
                    return theme.PrimaryText;
                case ThemeRole.SecondaryText:
                    return theme.SecondaryText;
                default:
                    return theme.Divider;
            }
        }
    }
}
