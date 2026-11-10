using System.Collections.Generic;

namespace Capstone.Dialogue
{
    /// <summary>
    /// Keyword rows for questions about the piece and how it fits the wall.
    /// When the word counts tie, the order is size, fit, light, colour, meaning, price, artist, medium, style, then mood.
    /// Kinyarwanda wording is a starter and should be checked by a speaker before the pilot.
    /// </summary>
    public static class StarterRules
    {
        public static IReadOnlyList<DialogueRule> All { get; } = new DialogueRule[]
        {
            new DialogueRule(
                "size",
                "size",
                10,
                new[] { "size", "big", "small", "large", "wide", "tall", "huge", "tiny", "height", "width" },
                "Size is the stored height and width of the work, not a guess.",
                new[] { "taille", "grand", "petit", "largeur", "hauteur" },
                "La taille est la hauteur et la largeur enregistrées, pas une estimation.",
                new[] { "ingano", "kinini", "gito" },
                "Ingano ni iyanditswe, ntabwo ari ukugereranya."),
            new DialogueRule(
                "fit",
                "fit",
                9,
                new[] { "fit", "fitting", "suit", "suitable", "room", "wall", "space", "hang", "hanging" },
                "Fit compares the stored height and width with the wall.",
                new[] { "convient" },
                "L'ajustement compare la hauteur et la largeur enregistrées au mur.",
                new[] { "bikwiye" },
                "Bikwiye bigereranya ingano yanditswe."),
            new DialogueRule(
                "light",
                "light",
                8,
                new[] { "light", "lighting", "bright", "dark", "sun", "shadow", "glare" },
                "Light is seen in the room. This rule does not invent a light reading.",
                new[] { "lumiere", "lumière", "sombre" },
                "La lumière se juge dans la pièce. Cette règle n'invente pas une mesure.",
                new[] { "urumuri" },
                "Urumuri buraboneka mu cyumba."),
            new DialogueRule(
                "colour",
                "colour",
                7,
                new[] { "colour", "color", "blue", "orange", "red", "palette" },
                "Colour fit uses the colour stored on the work.",
                new[] { "couleur", "bleu" },
                "La couleur est celle qui est enregistrée sur l'œuvre.",
                new[] { "ibara" },
                "Ibara ni iryanditswe ku gikoresho."),
            new DialogueRule(
                "meaning",
                "meaning",
                6,
                new[] { "meaning", "story", "inspiration", "mean" },
                "Meaning is the story note stored on the work.",
                new[] { "sens", "histoire", "signification" },
                "Le sens est la note enregistrée sur l'œuvre.",
                new[] { "igisobanuro" },
                "Igisobanuro ni inyandiko yabitswe."),
            new DialogueRule(
                "price",
                "price",
                5,
                new[] { "price", "cost", "expensive", "cheap", "budget" },
                "Price is the stored price band, not a negotiated amount.",
                new[] { "prix", "cher" },
                "Le prix est la fourchette enregistrée, pas un montant négocié.",
                new[] { "igiciro" },
                "Igiciro ni iryanditswe, ntabwo ari amafaranga yagiranye."),
            new DialogueRule(
                "artist",
                "artist",
                4,
                new[] { "artist", "painter", "maker", "who" },
                "The artist is the name stored on the work.",
                new[] { "artiste", "peintre", "auteur" },
                "L'artiste est le nom enregistré sur l'œuvre.",
                new[] { "umuhanzi" },
                "Umuhanzi ni uwanditswe ku gikorwa."),
            new DialogueRule(
                "medium",
                "medium",
                3,
                new[] { "medium", "material", "canvas", "oil", "acrylic" },
                "The medium is the material stored on the work.",
                new[] { "medium", "materiau", "matériau", "toile" },
                "Le médium est le matériau enregistré sur l'œuvre.",
                new[] { "ikintu" },
                "Icyakoreshejwe ni icyanditswe ku gikorwa."),
            new DialogueRule(
                "style",
                "style",
                2,
                new[] { "style", "genre" },
                "Style is the tag stored on the work.",
                new[] { "style", "genre" },
                "Le style est l'étiquette enregistrée sur l'œuvre.",
                new[] { "uburyo" },
                "Uburyo ni ubwanditswe ku gikorwa."),
            new DialogueRule(
                "mood",
                "mood",
                1,
                new[] { "mood", "feeling", "calm" },
                "Mood is the tag stored on the work.",
                new[] { "humeur" },
                "L'humeur est l'étiquette enregistrée sur l'œuvre.",
                new[] { "imimerere" },
                "Imimerere ni iyanditswe ku gikorwa."),
        };
    }
}
