using UnityEngine;

namespace Cinnarios.Painting.Presentation
{
    /// <summary>
    /// Scales an image down so its long side is at most a limit (2,048 px for the camera),
    /// keeping its proportions. Museum images can be several thousand pixels wide, which
    /// wastes phone memory without looking any sharper at viewing distance.
    /// </summary>
    public static class TextureLimiter
    {
        /// <returns>The same texture if it is already small enough, otherwise a new, smaller copy.</returns>
        public static Texture2D LimitLongSide(Texture2D source, int maxLongSide)
        {
            int longSide = Mathf.Max(source.width, source.height);
            if (longSide <= maxLongSide)
            {
                return source;
            }

            float scale = maxLongSide / (float)longSide;
            int width = Mathf.Max(1, Mathf.RoundToInt(source.width * scale));
            int height = Mathf.Max(1, Mathf.RoundToInt(source.height * scale));

            RenderTexture target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;
            Graphics.Blit(source, target);
            RenderTexture.active = target;

            var result = new Texture2D(width, height, TextureFormat.RGBA32, true)
            {
                name = source.name,
                wrapMode = TextureWrapMode.Clamp
            };
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            result.Apply(true, false);

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            return result;
        }
    }
}
