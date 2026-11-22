using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cinnarios.Catalogue.Data;
using Cinnarios.Catalogue.Networking;
using Cinnarios.Catalogue.Shop;
using Cinnarios.Painting.Data;
using Cinnarios.Painting.Presentation;
using Cinnarios.Placement;
using UnityEngine;

namespace Cinnarios.Launch
{
    /// <summary>
    /// Starts the camera on the painting the shop asked for. It reads the try link the app
    /// was opened with (or receives one while running), loads that artwork's record from the
    /// shop's API, waits for the image (capped at 2,048 px), then shows the painting.
    /// Opened without a link, it shows a fallback
    /// painting so the camera still works on its own.
    /// </summary>
    public sealed class ArtworkLaunchLoader : MonoBehaviour
    {
        private const int MaxTextureLongSide = 2048;

        [SerializeField] private WallPlacementController placement;
        [Tooltip("Editor only: the try link to test with, against the API on this laptop.")]
        [SerializeField] private string editorTestLink = "cinnarios://try/436535?api=http%3A%2F%2Flocalhost%3A3001";
        [Tooltip("Shown when the app is opened without a try link, for example from the home screen.")]
        [SerializeField] private PaintingSpecAsset fallbackPainting;

        private readonly TextureDownloader _downloader = new TextureDownloader();
        private readonly List<string> _catalogueIds = new List<string>();
        private List<ShopArtworkData> _catalogue;
        private readonly List<string> _tried = new List<string>();
        private CancellationTokenSource _loading;
        private TryLink _link;
        private int _index;
        private bool _waitForShop;

        /// <summary>Raised when another painting is put in the same spot, or the first one arrives.</summary>
        public event Action Changed;

        /// <summary>A plain-language description of what the loader is doing, for the debug overlay.</summary>
        public string Status { get; private set; } = "Waiting for a try link";

        /// <summary>The shop artwork currently on the wall, or null.</summary>
        public ArtworkRecord CurrentArtwork { get; private set; }

        /// <summary>True once the print itself is on the wall, not only its record.</summary>
        public bool PictureReady { get; private set; } = true;

        /// <summary>True when the last download failed.</summary>
        public bool PictureFailed { get; private set; }

        /// <summary>Paintings shown in this camera visit, in the order they were put on the wall.</summary>
        public IReadOnlyList<string> TriedSourceIds => _tried;

        /// <summary>True once the catalogue has another painting to put in this same spot.</summary>
        public bool CanSwap => _link != null && _catalogueIds.Count > 1;

        /// <summary>For example "2 of 11". Empty until the catalogue list has arrived.</summary>
        public string SwapCaption =>
            _catalogueIds.Count == 0 ? string.Empty : $"{_index + 1} of {_catalogueIds.Count}";

        private void OnEnable() => Application.deepLinkActivated += OnLinkReceived;

        private void OnDisable()
        {
            Application.deepLinkActivated -= OnLinkReceived;
            _loading?.Cancel();
        }

        /// <summary>
        /// The shop is about to cover the camera. Skip the automatic link so the painting
        /// loads only when the person asks to try it.
        /// </summary>
        public void WaitForTheShop() => _waitForShop = true;

        /// <summary>Loads the painting in a try link. This is a new visit, so the previous wall lock is cleared.</summary>
        public void OpenTryLink(string url)
        {
            _tried.Clear();
            placement.BeginNewVisit();
            OnLinkReceived(url);
        }

        /// <summary>
        /// Puts the previous or next catalogue painting in the holder. The wall position stays.
        /// The new painting uses its own centimetres.
        /// </summary>
        public void Swap(int step)
        {
            if (!CanSwap || step == 0)
            {
                return;
            }

            _index = (_index + step) % _catalogueIds.Count;
            if (_index < 0)
            {
                _index += _catalogueIds.Count;
            }

            _ = LoadSourceAsync(_catalogueIds[_index]);
        }

        /// <summary>Puts one catalogue painting in the holder. The locked spot does not move.</summary>
        public void ShowSource(string sourceId)
        {
            if (string.IsNullOrEmpty(sourceId) || _link == null)
            {
                return;
            }

            _ = LoadSourceAsync(sourceId);
        }

        /// <summary>The shop catalogue, loaded once per camera visit, for the suggestion step.</summary>
        public async Task<IReadOnlyList<ShopArtworkData>> CatalogueAsync()
        {
            if (_catalogue != null && _catalogue.Count > 0)
            {
                return _catalogue;
            }

            if (_link == null)
            {
                return new List<ShopArtworkData>();
            }

            _catalogue = await new ShopApiClient(_link.ApiBaseUrl).ListArtworksAsync();
            return _catalogue;
        }

        private void Start()
        {
            if (_waitForShop)
            {
                SetStatus("Waiting for the shop");
                return;
            }

            string link = Application.absoluteURL;
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(link))
            {
                link = editorTestLink;
            }
#endif
            if (string.IsNullOrEmpty(link))
            {
                ShowFallback("Opened without a try link");
                return;
            }

            OnLinkReceived(link);
        }

        // Errors are caught inside LoadAsync and shown in Status, so nothing is lost here.
        private void OnLinkReceived(string url) => _ = LoadAsync(url);

        private async Task LoadAsync(string url)
        {
            if (!TryLink.TryParse(url, out TryLink link, out string error))
            {
                ShowFallback($"Link not understood ({error})");
                return;
            }

            _link = link;
            await LoadSourceAsync(link.SourceId);
        }

        private async Task LoadSourceAsync(string sourceId)
        {
            if (_link.SourceId == null)
            {
                return;
            }

            _loading?.Cancel();
            _loading = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            CancellationToken cancellationToken = _loading.Token;

            try
            {
                var client = new ShopApiClient(_link.ApiBaseUrl);
                SetStatus($"Loading artwork {sourceId} from {_link.ApiBaseUrl}");
                ShopArtworkData data = await client.GetArArtworkAsync(sourceId, cancellationToken);
                ArtworkRecord artwork = ShopArtworkMapper.ToRecord(data);
                CurrentArtwork = artwork;
                RememberTried(artwork.SourceId);
                PictureReady = false;
                PictureFailed = false;
                // The wall search must not wait on the image. A plain card at the recorded
                // centimetres lets the phone find the wall while the picture is still downloading.
                placement.ShowPlaceholder(artwork.ToSpec(Paper()));
                Changed?.Invoke();
                SetStatus($"Downloading the image for {artwork.Title}");

                Task catalogue = EnsureCatalogueAsync(client, artwork.SourceId, cancellationToken);
                Texture2D image = await _downloader.GetAsync(artwork.ImageUrl, 1600, artwork.Title, cancellationToken);
                await catalogue;
                if (image == null)
                {
                    PictureFailed = true;
                    SetStatus($"{artwork.Title}: the image could not be downloaded, so it was not put on the wall");
                    Changed?.Invoke();
                    return;
                }

                Texture2D limited = TextureLimiter.LimitLongSide(image, MaxTextureLongSide);
                bool shown = placement.ShowPainting(artwork.ToSpec(limited));
                PictureReady = shown;
                PictureFailed = !shown;
                SetStatus(shown
                    ? $"{artwork.Title} | {artwork.Dimensions} | {artwork.Price:0.00} {artwork.CurrencyCode}"
                    : $"{artwork.Title}: the image does not match its size");
                Changed?.Invoke();
            }
            catch (OperationCanceledException)
            {
                // A newer link replaced this one, or the scene closed.
            }
            catch (Exception exception)
            {
                SetStatus($"Could not load the artwork: {exception.Message}");
                Debug.LogException(exception, this);
            }
        }

        private async Task EnsureCatalogueAsync(ShopApiClient client, string sourceId, CancellationToken cancellationToken)
        {
            if (_catalogueIds.Count > 0)
            {
                if (!_catalogueIds.Contains(sourceId))
                {
                    _catalogueIds.Add(sourceId);
                }

                _index = _catalogueIds.IndexOf(sourceId);
                return;
            }

            try
            {
                List<string> ids = await client.ListSourceIdsAsync(cancellationToken);
                if (ids.Count == 0)
                {
                    ids.Add(sourceId);
                }

                _catalogueIds.Clear();
                _catalogueIds.AddRange(ids);
                _index = Math.Max(0, _catalogueIds.IndexOf(sourceId));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Launch] The catalogue list did not load ({exception.Message}). Swapping stays on this painting.", this);
                _catalogueIds.Clear();
                _catalogueIds.Add(sourceId);
                _index = 0;
            }
        }

        private void RememberTried(string sourceId)
        {
            if (!_tried.Contains(sourceId))
            {
                _tried.Add(sourceId);
            }
        }

        private void ShowFallback(string reason)
        {
            if (fallbackPainting != null)
            {
                placement.ShowPainting(fallbackPainting.ToSpec());
                SetStatus($"{reason}: showing the fallback painting");
            }
            else
            {
                SetStatus(reason);
            }
        }

        private static Texture2D _paper;

        private static Texture2D Paper()
        {
            if (_paper != null)
            {
                return _paper;
            }

            _paper = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Color paper = new Color(0.93f, 0.9f, 0.84f, 1f);
            Color[] pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = paper;
            }

            _paper.SetPixels(pixels);
            _paper.Apply(false, true);
            return _paper;
        }

        private void SetStatus(string status)
        {
            Status = status;
            Debug.Log($"[Launch] {status}", this);
        }
    }
}
