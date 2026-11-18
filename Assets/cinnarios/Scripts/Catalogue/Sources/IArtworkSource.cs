using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cinnarios.Catalogue.Data;

namespace Cinnarios.Catalogue.Sources
{
    /// <summary>
    /// "Give me artworks." The shop depends only on this, so artworks can come from the Met,
    /// from local test assets or, later, from artists' uploads without changing the shop.
    /// </summary>
    public interface IArtworkSource
    {
        Task<IReadOnlyList<ArtworkRecord>> LoadAsync(CancellationToken cancellationToken = default);
    }
}
