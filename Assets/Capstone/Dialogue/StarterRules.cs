using System.Collections.Generic;

namespace Capstone.Dialogue
{
    /// <summary>
    /// Keyword rows for the concerns in the recommendation flow.
    /// When the word counts tie, the order is size, fit, light, colour, meaning, then price.
    /// Kinyarwanda wording is a starter and should be checked by a speaker before the pilot.
    /// </summary>
    public static class StarterRules
    {
        public static IReadOnlyList<DialogueRule> All { get; } = new DialogueRule[]
        {
            new DialogueRule(
                "size",
                "size",
                6,
                new[] { "size", "big", "small", "large", "wide", "tall" },
                "Size is the stored height and width of the work, not a guess.",
                new[] { "taille", "grand", "petit", "largeur", "hauteur" },
                "La taille est la hauteur et la largeur enregistrées, pas une estimation.",
                new[] { "ingano", "kinini", "gito" },
                "Ingano ni iyanditswe, ntabwo ari ukugereranya."),
            new DialogueRule(
                "fit",
                "fit",
                5,
                new[] { "fit", "fitting", "suit", "suitable" },
                "Fit compares the stored height and width with the wall.",
                new[] { "convient" },
                "L'ajustement compare la hauteur et la largeur enregistrées au mur.",
                new[] { "bikwiye" },
                "Bikwiye bigereranya ingano yanditswe."),
            new DialogueRule(
                "light",
                "light",
                4,
                new[] { "light", "lighting", "bright", "dark" },
                "Light is seen in the room. This rule does not invent a light reading.",
                new[] { "lumiere", "lumière", "sombre" },
                "La lumière se juge dans la pièce. Cette règle n'invente pas une mesure.",
                new[] { "urumuri" },
                "Urumuri buraboneka mu cyumba."),
            new DialogueRule(
                "colour",
                "colour",
                3,
                new[] { "colour", "color", "blue", "orange" },
                "Colour fit uses the colour stored on the work.",
                new[] { "couleur", "bleu" },
                "La couleur est celle qui est enregistrée sur l'œuvre.",
                new[] { "ibara" },
                "Ibara ni iryanditswe ku gikoresho."),
            new DialogueRule(
                "meaning",
                "meaning",
                2,
                new[] { "meaning", "story", "inspiration" },
                "Meaning is the story note stored on the work.",
                new[] { "sens", "histoire", "signification" },
                "Le sens est la note enregistrée sur l'œuvre.",
                new[] { "igisobanuro" },
                "Igisobanuro ni inyandiko yabitswe."),
            new DialogueRule(
                "price",
                "price",
                1,
                new[] { "price", "cost", "expensive", "cheap" },
                "Price is the stored price band, not a negotiated amount.",
                new[] { "prix", "cher" },
                "Le prix est la fourchette enregistrée, pas un montant négocié.",
                new[] { "igiciro" },
                "Igiciro ni iryanditswe, ntabwo ari amafaranga yagiranye."),
        };
    }
}
