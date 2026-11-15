using System.Collections.Generic;
using Capstone.App.Data;
using Capstone.App.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Capstone.App.UI
{
    public enum OrientationFilter
    {
        Any,
        Portrait,
        Landscape,
        Square
    }

    public enum SizeTierFilter
    {
        Any,
        Small,
        Medium,
        Large
    }

    /// <summary>
    /// Builds the catalogue grid from the filter service. Pill buttons call the Use methods.
    /// The grid object is expected to carry a layout group. This script does not search the scene for one.
    /// </summary>
    public sealed class CatalogueViewController : MonoBehaviour
    {
        [SerializeField] List<ArtworkData> artworks = new List<ArtworkData>();
        [SerializeField] ArtworkSessionState session;
        [SerializeField] ArtworkCardView cardPrefab;
        [SerializeField] RectTransform grid;
        [SerializeField] TMP_InputField searchField;
        [SerializeField] Button clearSearchButton;
        [SerializeField] string search = "";
        [SerializeField] string style = "";
        bool favoritesOnly;
        [SerializeField] string mood = "";
        [SerializeField] string colour = "";
        [SerializeField] OrientationFilter orientation = OrientationFilter.Any;
        [SerializeField] SizeTierFilter sizeTier = SizeTierFilter.Any;
        [SerializeReference] ICatalogueFilterService filter;
        [SerializeField] ArtworkEvent artworkChosen = new ArtworkEvent();

        readonly List<ArtworkCardView> spawned = new List<ArtworkCardView>();

        public UnityEngine.Events.UnityEvent<ArtworkData> ArtworkChosen => artworkChosen;

        void Awake()
        {
            if (filter == null)
                filter = new CatalogueFilterService();
            if (searchField != null)
                searchField.onValueChanged.AddListener(SetSearch);
            if (clearSearchButton != null)
                clearSearchButton.onClick.AddListener(ClearSearch);
        }

        public void OpenFavoritesMode()
        {
            favoritesOnly = true;
            Rebuild();
        }

        public void SetSearch(string value)
        {
            search = value ?? "";
            if (clearSearchButton != null)
                clearSearchButton.gameObject.SetActive(!string.IsNullOrEmpty(search));
            Rebuild();
        }

        public void ClearSearch()
        {
            search = "";
            if (searchField != null)
                searchField.SetTextWithoutNotify("");
            if (clearSearchButton != null)
                clearSearchButton.gameObject.SetActive(false);
            Rebuild();
        }

        void OnEnable()
        {
            if (session != null)
                session.Changed += Rebuild;
            Rebuild();
        }

        void OnDisable()
        {
            if (session != null)
                session.Changed -= Rebuild;
        }

        public void UseStyle(string value)
        {
            value = value ?? "";
            style = style == value ? "" : value;
            Rebuild();
        }

        public void UseMood(string value)
        {
            value = value ?? "";
            mood = mood == value ? "" : value;
            Rebuild();
        }

        public void UseColour(string value)
        {
            value = value ?? "";
            colour = colour == value ? "" : value;
            Rebuild();
        }

        public void UsePortrait()
        {
            orientation = orientation == OrientationFilter.Portrait ? OrientationFilter.Any : OrientationFilter.Portrait;
            Rebuild();
        }

        public void UseLandscape()
        {
            orientation = orientation == OrientationFilter.Landscape ? OrientationFilter.Any : OrientationFilter.Landscape;
            Rebuild();
        }

        public void UseSquare()
        {
            orientation = orientation == OrientationFilter.Square ? OrientationFilter.Any : OrientationFilter.Square;
            Rebuild();
        }

        public void UseAnyOrientation()
        {
            orientation = OrientationFilter.Any;
            Rebuild();
        }

        public void UseSmall()
        {
            sizeTier = sizeTier == SizeTierFilter.Small ? SizeTierFilter.Any : SizeTierFilter.Small;
            Rebuild();
        }

        public void UseMedium()
        {
            sizeTier = sizeTier == SizeTierFilter.Medium ? SizeTierFilter.Any : SizeTierFilter.Medium;
            Rebuild();
        }

        public void UseLarge()
        {
            sizeTier = sizeTier == SizeTierFilter.Large ? SizeTierFilter.Any : SizeTierFilter.Large;
            Rebuild();
        }

        public void UseAnySize()
        {
            sizeTier = SizeTierFilter.Any;
            Rebuild();
        }

        public void ClearFilters()
        {
            style = "";
            mood = "";
            colour = "";
            search = "";
            if (searchField != null)
                searchField.SetTextWithoutNotify("");
            if (clearSearchButton != null)
                clearSearchButton.gameObject.SetActive(false);
            orientation = OrientationFilter.Any;
            sizeTier = SizeTierFilter.Any;
            Rebuild();
        }

        public void Rebuild()
        {
            ClearSpawned();
            if (cardPrefab == null || grid == null || filter == null)
                return;

            CatalogueQuery query = new CatalogueQuery
            {
                Style = style,
                Mood = mood,
                Colour = colour,
                Search = search,
                Orientation = ToOrientation(orientation),
                SizeTier = ToSizeTier(sizeTier)
            };

            IReadOnlyList<ArtworkData> source = favoritesOnly && session != null ? session.Shortlist : artworks;
            IReadOnlyList<ArtworkData> shown = filter.Matching(source, query);
            for (int i = 0; i < shown.Count; i++)
            {
                ArtworkData work = shown[i];
                ArtworkCardView card = Instantiate(cardPrefab, grid);
                card.gameObject.SetActive(true);
                bool saved = session != null && session.IsShortlisted(work);
                card.Bind(work, saved, delegate { Choose(work); }, delegate { Toggle(work); });
                spawned.Add(card);
            }
        }

        void Choose(ArtworkData work)
        {
            artworkChosen.Invoke(work);
        }

        void Toggle(ArtworkData work)
        {
            if (session == null || work == null)
                return;

            session.SetShortlisted(work, !session.IsShortlisted(work));
        }

        void ClearSpawned()
        {
            for (int i = spawned.Count - 1; i >= 0; i--)
            {
                if (spawned[i] != null)
                    Destroy(spawned[i].gameObject);
            }

            spawned.Clear();
        }

        static Orientation? ToOrientation(OrientationFilter value)
        {
            switch (value)
            {
                case OrientationFilter.Portrait:
                    return Orientation.Portrait;
                case OrientationFilter.Landscape:
                    return Orientation.Landscape;
                case OrientationFilter.Square:
                    return Orientation.Square;
                default:
                    return null;
            }
        }

        static SizeTier? ToSizeTier(SizeTierFilter value)
        {
            switch (value)
            {
                case SizeTierFilter.Small:
                    return SizeTier.Small;
                case SizeTierFilter.Medium:
                    return SizeTier.Medium;
                case SizeTierFilter.Large:
                    return SizeTier.Large;
                default:
                    return null;
            }
        }
    }
}
