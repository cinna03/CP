using System;
using Capstone.App.Data;

namespace Capstone.App.Services
{
    /// <summary>
    /// The written reply. Size and colour are filled from the chosen size and the stored colour.
    /// English, French, and Kinyarwanda use the same word lists as the visit table.
    /// An empty story note stays empty, and the reply says so.
    /// </summary>
    [Serializable]
    public sealed class ArtworkQueryService : IArtworkQueryService
    {
        public const string FixedLine = "I can only answer size, colour, or the story note from the written table.";
        public const string EmptyStory = "No story note is stored for this artwork.";
        public const string EmptyColour = "No colour is stored.";

        public string DescribeSize(ArtworkData work, int sizeIndex)
        {
            if (work == null)
                throw new ArgumentNullException(nameof(work));
            if (!work.TryGetSizeCentimetres(sizeIndex, out UnityEngine.Vector2 size))
                return FixedLine;

            return "This work is " + ArtworkData.Centimetres(size.x) + " cm wide and " + ArtworkData.Centimetres(size.y) + " cm tall.";
        }

        public string DescribeColour(ArtworkData work)
        {
            if (work == null)
                throw new ArgumentNullException(nameof(work));
            if (string.IsNullOrWhiteSpace(work.Colour))
                return EmptyColour;

            return "The stored colour is " + work.Colour.Trim() + ".";
        }

        public string DescribeStory(ArtworkData work)
        {
            if (work == null)
                throw new ArgumentNullException(nameof(work));
            if (string.IsNullOrWhiteSpace(work.StoryNote))
                return EmptyStory;

            return work.StoryNote;
        }

        public string Answer(ArtworkData work, int sizeIndex, string question)
        {
            if (work == null)
                throw new ArgumentNullException(nameof(work));

            string asked = question ?? "";
            if (Contains(asked, "taille", "largeur", "hauteur"))
                return FrenchSize(work, sizeIndex);
            if (Contains(asked, "couleur"))
                return FrenchColour(work);
            if (Contains(asked, "ingano", "ubugari", "uburebure"))
                return KinyarwandaSize(work, sizeIndex);
            if (Contains(asked, "ibara"))
                return KinyarwandaColour(work);
            if (Contains(asked, "size", "centimetre", "centimeter", "width", "height"))
                return DescribeSize(work, sizeIndex);
            if (Contains(asked, "colour", "color"))
                return DescribeColour(work);
            if (Contains(asked, "story", "note", "meaning"))
                return DescribeStory(work);

            return FixedLine;
        }

        string FrenchSize(ArtworkData work, int sizeIndex)
        {
            if (!work.TryGetSizeCentimetres(sizeIndex, out UnityEngine.Vector2 size))
                return FixedLine;

            return "Cette oeuvre mesure " + ArtworkData.Centimetres(size.x) + " cm de largeur et " + ArtworkData.Centimetres(size.y) + " cm de hauteur.";
        }

        string FrenchColour(ArtworkData work)
        {
            if (string.IsNullOrWhiteSpace(work.Colour))
                return EmptyColour;

            return "La couleur enregistree est " + work.Colour.Trim() + ".";
        }

        string KinyarwandaSize(ArtworkData work, int sizeIndex)
        {
            if (!work.TryGetSizeCentimetres(sizeIndex, out UnityEngine.Vector2 size))
                return FixedLine;

            return "Iyi shusho ifite ubugari bwa " + ArtworkData.Centimetres(size.x) + " cm n'uburebure bwa " + ArtworkData.Centimetres(size.y) + " cm.";
        }

        string KinyarwandaColour(ArtworkData work)
        {
            if (string.IsNullOrWhiteSpace(work.Colour))
                return EmptyColour;

            return "Ibara ryanditswe ni " + work.Colour.Trim() + ".";
        }

        static bool Contains(string question, params string[] words)
        {
            for (int i = 0; i < words.Length; i++)
            {
                if (question.IndexOf(words[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }
    }
}
