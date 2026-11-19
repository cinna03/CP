using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cinnarios.Catalogue.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace Cinnarios.Catalogue.Shop
{
    /// <summary>
    /// Reads artworks from the marketplace API. The camera uses the shop's own record for a
    /// painting rather than asking The Met, so the shop and the camera always agree on size,
    /// price and credit line. The API address comes from the try link, never from the build.
    /// </summary>
    public sealed class ShopApiClient
    {
        private const int TimeoutSeconds = 15;

        private readonly string _apiBaseUrl;

        /// <param name="apiBaseUrl">For example http://192.168.1.67:3001</param>
        public ShopApiClient(string apiBaseUrl)
        {
            _apiBaseUrl = apiBaseUrl.TrimEnd('/');
        }

        /// <summary>GET /v1/integrations/ar/artworks/:sourceId (public, no sign-in needed).</summary>
        public async Task<ShopArtworkData> GetArArtworkAsync(string sourceId, CancellationToken cancellationToken = default)
        {
            string url = $"{_apiBaseUrl}/v1/integrations/ar/artworks/{UnityWebRequest.EscapeURL(sourceId)}";
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = TimeoutSeconds;
                await WebRequestAsync.SendAsync(request, cancellationToken);

                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new ShopApiException($"{url} failed ({request.responseCode}): {request.error}");
                }

                ShopArtworkData artwork = JsonUtility.FromJson<ShopArtworkData>(request.downloadHandler.text);
                if (artwork == null || string.IsNullOrEmpty(artwork.sourceId))
                {
                    throw new ShopApiException($"{url} did not return an artwork record.");
                }

                return artwork;
            }
        }

        /// <summary>GET /v1/catalog/artworks. The body is a JSON array, so only the ids are read.</summary>
        public async Task<List<string>> ListSourceIdsAsync(CancellationToken cancellationToken = default)
        {
            string url = $"{_apiBaseUrl}/v1/catalog/artworks";
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = TimeoutSeconds;
                await WebRequestAsync.SendAsync(request, cancellationToken);

                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new ShopApiException($"{url} failed ({request.responseCode}): {request.error}");
                }

                var ids = new List<string>();
                MatchCollection matches = Regex.Matches(request.downloadHandler.text, "\"sourceId\"\\s*:\\s*\"(\\d+)\"");
                foreach (Match match in matches)
                {
                    string sourceId = match.Groups[1].Value;
                    if (!ids.Contains(sourceId))
                    {
                        ids.Add(sourceId);
                    }
                }

                return ids;
            }
        }

        /// <summary>The same catalogue list, as records the decision step can compare.</summary>
        public async Task<List<ShopArtworkData>> ListArtworksAsync(CancellationToken cancellationToken = default)
        {
            string url = $"{_apiBaseUrl}/v1/catalog/artworks";
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = TimeoutSeconds;
                await WebRequestAsync.SendAsync(request, cancellationToken);
                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new ShopApiException($"{url} failed ({request.responseCode}): {request.error}");
                }

                return ParseArtworkArray(request.downloadHandler.text);
            }
        }

        /// <summary>Splits a JSON array into artwork records. JsonUtility cannot read a bare array.</summary>
        public static List<ShopArtworkData> ParseArtworkArray(string json)
        {
            var list = new List<ShopArtworkData>();
            if (string.IsNullOrEmpty(json))
            {
                return list;
            }

            int depth = 0;
            int start = -1;
            bool inString = false;
            bool escape = false;
            for (int i = 0; i < json.Length; i++)
            {
                char character = json[i];
                if (inString)
                {
                    if (escape)
                    {
                        escape = false;
                    }
                    else if (character == '\\')
                    {
                        escape = true;
                    }
                    else if (character == '"')
                    {
                        inString = false;
                    }

                    continue;
                }

                if (character == '"')
                {
                    inString = true;
                    continue;
                }

                if (character == '{')
                {
                    if (depth == 0)
                    {
                        start = i;
                    }

                    depth++;
                }
                else if (character == '}' && depth > 0)
                {
                    depth--;
                    if (depth == 0 && start >= 0)
                    {
                        ShopArtworkData artwork = JsonUtility.FromJson<ShopArtworkData>(json.Substring(start, i - start + 1));
                        if (artwork != null && !string.IsNullOrEmpty(artwork.sourceId))
                        {
                            list.Add(artwork);
                        }

                        start = -1;
                    }
                }
            }

            return list;
        }
    }
}
