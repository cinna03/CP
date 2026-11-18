using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cinnarios.Catalogue.Data;
using Cinnarios.Catalogue.Filtering;
using Cinnarios.Catalogue.Met;
using Cinnarios.Catalogue.Networking;
using Cinnarios.Catalogue.Pricing;
using Cinnarios.Catalogue.Sources;
using Cinnarios.Painting.Data;
using Cinnarios.Painting.Presentation;
using Cinnarios.Painting.Validation;
using UnityEngine;

namespace Cinnarios.Sandbox
{
    /// <summary>
    /// Test harness only, not part of the app: loads a curated Met collection, logs every
    /// artwork with its size and price, and shows them one at a time on the same painting
    /// holder (right-click the component > Show Next Artwork), the way swapping will work on a wall.
    /// </summary>
    [RequireComponent(typeof(PaintingView))]
    public sealed class CatalogueSandbox : MonoBehaviour
    {
        [SerializeField] private CuratedCollection collection;
        [Tooltip("Use the Met's smaller images: faster to download and lighter on memory.")]
        [SerializeField] private bool useSmallImages = true;

        private readonly TextureDownloader _downloader = new TextureDownloader();
        private IReadOnlyList<ArtworkRecord> _artworks = Array.Empty<ArtworkRecord>();
        private int _currentIndex = -1;

        private async void Start()
        {
            if (collection == null)
            {
                Debug.LogWarning("CatalogueSandbox: assign a Curated Collection first.", this);
                return;
            }

            IArtworkSource source = new MetArtworkSource(
                collection.MetObjectIds,
                new MetCollectionClient(),
                new IArtworkFilter[] { new PublicDomainFilter(), new HasImageFilter(), new IsPaintingFilter(), new HasDimensionsFilter() },
                new MetArtworkMapper(new AreaPricingRule()));

            try
            {
                _artworks = await source.LoadAsync(destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                return; // the scene was stopped while loading
            }

            Debug.Log($"Loaded {_artworks.Count} of {collection.MetObjectIds.Count} artworks from '{collection.DisplayName}'.", this);
            foreach (ArtworkRecord artwork in _artworks)
            {
                Debug.Log($"  {artwork.Title} | {artwork.Artist} | {artwork.Dimensions} | {artwork.Medium} ({artwork.MediumDescription}) | {artwork.Price:0} {artwork.CurrencyCode}");
            }

            await ShowNextAsync();
        }

        [ContextMenu("Show Next Artwork")]
        public async void ShowNext() => await ShowNextAsync();

        private async Task ShowNextAsync()
        {
            if (_artworks.Count == 0)
            {
                return;
            }

            _currentIndex = (_currentIndex + 1) % _artworks.Count;
            ArtworkRecord artwork = _artworks[_currentIndex];

            Texture2D image;
            try
            {
                image = await _downloader.GetAsync(useSmallImages ? artwork.ThumbnailUrl : artwork.ImageUrl, destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (image == null)
            {
                return;
            }

            if (!PaintingPreparation.TryPrepare(artwork.ToSpec(image), out PaintingSpec ready, out ImageFitResult fit))
            {
                Debug.LogWarning($"{artwork.Title} not shown: {fit}", this);
                return;
            }

            GetComponent<PaintingView>().Show(ready, fit.CropRect);
            Debug.Log($"Showing {_currentIndex + 1}/{_artworks.Count}: {ready}. Image fit: {fit}", this);
        }
    }
}
