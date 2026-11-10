using System;

namespace Capstone.Catalogue
{
    /// <summary>
    /// Facts about one work on one wall. Every number comes from the stored artwork
    /// or from the wall size passed in. A missing wall size is said to be missing.
    /// Light has no stored reading, so that concern returns no fact.
    /// </summary>
    public static class PieceInRoom
    {
        public static string Fact(string concern, string language, Artwork artwork, float wallWidthCm, float wallHeightCm)
        {
            if (artwork == null || string.IsNullOrWhiteSpace(concern))
                return null;

            string tongue = language == "fr" || language == "rw" ? language : "en";
            bool wallKnown = wallWidthCm > 0f && wallHeightCm > 0f;
            switch (concern)
            {
                case "size":
                    return wallKnown
                        ? Fit(tongue, artwork, wallWidthCm, wallHeightCm, true)
                        : Size(tongue, artwork);
                case "fit":
                    return Fit(tongue, artwork, wallWidthCm, wallHeightCm, wallKnown);
                case "colour":
                    return Named(tongue, "colour", artwork.Colour, artwork.Title);
                case "meaning":
                    return Note(tongue, artwork.StoryNotes != null ? artwork.StoryNotes.Meaning : string.Empty);
                case "price":
                    return Named(tongue, "price", artwork.PriceBand, artwork.Title);
                case "artist":
                    return Artist(tongue, artwork);
                case "medium":
                    return Named(tongue, "medium", artwork.Medium, artwork.Title);
                case "style":
                    return Named(tongue, "style", artwork.Style, artwork.Title);
                case "mood":
                    return Named(tongue, "mood", artwork.Mood, artwork.Title);
                default:
                    return null;
            }
        }

        static string Size(string language, Artwork artwork)
        {
            string wide = Cm(artwork.WidthCm);
            string tall = Cm(artwork.HeightCm);
            if (language == "fr")
                return artwork.Title + " fait " + wide + " cm de large et " + tall + " cm de haut.";
            if (language == "rw")
                return artwork.Title + " ifite ubugari bwa " + wide + " cm n'uburebure bwa " + tall + " cm.";
            return artwork.Title + " is " + wide + " cm wide and " + tall + " cm tall.";
        }

        static string Fit(string language, Artwork artwork, float wallWidthCm, float wallHeightCm, bool wallKnown)
        {
            string size = Size(language, artwork);
            if (!wallKnown)
            {
                if (language == "fr")
                    return size + " La taille de ce mur n'est pas enregistrée.";
                if (language == "rw")
                    return size + " Ingano y'uru rukuta ntabwo yanditswe.";
                return size + " This wall's size is not stored.";
            }

            string wide = Cm(wallWidthCm);
            string tall = Cm(wallHeightCm);
            int widthShare = Share(artwork.WidthCm, wallWidthCm);
            int heightShare = Share(artwork.HeightCm, wallHeightCm);
            bool wider = artwork.WidthCm > wallWidthCm;
            bool taller = artwork.HeightCm > wallHeightCm;
            if (language == "fr")
            {
                return size
                    + " Le mur à l'écran fait " + wide + " cm de large et " + tall + " cm de haut."
                    + " " + RelationFr(wider, taller)
                    + " Elle couvre " + widthShare + " % de la largeur et " + heightShare + " % de la hauteur.";
            }

            if (language == "rw")
            {
                return size
                    + " Urukuta ruri ku rubuga rufite ubugari bwa " + wide + " cm n'uburebure bwa " + tall + " cm."
                    + " " + RelationRw(wider, taller)
                    + " Igikorwa gipfuka " + widthShare + " % by'ubugari na " + heightShare + " % by'uburebure.";
            }

            return size
                + " The wall on screen is " + wide + " cm wide and " + tall + " cm tall."
                + " " + RelationEn(wider, taller)
                + " It covers " + widthShare + "% of that width and " + heightShare + "% of that height.";
        }

        static string RelationEn(bool wider, bool taller)
        {
            if (wider && taller)
                return "The work is wider and taller than that wall.";
            if (wider)
                return "The work is wider than that wall.";
            if (taller)
                return "The work is taller than that wall.";
            return "The work fits inside that wall.";
        }

        static string RelationFr(bool wider, bool taller)
        {
            if (wider && taller)
                return "L'œuvre est plus large et plus haute que ce mur.";
            if (wider)
                return "L'œuvre est plus large que ce mur.";
            if (taller)
                return "L'œuvre est plus haute que ce mur.";
            return "L'œuvre tient dans ce mur.";
        }

        static string RelationRw(bool wider, bool taller)
        {
            if (wider && taller)
                return "Igikorwa kirarambye kandi kirarire kurusha urwo rukuta.";
            if (wider)
                return "Igikorwa kirarambye kurusha urwo rukuta.";
            if (taller)
                return "Igikorwa kirarire kurusha urwo rukuta.";
            return "Igikorwa kiba mu rukuta.";
        }

        static string Artist(string language, Artwork artwork)
        {
            if (string.IsNullOrWhiteSpace(artwork.Artist))
                return Missing(language, "artist");
            if (language == "fr")
                return artwork.Title + " est de " + artwork.Artist + ", " + artwork.Year + ".";
            if (language == "rw")
                return artwork.Title + " yakozwe na " + artwork.Artist + ", " + artwork.Year + ".";
            return artwork.Title + " is by " + artwork.Artist + ", " + artwork.Year + ".";
        }

        static string Note(string language, string meaning)
        {
            if (string.IsNullOrWhiteSpace(meaning))
                return Missing(language, "meaning");
            if (language == "fr")
                return "Le sens enregistré : " + meaning;
            if (language == "rw")
                return "Igisobanuro cyanditswe: " + meaning;
            return "The stored meaning: " + meaning;
        }

        static string Named(string language, string field, string value, string title)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Missing(language, field);
            if (language == "fr")
                return LabelFr(field) + " de " + title + " : " + value + ".";
            if (language == "rw")
                return LabelRw(field) + " ya " + title + ": " + value + ".";
            return "The stored " + LabelEn(field) + " of " + title + " is " + value + ".";
        }

        static string Missing(string language, string field)
        {
            if (language == "fr")
                return "Ce " + LabelFr(field).ToLowerInvariant() + " n'est pas enregistré.";
            if (language == "rw")
                return LabelRw(field) + " ntabwo yanditswe.";
            return "The " + LabelEn(field) + " is not stored.";
        }

        static string LabelEn(string field)
        {
            if (field == "colour")
                return "colour";
            if (field == "price")
                return "price band";
            if (field == "meaning")
                return "meaning";
            if (field == "artist")
                return "artist";
            if (field == "medium")
                return "medium";
            if (field == "style")
                return "style";
            return "mood";
        }

        static string LabelFr(string field)
        {
            if (field == "colour")
                return "La couleur enregistrée";
            if (field == "price")
                return "La fourchette de prix enregistrée";
            if (field == "meaning")
                return "Le sens";
            if (field == "artist")
                return "L'artiste";
            if (field == "medium")
                return "Le médium enregistré";
            if (field == "style")
                return "Le style enregistré";
            return "L'humeur enregistrée";
        }

        static string LabelRw(string field)
        {
            if (field == "colour")
                return "Ibara ryanditswe";
            if (field == "price")
                return "Igiciro cyanditswe";
            if (field == "meaning")
                return "Igisobanuro";
            if (field == "artist")
                return "Umuhanzi";
            if (field == "medium")
                return "Icyakoreshejwe cyanditswe";
            if (field == "style")
                return "Uburyo bwanditswe";
            return "Imimerere yanditswe";
        }

        static string Cm(float value)
        {
            return Math.Round(value, MidpointRounding.AwayFromZero).ToString("0", System.Globalization.CultureInfo.InvariantCulture);
        }

        static int Share(float part, float whole)
        {
            return (int)Math.Round(part / whole * 100d, MidpointRounding.AwayFromZero);
        }
    }
}
