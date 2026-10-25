using System;
using System.Collections.Generic;

namespace Capstone.Catalogue
{
    /// <summary>
    /// Keeps the works whose stored style, mood, and colour match the request.
    /// A blank tag does not restrict that field. A positive maximum drops a work that is larger.
    /// Zero means that side is not limited. The stored centimetres are not changed.
    /// </summary>
    public static class CatalogueFilter
    {
        public static List<Artwork> Matching(
            IReadOnlyList<Artwork> works,
            string style,
            string mood,
            string colour,
            float maxWidthCm,
            float maxHeightCm)
        {
            List<Artwork> matches = new List<Artwork>();
            if (works == null)
                return matches;

            for (int i = 0; i < works.Count; i++)
            {
                Artwork work = works[i];
                if (work == null)
                    continue;
                if (!Same(style, work.Style) || !Same(mood, work.Mood) || !Same(colour, work.Colour))
                    continue;
                if (maxWidthCm > 0f && work.WidthCm > maxWidthCm)
                    continue;
                if (maxHeightCm > 0f && work.HeightCm > maxHeightCm)
                    continue;
                matches.Add(work);
            }

            return matches;
        }

        static bool Same(string wanted, string actual)
        {
            if (string.IsNullOrWhiteSpace(wanted))
                return true;

            string stored = string.IsNullOrWhiteSpace(actual) ? string.Empty : actual.Trim();
            return string.Equals(wanted.Trim(), stored, StringComparison.OrdinalIgnoreCase);
        }
    }
}
