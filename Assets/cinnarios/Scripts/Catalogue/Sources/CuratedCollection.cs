using System.Collections.Generic;
using UnityEngine;

namespace Cinnarios.Catalogue.Sources
{
    /// <summary>
    /// A hand-picked list of Met artworks, created as an asset in the Editor
    /// (Create > Cinnarios > Curated Collection). A new asset starts with a starter list,
    /// which can be edited freely; works that fail the filters are skipped and logged.
    /// </summary>
    [CreateAssetMenu(fileName = "CuratedCollection", menuName = "Cinnarios/Curated Collection")]
    public sealed class CuratedCollection : ScriptableObject
    {
        // Well-known paintings in The Met's collection, as a starting point.
        private static readonly int[] StarterMetObjectIds =
        {
            10065,  // Repose, John White Alexander
            436535, // Wheat Field with Cypresses, Vincent van Gogh
            436532, // Self-Portrait with a Straw Hat, Vincent van Gogh
            436528, // Irises, Vincent van Gogh
            436524, // Sunflowers, Vincent van Gogh
            437881, // Young Woman with a Water Pitcher, Johannes Vermeer
            437127, // Bridge over a Pond of Water Lilies, Claude Monet
            437133, // Garden at Sainte-Adresse, Claude Monet
            435809, // The Harvesters, Pieter Bruegel the Elder
            437394, // Aristotle with a Bust of Homer, Rembrandt
            436575, // View of Toledo, El Greco
            12127,  // Madame X, John Singer Sargent
            11417   // Washington Crossing the Delaware, Emanuel Leutze
        };

        [SerializeField] private string displayName = "Featured";
        [SerializeField] private List<int> metObjectIds = new List<int>(StarterMetObjectIds);

        public string DisplayName => displayName;
        public IReadOnlyList<int> MetObjectIds => metObjectIds;

        // Called by Unity when the asset is created or reset from its menu.
        private void Reset()
        {
            metObjectIds = new List<int>(StarterMetObjectIds);
        }
    }
}
