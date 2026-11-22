using System;
using System.Text.RegularExpressions;

namespace Cinnarios.Launch
{
    /// <summary>
    /// The link the shop's "Open in camera" button opens:
    /// cinnarios-ar://try/436535?api=https%3A%2F%2Fcinnarios.onrender.com
    /// It carries only the painting (sourceId) and where the shop's API is. It carries no
    /// sign-in: who the person is will come later through a pairing code.
    /// cinnarios-ar is this app only. The older cinnarios address is still accepted, because
    /// another Unity app on the phone is also registered for that one.
    /// </summary>
    public sealed class TryLink
    {
        public const string Prefix = "cinnarios-ar://try/";
        private const string OlderPrefix = "cinnarios://try/";

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

            string prefix = MatchingPrefix(url);
            if (prefix == null)
            {
                error = $"expected a link starting with {Prefix}";
                return false;
            }

            string rest = url.Substring(prefix.Length);
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

        private static string MatchingPrefix(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            if (url.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            {
                return Prefix;
            }

            if (url.StartsWith(OlderPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return OlderPrefix;
            }

            return null;
        }
    }
}
