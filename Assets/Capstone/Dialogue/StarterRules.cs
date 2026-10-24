using System.Collections.Generic;

namespace Capstone.Dialogue
{
    /// <summary>
    /// The first keyword rows. Size outranks colour, and colour outranks price, when the word counts tie.
    /// Kinyarwanda wording is a starter and should be checked by a speaker before the pilot.
    /// </summary>
    public static class StarterRules
    {
        public static IReadOnlyList<DialogueRule> All { get; } = new DialogueRule[]
        {
            new DialogueRule(
                "size",
                "size",
                3,
                new[] { "size", "big", "small", "large", "wide", "tall" },
                "Size is the stored height and width of the work, not a guess.",
                new[] { "taille", "grand", "petit", "largeur", "hauteur" },
                "La taille est la hauteur et la largeur enregistrées, pas une estimation.",
                new[] { "ingano", "kinini", "gito" },
                "Ingano ni iyanditswe, ntabwo ari ukugereranya."),
            new DialogueRule(
                "colour",
                "colour",
                2,
                new[] { "colour", "color", "blue", "orange" },
                "Colour fit uses the colour stored on the work.",
                new[] { "couleur", "bleu" },
                "La couleur est celle qui est enregistrée sur l'œuvre.",
                new[] { "ibara" },
                "Ibara ni iryanditswe ku gikoresho."),
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
