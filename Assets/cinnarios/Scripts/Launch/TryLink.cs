using System;
using System.Text.RegularExpressions;

namespace Cinnarios.Launch
{
    /// <summary>
    /// The link the shop's "Open in camera" button opens:
    /// cinnarios://try/436535?api=http%3A%2F%2F192.168.1.67%3A3001
    /// It carries only the painting (sourceId) and where the shop's API is. It carries no
    /// sign-in: who the person is will come later through a pairing code.
    /// </summary>
    public sealed class TryLink
    {
        public const string Prefix = "cinnarios://try/";

        private static readonly Regex SourceIdPattern = new Regex(@"^\d{1,12}$");

        public string SourceId { get; }
        public string ApiBaseUrl { get; }

        private TryLink(string sourceId, string apiBaseUrl)
        {
            SourceId = sourceId;
            ApiBaseUrl = apiBaseUrl;
        }

        public static bool TryParse(string url, out TryLink link, out string error)
        {
            link = null;
            error = null;

            if (string.IsNullOrWhiteSpace(url) || !url.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            {
                error = $"expected a link starting with {Prefix}";
                return false;
            }

            string rest = url.Substring(Prefix.Length);
            int queryStart = rest.IndexOf('?');
            string sourceId = (queryStart >= 0 ? rest.Substring(0, queryStart) : rest).Trim('/');
            string query = queryStart >= 0 ? rest.Substring(queryStart + 1) : string.Empty;

            if (!SourceIdPattern.IsMatch(sourceId))
            {
                error = $"'{sourceId}' is not a valid artwork id";
                return false;
            }

            string apiBaseUrl = ReadQueryValue(query, "api");
            if (string.IsNullOrEmpty(apiBaseUrl) ||
                !(apiBaseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                  apiBaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
            {
                error = "the link has no valid api address";
                return false;
            }

            link = new TryLink(sourceId, apiBaseUrl);
            return true;
        }

        private static string ReadQueryValue(string query, string key)
        {
            foreach (string pair in query.Split('&'))
            {
                int equals = pair.IndexOf('=');
                if (equals > 0 && string.Equals(pair.Substring(0, equals), key, StringComparison.OrdinalIgnoreCase))
                {
                    return Uri.UnescapeDataString(pair.Substring(equals + 1));
                }
            }

            return null;
        }
    }
}
