using System;

namespace Cinnarios.Catalogue.Shop
{
    /// <summary>The marketplace API could not be reached or did not return the artwork.</summary>
    public sealed class ShopApiException : Exception
    {
        public ShopApiException(string message) : base(message)
        {
        }
    }
}
