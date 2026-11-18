using Cinnarios.Catalogue.Data;
using Cinnarios.Catalogue.Pricing;
using Cinnarios.Painting.Data;

namespace Cinnarios.Catalogue.Met
{
    /// <summary>
    /// Translates a Met object into the shop's ArtworkRecord: picks the painting's size,
    /// reads the medium, fills in a missing depth, prices it and builds the credit line
    /// in the form the Met asks for.
    /// </summary>
    public sealed class MetArtworkMapper
    {
        private const string MetCreditPrefix = "The Metropolitan Museum of Art, New York, ";

        private readonly IPricingRule _pricing;

        public MetArtworkMapper(IPricingRule pricing)
        {
            _pricing = pricing;
        }

        public bool TryMap(MetObjectData artwork, out ArtworkRecord record)
        {
            record = null;
            if (artwork == null || !MetMeasurementSelector.TryGetPaintingSize(artwork, out MetElementMeasurements size))
            {
                return false;
            }

            PaintingMedium medium = MetMediumParser.Parse(artwork.medium);
            float depthCm = size.Depth > 0f ? size.Depth : medium.DefaultDepthCm();
            var dimensions = new PaintingDimensions(size.Width, size.Height, depthCm);

            record = new ArtworkRecord(
                artwork.objectID.ToString(),
                artwork.title,
                artwork.artistDisplayName,
                artwork.objectDate,
                MetCreditPrefix + artwork.creditLine,
                artwork.medium,
                medium,
                dimensions,
                artwork.primaryImage,
                artwork.primaryImageSmall,
                _pricing.PriceFor(dimensions),
                _pricing.CurrencyCode);
            return true;
        }
    }
}
