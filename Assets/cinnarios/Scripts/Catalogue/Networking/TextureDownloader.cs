using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Cinnarios.Catalogue.Networking
{
    /// <summary>
    /// Downloads images and remembers them, so showing the same artwork again (for example
    /// when swapping back and forth on the wall) is instant. Two requests for the same image
    /// at the same time share one download.
    /// </summary>
    public sealed class TextureDownloader
    {
        private readonly Dictionary<string, Task<Texture2D>> _cache = new Dictionary<string, Task<Texture2D>>();

        /// <returns>The image, or null if it could not be downloaded (the reason is logged).</returns>
        public Task<Texture2D> GetAsync(string url, CancellationToken cancellationToken = default)
        {
            if (!_cache.TryGetValue(url, out Task<Texture2D> download))
            {
                download = DownloadAsync(url, cancellationToken);
                _cache[url] = download;
            }

            return download;
        }

        private async Task<Texture2D> DownloadAsync(string url, CancellationToken cancellationToken)
        {
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
            {
                await WebRequestAsync.SendAsync(request, cancellationToken);

                if (request.result != UnityWebRequest.Result.Success)
                {
                    _cache.Remove(url); // allow a later retry
                    Debug.LogWarning($"Image could not be downloaded from {url}: {request.error}");
                    return null;
                }

                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                texture.wrapMode = TextureWrapMode.Clamp; // stops the edges bleeding into each other
                return texture;
            }
        }
    }
}
