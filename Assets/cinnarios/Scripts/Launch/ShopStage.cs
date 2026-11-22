using Gree.UnityWebView;
using UnityEngine;

namespace Cinnarios.Launch
{
    /// <summary>
    /// Shows the shop when the app opens. The camera stays off until the page asks to try a
    /// painting. The website is paused before the camera starts, and the camera is stopped
    /// before the website is shown again. The page is not reloaded, so a sign-in survives.
    /// </summary>
    public sealed class ShopStage : MonoBehaviour
    {
        [Tooltip("On: the phone carries the shop, and the laptop can be off. Off: open the address below, which is the shop running on a computer.")]
        [SerializeField] private bool carryTheShopOnThePhone = true;
        [Tooltip("Used only when Carry The Shop On The Phone is off.")]
        [SerializeField] private string shopPageUrl = "https://cinnarios.onrender.com";
        private string _pageAddress;
        [SerializeField] private ArtworkLaunchLoader loader;
        [Tooltip("AR Session, XR Origin, Placement, and the placement HUD. All of them stay off while the shop is on screen.")]
        [SerializeField] private GameObject[] cameraParts;

#if CINNARIOS_WEBVIEW
        private WebViewObject _page;
        private bool _showingShop;
        private bool _openingCamera;
#endif

        private void Awake()
        {
            _pageAddress = carryTheShopOnThePhone ? PhoneShopServer.EnsureStarted() : shopPageUrl;
            if (string.IsNullOrWhiteSpace(_pageAddress) || loader == null)
            {
                return;
            }

            loader.WaitForTheShop();
            SetCameraActive(false);
#if !CINNARIOS_WEBVIEW
            Debug.LogWarning("The shop is ready, but the WebView plugin is not switched on. The camera stays off.", this);
#endif
        }

#if CINNARIOS_WEBVIEW
        private void Start()
        {
            if (string.IsNullOrWhiteSpace(_pageAddress) || loader == null)
            {
                return;
            }

            _page = gameObject.AddComponent<WebViewObject>();
            _page.Init(
                cb: OnPageMessage,
                err: (message) => Debug.LogWarning($"Shop page: {message}", this),
                httpErr: (message) => Debug.LogWarning($"Shop page: {message}", this),
                hooked: OnPageMessage,
                enableWKWebView: true,
                wkAllowsLinkPreview: false,
                separated: Application.isEditor);
            // A camera link must stay in this app. Without this, the page hand-off asks iOS
            // to open cinnarios://, and iOS can launch the other Unity app that registered it.
            _page.SetURLPattern(string.Empty, string.Empty, "^cinnarios(-ar)?:");
            _page.SetMargins(0, 0, 0, 0);
            _page.LoadURL(_pageAddress);

            string openedWith = Application.absoluteURL;
            if (_openingCamera || TryLink.TryParse(openedWith, out _, out _))
            {
                _page.SetVisibility(false);
                _showingShop = false;
                if (!_openingCamera)
                {
                    SetCameraActive(true);
                    loader.OpenTryLink(openedWith);
                }

                return;
            }

            _page.SetVisibility(true);
            _showingShop = true;
        }

        private void OnEnable()
        {
            Application.deepLinkActivated += OnPageMessage;
        }

        private void OnDisable()
        {
            Application.deepLinkActivated -= OnPageMessage;
        }

        private void OnDestroy()
        {
            if (_page != null)
            {
                _page.SetVisibility(false);
            }
        }

        private void OnPageMessage(string message)
        {
            if (!TryLink.TryParse(message, out _, out _))
            {
                Debug.Log($"Shop page said: {message}", this);
                return;
            }

            _openingCamera = true;

            // The page stops first. The camera starts only after that.
            if (_page != null)
            {
                _page.Pause();
                _page.SetInteractionEnabled(false);
                _page.SetVisibility(false);
            }

            _showingShop = false;
            SetCameraActive(true);
            loader.OpenTryLink(message);
        }

        /// <summary>The person finished the wall session. The shop comes back with the same page.</summary>
        public void EndTheSession()
        {
            if (!_showingShop)
            {
                ReturnToShop();
            }
        }

        private void ReturnToShop()
        {
            SetCameraActive(false);
            _page.SetInteractionEnabled(true);
            _page.Resume();
            _page.SetVisibility(true);
            _showingShop = true;
            TellTheShopAboutTheTry();
        }

        // The page asks whether the painting still feels uncertain, and keeps the tries.
        private void TellTheShopAboutTheTry()
        {
            if (loader == null || loader.CurrentArtwork == null)
            {
                return;
            }

            string tried = string.Join(",", System.Linq.Enumerable.Select(loader.TriedSourceIds, id => $"\"{id}\""));
            string outcome = string.IsNullOrEmpty(WallVisitRecord.Outcome) ? "" : WallVisitRecord.Outcome;
            string image = string.IsNullOrEmpty(WallVisitRecord.ImageFile) ? "" : WallVisitRecord.ImageFile;
            _page.EvaluateJS(
                "if(window.Rooms&&window.Rooms.noteWallTry)window.Rooms.noteWallTry({current:\""
                + loader.CurrentArtwork.SourceId
                + "\",tried:[" + tried + "],outcome:\"" + outcome + "\",image:\"" + image + "\"});");
            WallVisitRecord.Clear();
        }

#endif

        private void SetCameraActive(bool active)
        {
            if (cameraParts == null)
            {
                return;
            }

            foreach (GameObject part in cameraParts)
            {
                if (part != null)
                {
                    part.SetActive(active);
                }
            }
        }

#if !CINNARIOS_WEBVIEW
        public void EndTheSession()
        {
        }
#endif
    }
}
