using System;
using System.Collections.Generic;
using Capstone.App.Data;

namespace Capstone.App.Services
{
    /// <summary>
    /// The buyer's current narrowing of the catalogue. A blank word, or a null orientation or tier, leaves that field open.
    /// </summary>
    public struct CatalogueQuery
    {
        public string Style;
        public string Mood;
        public string Colour;
        public string Search;
        public Orientation? Orientation;
        public SizeTier? SizeTier;
    }

    /// <summary>
    /// Narrows the in-app records. The list itself stays on the caller, so this service holds no scene and no asset.
    /// </summary>
    public interface ICatalogueFilterService
    {
        IReadOnlyList<ArtworkData> Matching(IReadOnlyList<ArtworkData> works, CatalogueQuery query);
        IReadOnlyList<ArtworkData> Suggested(IReadOnlyList<ArtworkData> works, UserTasteProfile taste);
    }
}
