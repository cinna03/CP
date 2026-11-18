using Cinnarios.Catalogue.Met;

namespace Cinnarios.Catalogue.Filtering
{
    /// <summary>Only public-domain works may be shown and reused freely (CC0).</summary>
    public sealed class PublicDomainFilter : IArtworkFilter
    {
        public string Reason => "not in the public domain";

        public bool Accepts(MetObjectData artwork) => artwork.isPublicDomain;
    }
}
