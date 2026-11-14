using System;
using System.Collections.Generic;
using Capstone.App.Data;

namespace Capstone.App.Services
{
    /// <summary>
    /// Matches style, mood, and colour as whole words, and orientation and size tier from the default size.
    /// An open query returns every record that was passed in.
    /// </summary>
    [Serializable]
    public sealed class CatalogueFilterService : ICatalogueFilterService
    {
        public IReadOnlyList<ArtworkData> Matching(IReadOnlyList<ArtworkData> works, CatalogueQuery query)
        {
            if (works == null)
                throw new ArgumentNullException(nameof(works));

            var found = new List<ArtworkData>();
            for (int i = 0; i < works.Count; i++)
            {
                ArtworkData work = works[i];
                if (work == null)
                    continue;
                if (!Fits(work.Style, query.Style) || !Fits(work.Mood, query.Mood) || !Fits(work.Colour, query.Colour))
                    continue;
                if (query.Orientation.HasValue && work.Orientation != query.Orientation.Value)
                    continue;
                if (query.SizeTier.HasValue && work.SizeTier != query.SizeTier.Value)
                    continue;

                found.Add(work);
            }

            return found;
        }

        static bool Fits(string stored, string wanted)
        {
            if (string.IsNullOrWhiteSpace(wanted))
                return true;

            return string.Equals(stored, wanted.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
