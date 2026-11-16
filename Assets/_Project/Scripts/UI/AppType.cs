using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

namespace Capstone.App.UI
{
    /// <summary>
    /// One typeface and one type scale for every screen.
    /// Inter is built into a dynamic TextMeshPro font at startup from the TTFs in Resources/Fonts,
    /// with SemiBold wired in as the bold weight. The TextMeshPro default stays as the fallback,
    /// so a glyph Inter lacks still renders instead of showing a box.
    /// </summary>
    public static class AppType
    {
        // Type scale (canvas px). Use these instead of loose numbers in new code.
        public const float Display = 56f;
        public const float Title = 40f;
        public const float Heading = 30f;
        public const float Body = 26f;
        public const float Caption = 22f;
        public const float Micro = 18f;

        static TMP_FontAsset body;
        static TMP_FontAsset display;
        static bool tried;

        /// <summary>Inter Regular with SemiBold as bold. Falls back to the TextMeshPro default.</summary>
        public static TMP_FontAsset BodyFont { get { Ensure(); return body != null ? body : TMP_Settings.defaultFontAsset; } }

        /// <summary>Inter SemiBold for titles and labels.</summary>
        public static TMP_FontAsset DisplayFont { get { Ensure(); return display != null ? display : BodyFont; } }

        /// <summary>Archivo Black for the wordmark and code letters. Falls back to Inter SemiBold.</summary>
        public static TMP_FontAsset HeavyFont { get { EnsureEditorial(); return heavy != null ? heavy : DisplayFont; } }

        /// <summary>Archivo Medium for editorial labels and lines. Falls back to Inter.</summary>
        public static TMP_FontAsset LabelFont { get { EnsureEditorial(); return label != null ? label : BodyFont; } }

        static TMP_FontAsset heavy;
        static TMP_FontAsset label;
        static bool triedEditorial;

        static void EnsureEditorial()
        {
            if (triedEditorial)
                return;
            triedEditorial = true;
            try
            {
                TMP_FontAsset fallback = BodyFont;
                heavy = Build("Fonts/Archivo-Black", fallback);
                label = Build("Fonts/Archivo-Medium", fallback);
            }
            catch (Exception error)
            {
                Debug.LogWarning("AppType: Archivo could not be loaded, using Inter. " + error.Message);
                heavy = null;
                label = null;
            }
        }

        /// <summary>Kept for call sites written before the type scale.</summary>
        public static TMP_FontAsset Default => BodyFont;

        static void Ensure()
        {
            if (tried)
                return;
            tried = true;
            try
            {
                TMP_FontAsset fallback = TMP_Settings.defaultFontAsset;
                TMP_FontAsset semi = Build("Fonts/Inter-SemiBold", fallback);
                TMP_FontAsset regular = Build("Fonts/Inter-Regular", fallback);
                if (regular != null && semi != null)
                {
                    TMP_FontWeightPair[] table = regular.fontWeightTable;
                    if (table != null && table.Length > 7)
                    {
                        table[6].regularTypeface = semi;
                        table[7].regularTypeface = semi;
                    }
                }

                body = regular;
                display = semi != null ? semi : regular;
            }
            catch (Exception error)
            {
                Debug.LogWarning("AppType: Inter could not be loaded, keeping the default font. " + error.Message);
                body = null;
                display = null;
            }
        }

        static TMP_FontAsset Build(string resource, TMP_FontAsset fallback)
        {
            Font source = Resources.Load<Font>(resource);
            if (source == null)
                return null;
            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            if (asset == null)
                return null;
            asset.name = source.name + " (runtime)";
            if (fallback != null)
                asset.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };
            return asset;
        }

        /// <summary>Puts every scene-authored text that still uses the TextMeshPro default onto Inter.</summary>
        public static void Restyle(GameObject root)
        {
            if (root == null)
                return;
            TMP_FontAsset fallback = TMP_Settings.defaultFontAsset;
            TMP_FontAsset font = BodyFont;
            if (font == null || font == fallback)
                return;
            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && (texts[i].font == null || texts[i].font == fallback))
                    texts[i].font = font;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            Ensure();
            RestyleScene(SceneManager.GetActiveScene());
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => RestyleScene(scene);

        static void RestyleScene(Scene scene)
        {
            if (!scene.IsValid())
                return;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
                Restyle(roots[i]);
        }
    }
}
