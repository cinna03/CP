using System;
using System.Collections.Generic;

namespace Capstone.Catalogue
{
    /// <summary>
    /// A fixed list of works. An empty style, mood, or colour means that field is not used to narrow the list.
    /// </summary>
    public sealed class CatalogueFilter
    {
        readonly List<Artwork> works;

        public CatalogueFilter(IEnumerable<Artwork> works)
        {
            if (works == null)
                throw new ArgumentNullException(nameof(works));

            this.works = new List<Artwork>(works);
        }

        public IReadOnlyList<Artwork> All => works;

        public IReadOnlyList<Artwork> Matching(string style, string mood, string colour)
        {
            var found = new List<Artwork>();
            for (int i = 0; i < works.Count; i++)
            {
                Artwork work = works[i];
                if (Fits(work.Style, style) && Fits(work.Mood, mood) && Fits(work.Colour, colour))
                    found.Add(work);
            }

            return found;
        }

        public Artwork Find(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            for (int i = 0; i < works.Count; i++)
            {
                if (string.Equals(works[i].Id, id.Trim(), StringComparison.Ordinal))
                    return works[i];
            }

            return null;
        }

        static bool Fits(string stored, string wanted)
        {
            if (string.IsNullOrWhiteSpace(wanted))
                return true;

            return string.Equals(stored, wanted.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
