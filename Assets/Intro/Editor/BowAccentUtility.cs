using System.IO;
using UnityEditor;
using UnityEngine;

// Turns a white-backed bow into a sprite that sits on the warm entrance canvas.
public static class BowAccentUtility
{
    const string Output = "Assets/Resources/TopBowAccent.png";

    [MenuItem("Tools/Capstone/Prepare Bow Accent")]
    public static void Prepare()
    {
        Texture2D source = FindSource();
        if (source == null)
        {
            Debug.LogWarning("No bow texture was found under Resources or Themes.");
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(source);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        bool wasReadable = importer != null && importer.isReadable;
        if (importer != null && !wasReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
            source = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        Texture2D clear = StripWhite(source);
        File.WriteAllBytes(Output, clear.EncodeToPNG());
        Object.DestroyImmediate(clear);

        if (importer != null && !wasReadable)
        {
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        AssetDatabase.ImportAsset(Output);
        TextureImporter written = AssetImporter.GetAtPath(Output) as TextureImporter;
        if (written != null)
        {
            written.textureType = TextureImporterType.Sprite;
            written.spriteImportMode = SpriteImportMode.Single;
            written.alphaIsTransparency = true;
            written.mipmapEnabled = false;
            written.SaveAndReimport();
        }
    }

    static Texture2D FindSource()
    {
        string[] folders = { "Assets/Resources", "Assets/_Project/Themes" };
        string[] guids = AssetDatabase.FindAssets("bow t:Texture2D", folders);
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (path.EndsWith("TopBowAccent.png"))
                continue;
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null)
                return texture;
        }

        return null;
    }

    public static Texture2D StripWhite(Texture2D source)
    {
        Color32[] pixels = source.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 pixel = pixels[i];
            byte low = System.Math.Min(pixel.r, System.Math.Min(pixel.g, pixel.b));
            byte high = System.Math.Max(pixel.r, System.Math.Max(pixel.g, pixel.b));
            if (high - low >= 18 || low < 236)
                continue;

            int fade = Mathf.Clamp((250 - low) * 18, 0, 255);
            pixel.a = (byte)fade;
            pixels[i] = pixel;
        }

        Texture2D copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        copy.SetPixels32(pixels);
        copy.Apply();
        return copy;
    }
}
