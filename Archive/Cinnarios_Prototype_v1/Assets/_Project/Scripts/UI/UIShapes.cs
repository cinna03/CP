using UnityEngine;

namespace Capstone.App.UI
{
    /// <summary>Shared procedural shapes, made once and reused, so screens do not each build their own.</summary>
    public static class UIShapes
    {
        static Sprite rounded;

        /// <summary>A white rounded rectangle for sliced Images. Tint it with Image.color.</summary>
        public static Sprite Rounded()
        {
            if (rounded != null)
                return rounded;

            const int size = 64;
            const float radius = 24f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0f, Mathf.Max(radius - x - 0.5f, x + 0.5f - (size - radius)));
                    float dy = Mathf.Max(0f, Mathf.Max(radius - y - 0.5f, y + 0.5f - (size - radius)));
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    byte alpha = (byte)(Mathf.Clamp01(radius - distance + 0.5f) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            rounded = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            return rounded;
        }
    }
}
