using System;
using System.Collections.Generic;
using System.Text;
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
    ///
    /// The Met image host answers this app with a block page instead of the file. When the
    /// bytes are not a picture, the same file is taken from Wikimedia Commons, which holds
    /// the museum's open-access photographs under the Met file name.
    /// </summary>
    public sealed class TextureDownloader
    {
        private const string UserAgent = "Cinnarios/1.0 (https://cinnarios.onrender.com; educational AR viewer)";

        private readonly Dictionary<string, Task<Texture2D>> _cache = new Dictionary<string, Task<Texture2D>>();
        private readonly Dictionary<string, string> _commonsByFile = new Dictionary<string, string>();

        /// <summary>The print, at a size suitable for the wall.</summary>
        public Task<Texture2D> GetAsync(string url, CancellationToken cancellationToken = default)
        {
            return GetAsync(url, 1600, cancellationToken);
        }

        public Task<Texture2D> GetAsync(string url, int longestSide, CancellationToken cancellationToken = default)
        {
            return GetAsync(url, longestSide, null, cancellationToken);
        }

        /// <param name="longestSide">How wide a Commons stand-in may be, in pixels.</param>
        /// <param name="workTitle">Used only when the file name is not on Commons, to find the same painting.</param>
        /// <returns>The image, or null if it could not be downloaded (the reason is logged).</returns>
        public Task<Texture2D> GetAsync(string url, int longestSide, string workTitle, CancellationToken cancellationToken = default)
        {
            string key = url + "|" + longestSide;
            if (!_cache.TryGetValue(key, out Task<Texture2D> download))
            {
                download = DownloadAsync(url, longestSide, workTitle, cancellationToken);
                _cache[key] = download;
            }

            return download;
        }

        private async Task<Texture2D> DownloadAsync(string url, int longestSide, string workTitle, CancellationToken cancellationToken)
        {
            string key = url + "|" + longestSide;
            try
            {
                // The museum image host answers this app with a block page. Skip that
                // request and ask Wikimedia, which has the same photograph, in one call.
                bool blockedHost = url.IndexOf("images.metmuseum.org", StringComparison.OrdinalIgnoreCase) >= 0;
                byte[] bytes = blockedHost ? null : await GetBytesAsync(url, cancellationToken);
                if (!IsImage(bytes))
                {
                    string commons = await CommonsThumbAsync(url, longestSide, workTitle, cancellationToken);
                    bytes = string.IsNullOrEmpty(commons) ? null : await GetBytesAsync(commons, cancellationToken);
                }

                if (!IsImage(bytes))
                {
                    _cache.Remove(key);
                    Debug.LogWarning($"Image could not be downloaded from {url}");
                    return null;
                }

                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(bytes))
                {
                    _cache.Remove(key);
                    Debug.LogWarning($"Image from {url} could not be read");
                    return null;
                }

                texture.wrapMode = TextureWrapMode.Clamp;
                return texture;
            }
            catch
            {
                _cache.Remove(key);
                throw;
            }
        }

        private async Task<string> CommonsThumbAsync(string metUrl, int longestSide, string workTitle, CancellationToken cancellationToken)
        {
            string stem = FileStem(metUrl);
            string cacheKey = stem + "|" + (workTitle ?? string.Empty);
            if (_commonsByFile.TryGetValue(cacheKey, out string known))
            {
                return string.IsNullOrEmpty(known) ? null : known;
            }

            int width = Mathf.Clamp(longestSide, 128, 2048);
            string thumb = null;
            if (stem.Length >= 4)
            {
                thumb = await SearchThumbAsync("\"" + stem + "\"", Compact(stem), null, width, cancellationToken);
                string spaced = stem.Replace('_', ' ').Replace('-', ' ');
                if (thumb == null && !string.Equals(spaced, stem, StringComparison.Ordinal))
                {
                    thumb = await SearchThumbAsync(spaced, Compact(stem), null, width, cancellationToken);
                }
            }

            if (thumb == null && !string.IsNullOrWhiteSpace(workTitle))
            {
                thumb = await SearchThumbAsync(workTitle + " MET", null, workTitle, width, cancellationToken);
            }

            if (thumb == null && stem.Length >= 4)
            {
                string title = await SearchFileAsync("\"" + stem + "\"", Compact(stem), null, cancellationToken);
                thumb = await ThumbForTitleAsync(title, width, cancellationToken);
            }

            if (!string.IsNullOrEmpty(thumb))
            {
                _commonsByFile[cacheKey] = thumb;
            }

            return thumb;
        }

        // One Commons request returns the file name and a small image address together.
        private async Task<string> SearchThumbAsync(
            string query, string compactStem, string workTitle, int width, CancellationToken cancellationToken)
        {
            string searchUrl =
                "https://commons.wikimedia.org/w/api.php?action=query&generator=search&gsrnamespace=6&gsrlimit=5&prop=imageinfo&iiprop=url&format=json&iiurlwidth="
                + width
                + "&gsrsearch="
                + UnityWebRequest.EscapeURL(query);
            byte[] searchBytes = await GetBytesAsync(searchUrl, cancellationToken);
            if (searchBytes == null)
            {
                return null;
            }

            return PickThumb(Encoding.UTF8.GetString(searchBytes), compactStem, workTitle);
        }

        private static string PickThumb(string json, string compactStem, string workTitle)
        {
            string best = null;
            int bestScore = int.MinValue;
            int from = 0;
            const string marker = "\"thumburl\":\"";
            while (from < json.Length)
            {
                int at = json.IndexOf(marker, from, StringComparison.Ordinal);
                if (at < 0)
                {
                    break;
                }

                string thumb = JsonUnescape(ReadJsonString(json, at + marker.Length));
                from = at + marker.Length;
                int window = Math.Max(0, at - 600);
                string before = json.Substring(window, at - window);
                const string titleMarker = "\"title\":\"";
                int titleAt = before.LastIndexOf(titleMarker, StringComparison.Ordinal);
                if (titleAt < 0)
                {
                    continue;
                }

                string title = JsonUnescape(ReadJsonString(before, titleAt + titleMarker.Length));
                string compact = Compact(title);
                bool fileMatch = !string.IsNullOrEmpty(compactStem) && compact.Contains(compactStem);
                int sharedWords = string.IsNullOrEmpty(compactStem) ? SharedWords(compact, workTitle) : 0;
                if (!fileMatch && sharedWords == 0)
                {
                    continue;
                }

                int score = sharedWords * 10;
                if (compact.Contains("met")) score += 10;
                if (compact.Contains("detail")) score -= 8;
                if (compact.Contains("fxd")) score -= 4;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = thumb;
                }
            }

            return best;
        }

        private async Task<string> ThumbForTitleAsync(string title, int width, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(title))
            {
                return null;
            }

            string infoUrl =
                "https://commons.wikimedia.org/w/api.php?action=query&prop=imageinfo&iiprop=url&format=json&iiurlwidth="
                + width
                + "&titles="
                + UnityWebRequest.EscapeURL(title);
            byte[] infoBytes = await GetBytesAsync(infoUrl, cancellationToken);
            if (infoBytes == null)
            {
                return null;
            }

            string info = Encoding.UTF8.GetString(infoBytes);
            return JsonString(info, "thumburl") ?? JsonString(info, "url");
        }

        private async Task<string> SearchFileAsync(string query, string compactStem, string workTitle, CancellationToken cancellationToken)
        {
            string searchUrl =
                "https://commons.wikimedia.org/w/api.php?action=query&list=search&srnamespace=6&srlimit=8&format=json&srsearch="
                + UnityWebRequest.EscapeURL(query);
            byte[] searchBytes = await GetBytesAsync(searchUrl, cancellationToken);
            if (searchBytes == null)
            {
                return null;
            }

            return PickTitle(Encoding.UTF8.GetString(searchBytes), compactStem, workTitle);
        }

        private static string Compact(string value)
        {
            var text = new StringBuilder(value.Length);
            foreach (char character in value)
            {
                if (character != ' ' && character != '_' && character != '-')
                {
                    text.Append(char.ToLowerInvariant(character));
                }
            }

            return text.ToString();
        }

        private static string FileStem(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return string.Empty;
            }

            int query = url.IndexOf('?');
            string path = query >= 0 ? url.Substring(0, query) : url;
            int slash = path.LastIndexOf('/');
            string name = slash >= 0 ? path.Substring(slash + 1) : path;
            int dot = name.LastIndexOf('.');
            return dot > 0 ? name.Substring(0, dot) : name;
        }

        private static string PickTitle(string json, string compactStem, string workTitle)
        {
            string best = null;
            int bestScore = int.MinValue;
            int from = 0;
            const string marker = "\"title\":\"";
            while (from < json.Length)
            {
                int at = json.IndexOf(marker, from, StringComparison.Ordinal);
                if (at < 0)
                {
                    break;
                }

                int start = at + marker.Length;
                string title = JsonUnescape(ReadJsonString(json, start));
                from = start;
                string compact = Compact(title);
                bool fileMatch = !string.IsNullOrEmpty(compactStem) && compact.Contains(compactStem);
                int sharedWords = string.IsNullOrEmpty(compactStem) ? SharedWords(compact, workTitle) : 0;
                if (!fileMatch && sharedWords == 0)
                {
                    continue;
                }

                int score = sharedWords * 10;
                if (compact.Contains("met")) score += 10;
                if (compact.Contains("detail")) score -= 8;
                if (compact.Contains("fxd")) score -= 4;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = title;
                }
            }

            return best;
        }

        // How many of the painting's longer words show up in a Commons file name.
        private static int SharedWords(string compactTitle, string workTitle)
        {
            if (string.IsNullOrWhiteSpace(workTitle) || !compactTitle.Contains("met"))
            {
                return 0;
            }

            int shared = 0;
            foreach (string raw in workTitle.Split(new[] { ' ', '(', ')', ',', ':', ';', '—', '-' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string word = Compact(raw);
                if (word.Length >= 5 && compactTitle.Contains(word))
                {
                    shared++;
                }
            }

            return shared;
        }

        private static string JsonString(string json, string key)
        {
            string marker = "\"" + key + "\":\"";
            int at = json.IndexOf(marker, StringComparison.Ordinal);
            if (at < 0)
            {
                return null;
            }

            return JsonUnescape(ReadJsonString(json, at + marker.Length));
        }

        private static string ReadJsonString(string json, int start)
        {
            var value = new StringBuilder();
            for (int i = start; i < json.Length; i++)
            {
                char character = json[i];
                if (character == '\\' && i + 1 < json.Length)
                {
                    value.Append(character);
                    value.Append(json[i + 1]);
                    i++;
                    continue;
                }

                if (character == '"')
                {
                    break;
                }

                value.Append(character);
            }

            return value.ToString();
        }

        private static string JsonUnescape(string value)
        {
            var text = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] != '\\' || i + 1 >= value.Length)
                {
                    text.Append(value[i]);
                    continue;
                }

                char next = value[i + 1];
                if (next == '/')
                {
                    text.Append('/');
                    i++;
                }
                else if (next == '"')
                {
                    text.Append('"');
                    i++;
                }
                else if (next == 'u' && i + 5 < value.Length)
                {
                    text.Append((char)Convert.ToInt32(value.Substring(i + 2, 4), 16));
                    i += 5;
                }
                else
                {
                    text.Append(next);
                    i++;
                }
            }

            return text.ToString();
        }

        private static async Task<byte[]> GetBytesAsync(string url, CancellationToken cancellationToken)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("User-Agent", UserAgent);
                request.SetRequestHeader("Accept", "image/jpeg,image/png,image/webp,*/*;q=0.8");
                await WebRequestAsync.SendAsync(request, cancellationToken);
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"Request failed for {url}: {request.error}");
                    return null;
                }

                return request.downloadHandler.data;
            }
        }

        private static bool IsImage(byte[] data)
        {
            if (data == null || data.Length < 12)
            {
                return false;
            }

            if (data[0] == 0xFF && data[1] == 0xD8)
            {
                return true;
            }

            if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
            {
                return true;
            }

            if (data[0] == 'G' && data[1] == 'I' && data[2] == 'F')
            {
                return true;
            }

            return data[0] == 'R' && data[1] == 'I' && data[2] == 'F' && data[3] == 'F'
                && data[8] == 'W' && data[9] == 'E' && data[10] == 'B' && data[11] == 'P';
        }
    }
}
