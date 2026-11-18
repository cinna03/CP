using System;
using Cinnarios.Catalogue.Met;

namespace Cinnarios.Catalogue.Filtering
{
    /// <summary>
    /// The shop sells paintings, so sculptures, vases and furniture are left out.
    /// Some Met departments (for example the American Wing) leave "classification" empty
    /// and record the type in "objectName" instead, so either field counts.
    /// </summary>
    public sealed class IsPaintingFilter : IArtworkFilter
    {
        public string Reason => "not classified as a painting";

        public bool Accepts(MetObjectData artwork) =>
            MentionsPainting(artwork.classification) || MentionsPainting(artwork.objectName);

        private static bool MentionsPainting(string text) =>
            text != null && text.IndexOf("painting", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
