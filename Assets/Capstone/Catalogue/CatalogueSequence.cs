using System.Collections.Generic;

namespace Capstone.Catalogue
{
    /// <summary>
    /// Walks a catalogue in stored order and wraps from the last work back to the first.
    /// The next work keeps its own centimetres. Nothing is invented.
    /// </summary>
    public static class CatalogueSequence
    {
        public static Artwork Next(IReadOnlyList<Artwork> works, string currentId)
        {
            if (works == null || works.Count == 0)
                return null;

            int currentIndex = -1;
            for (int i = 0; i < works.Count; i++)
            {
                if (works[i] != null && works[i].Id == currentId)
                {
                    currentIndex = i;
                    break;
                }
            }

            int start = currentIndex < 0 ? -1 : currentIndex;
            for (int step = 1; step <= works.Count; step++)
            {
                int index = (start + step) % works.Count;
                if (works[index] != null)
                    return works[index];
            }

            return null;
        }
    }
}
