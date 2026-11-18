using Cinnarios.Catalogue.Met;

namespace Cinnarios.Catalogue.Filtering
{
    /// <summary>Without an image there is nothing to put on the painting.</summary>
    public sealed class HasImageFilter : IArtworkFilter
    {
        public string Reason => "no image available";

        public bool Accepts(MetObjectData artwork) => !string.IsNullOrWhiteSpace(artwork.primaryImage);
    }
}
