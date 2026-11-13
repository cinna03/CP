using System;
using Capstone.Catalogue;

namespace Capstone.Dialogue
{
    /// <summary>
    /// A written reply table. An unknown question returns one fixed line and adds no fact.
    /// </summary>
    public static class RuleTable
    {
        public const string FixedLine = "I can only answer size, colour, or the story note from the written table.";

        public static string Answer(Artwork work, string question)
        {
            if (work == null)
                throw new ArgumentNullException(nameof(work));

            string asked = question ?? "";
            if (Contains(asked, "taille", "largeur", "hauteur"))
                return FrenchSize(work);
            if (Contains(asked, "couleur"))
                return "La couleur enregistree est " + work.Colour + ".";
            if (Contains(asked, "ingano", "ubugari", "uburebure"))
                return KinyarwandaSize(work);
            if (Contains(asked, "ibara"))
                return "Ibara ryanditswe ni " + work.Colour + ".";
            if (Contains(asked, "size", "centimetre", "centimeter", "width", "height"))
                return EnglishSize(work);
            if (Contains(asked, "colour", "color"))
                return "The stored colour is " + work.Colour + ".";
            if (Contains(asked, "story", "note", "meaning"))
                return string.IsNullOrEmpty(work.StoryNote) ? "No story note is stored." : work.StoryNote;

            return FixedLine;
        }

        static string EnglishSize(Artwork work)
        {
            return "This work is " + Centimetres(work.WidthCentimetres) + " cm wide and " + Centimetres(work.HeightCentimetres) + " cm tall.";
        }

        static string FrenchSize(Artwork work)
        {
            return "Cette oeuvre mesure " + Centimetres(work.WidthCentimetres) + " cm de large et " + Centimetres(work.HeightCentimetres) + " cm de haut.";
        }

        static string KinyarwandaSize(Artwork work)
        {
            return "Iyi shusho ifite ubugari bwa " + Centimetres(work.WidthCentimetres) + " cm n'uburebure bwa " + Centimetres(work.HeightCentimetres) + " cm.";
        }

        static string Centimetres(float value)
        {
            return value.ToString("0.##");
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
