using System.Globalization;
using Cinnarios.Catalogue.Data;
using Cinnarios.Catalogue.Met;
using Cinnarios.Painting.Data;

namespace Cinnarios.Catalogue.Shop
{
    /// <summary>
    /// Turns the shop's artwork record into the camera's ArtworkRecord. The size is the shop's
    /// centimetres, unchanged, and the price is the shop's price converted from cents to
    /// dollars: the camera never invents its own price.
    /// </summary>
    public static class ShopArtworkMapper
    {
        private const decimal CentsPerDollar = 100m;

        public static ArtworkRecord ToRecord(ShopArtworkData artwork)
        {
            PaintingMedium medium = MetMediumParser.Parse(artwork.mediumDescription);
            float depthCm = artwork.depthCm > 0f ? artwork.depthCm : medium.DefaultDepthCm();
            var dimensions = new PaintingDimensions(artwork.widthCm, artwork.heightCm, depthCm);

            decimal price = decimal.TryParse(artwork.priceAmount, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal cents)
                ? cents / CentsPerDollar
                : 0m;

            return new ArtworkRecord(
                artwork.sourceId,
                artwork.title,
                artwork.artist,
                artwork.date,
                artwork.creditLine,
                artwork.mediumDescription,
                medium,
                dimensions,
                artwork.imageUrl,
                artwork.thumbnailUrl,
                price,
                artwork.currency);
        }
    }
}
