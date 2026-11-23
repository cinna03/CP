using Cinnarios.Painting.Data;

namespace Cinnarios.Catalogue.Pricing
{
    /// <summary>Decides what an artwork costs. Different rules can be swapped in without touching the catalogue.</summary>
    public interface IPricingRule
    {
        string CurrencyCode { get; }

        decimal PriceFor(PaintingDimensions dimensions);
    }
}
