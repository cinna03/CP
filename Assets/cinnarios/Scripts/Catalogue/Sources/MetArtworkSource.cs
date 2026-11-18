using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cinnarios.Catalogue.Data;
using Cinnarios.Catalogue.Filtering;
using Cinnarios.Catalogue.Met;
using UnityEngine;

namespace Cinnarios.Catalogue.Sources
{
    /// <summary>
    /// Loads a list of Met artworks by object ID: fetches them all at once, keeps only those
    /// every filter accepts, and translates the rest into ArtworkRecords. Order is preserved.
    /// </summary>
    public sealed class MetArtworkSource : IArtworkSource
    {
        private readonly IReadOnlyList<int> _objectIds;
        private readonly MetCollectionClient _client;
        private readonly IReadOnlyList<IArtworkFilter> _filters;
        private readonly MetArtworkMapper _mapper;

        public MetArtworkSource(
            IReadOnlyList<int> objectIds,
            MetCollectionClient client,
            IReadOnlyList<IArtworkFilter> filters,
            MetArtworkMapper mapper)
        {
            _objectIds = objectIds;
            _client = client;
            _filters = filters;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<ArtworkRecord>> LoadAsync(CancellationToken cancellationToken = default)
        {
            // Requests run side by side, so loading takes about as long as the slowest one.
            Task<MetObjectData>[] requests = _objectIds.Select(id => _client.GetObjectAsync(id, cancellationToken)).ToArray();
            MetObjectData[] objects = await Task.WhenAll(requests);

            var records = new List<ArtworkRecord>(objects.Length);
            foreach (MetObjectData artwork in objects)
            {
                if (artwork == null)
                {
                    continue; // the client has already logged why
                }

                IArtworkFilter rejectedBy = _filters.FirstOrDefault(filter => !filter.Accepts(artwork));
                if (rejectedBy != null)
                {
                    Debug.Log($"Skipped Met {artwork.objectID} '{artwork.title}': {rejectedBy.Reason}");
                    continue;
                }

                if (_mapper.TryMap(artwork, out ArtworkRecord record))
                {
                    records.Add(record);
                }
            }

            return records;
        }
    }
}
