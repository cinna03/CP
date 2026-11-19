using System;

namespace Cinnarios.Catalogue.Shop
{
    /// <summary>
    /// Mirrors the marketplace API's artwork record (GET /v1/integrations/ar/artworks/:sourceId)
    /// so Unity's JsonUtility can read it. Field names must match the JSON keys exactly.
    /// priceAmount is a string of US cents, for example "42000" for 420.00 USD.
    /// </summary>
    [Serializable]
    public sealed class ShopArtworkData
    {
        public string source;
        public string sourceId;
        public string title;
        public string artist;
        public string date;
        public string creditLine;
        public string mediumDescription;
        public string medium;
        public float widthCm;
        public float heightCm;
        public float depthCm;
        public string imageUrl;
        public string thumbnailUrl;
        public string priceAmount;
        public string currency;
        public string colourGuide;
        public string subjectGuide;
    }
}
