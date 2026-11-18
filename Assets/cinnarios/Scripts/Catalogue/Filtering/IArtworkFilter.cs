using Cinnarios.Catalogue.Met;

namespace Cinnarios.Catalogue.Filtering
{
    /// <summary>
    /// One rule an artwork must pass to appear in the shop. Rules are applied one after
    /// another and the first one that rejects an artwork explains why.
    /// </summary>
    public interface IArtworkFilter
    {
        /// <summary>Plain-language reason shown when this filter rejects an artwork.</summary>
        string Reason { get; }

        bool Accepts(MetObjectData artwork);
    }
}
