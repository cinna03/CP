using System;
using Cinnarios.Painting.Data;

namespace Cinnarios.Catalogue.Pricing
{
    /// <summary>
    /// Prices a painting by its size: a base price plus a rate for every square metre of
    /// canvas, rounded to a tidy number. Bigger paintings cost more, consistently.
    /// Example with the defaults: a 60 x 80 cm painting is 0.48 m², so
    /// 150 + 0.48 x 400 = 342, rounded to 340.
    /// </summary>
    public sealed class AreaPricingRule : IPricingRule
    {
        public const decimal DefaultBasePrice = 150m;
        public const decimal DefaultPricePerSquareMetre = 400m;
        public const decimal DefaultRoundTo = 10m;

        private const decimal SquareCentimetresPerSquareMetre = 10000m;

        private readonly decimal _basePrice;
        private readonly decimal _pricePerSquareMetre;
        private readonly decimal _roundTo;

        public string CurrencyCode { get; }

        public AreaPricingRule(
            decimal basePrice = DefaultBasePrice,
            decimal pricePerSquareMetre = DefaultPricePerSquareMetre,
            decimal roundTo = DefaultRoundTo,
            string currencyCode = "USD")
        {
            if (basePrice < 0m || pricePerSquareMetre < 0m || roundTo <= 0m)
            {
                throw new ArgumentException("Prices cannot be negative and the rounding step must be positive.");
            }

            _basePrice = basePrice;
            _pricePerSquareMetre = pricePerSquareMetre;
            _roundTo = roundTo;
            CurrencyCode = currencyCode;
        }

        public decimal PriceFor(PaintingDimensions dimensions)
        {
            if (dimensions == null)
            {
                throw new ArgumentNullException(nameof(dimensions));
            }

            decimal areaSquareMetres = (decimal)dimensions.WidthCm * (decimal)dimensions.HeightCm / SquareCentimetresPerSquareMetre;
            decimal rawPrice = _basePrice + areaSquareMetres * _pricePerSquareMetre;
            return Math.Round(rawPrice / _roundTo, MidpointRounding.AwayFromZero) * _roundTo;
        }
    }
}
