using System;
using System.Collections.Generic;
using UnityEngine;

namespace Capstone.App.Data
{
    /// <summary>
    /// The work, size, and frame chosen for the wall, plus the works saved during this visit.
    /// Views write this asset. The wall reads it. Neither side looks the other up.
    /// Saved identifiers are also kept on the device, so the hearts return after the app closes.
    /// </summary>
    [CreateAssetMenu(fileName = "ArtworkSession", menuName = "Capstone/Artwork Session")]
    public sealed class ArtworkSessionState : ScriptableObject
    {
        public const string SavedArtworkIdsKey = "Capstone_SavedArtworkIds";

        [SerializeField] ArtworkData selectedArtwork;
        [SerializeField] int sizeIndex;
        [SerializeField] FrameOption selectedFrame;
        [SerializeField] List<ArtworkData> shortlist = new List<ArtworkData>();
        bool restoring;

        /// <summary>Raised after the selection or the shortlist changes.</summary>
        public event Action Changed;

        public ArtworkData SelectedArtwork => selectedArtwork;
        public int SizeIndex => sizeIndex;
        public FrameOption SelectedFrame => selectedFrame;
        public IReadOnlyList<ArtworkData> Shortlist => shortlist;

        public void Select(ArtworkData artwork, int index, FrameOption frame)
        {
            selectedArtwork = artwork;
            sizeIndex = index < 0 ? 0 : index;
            selectedFrame = frame;
            Changed?.Invoke();
        }

        public bool IsShortlisted(ArtworkData artwork)
        {
            return IndexOf(artwork) >= 0;
        }

        public void SetShortlisted(ArtworkData artwork, bool include)
        {
            if (artwork == null)
                return;

            int index = IndexOf(artwork);
            if (include && index < 0)
                shortlist.Add(artwork);
            else if (!include && index >= 0)
                shortlist.RemoveAt(index);
            else
                return;

            Remember();
            Changed?.Invoke();
        }

        void OnEnable()
        {
            if (!Application.isPlaying || restoring)
                return;

            restoring = true;
            CatalogueShelf shelf = Resources.Load<CatalogueShelf>("CatalogueShelf");
            restoring = false;
            if (shelf != null)
                RestoreSaved(shelf.Works);
        }

        /// <summary>
        /// Puts the saved identifiers back into the shortlist by matching them to the stored catalogue.
        /// </summary>
        public void RestoreSaved(IReadOnlyList<ArtworkData> catalogue)
        {
            if (!Application.isPlaying || catalogue == null || catalogue.Count == 0)
                return;

            string[] savedIds = ReadIds();
            if (shortlist == null)
                shortlist = new List<ArtworkData>();

            var restored = new List<ArtworkData>();
            for (int i = 0; i < savedIds.Length; i++)
            {
                ArtworkData match = Find(catalogue, savedIds[i]);
                if (match != null && IndexOf(match, restored) < 0)
                    restored.Add(match);
            }

            if (savedIds.Length > 0 && restored.Count == 0)
                return;

            shortlist.Clear();
            for (int i = 0; i < restored.Count; i++)
                shortlist.Add(restored[i]);

            Remember();
            Changed?.Invoke();
        }

        void Remember()
        {
            if (!Application.isPlaying || shortlist == null)
                return;

            var ids = new List<string>();
            for (int i = 0; i < shortlist.Count; i++)
            {
                ArtworkData work = shortlist[i];
                if (work == null || string.IsNullOrEmpty(work.Identifier))
                    continue;
                ids.Add(work.Identifier);
            }

            PlayerPrefs.SetString(SavedArtworkIdsKey, string.Join(",", ids));
            PlayerPrefs.Save();
        }

        static string[] ReadIds()
        {
            string stored = PlayerPrefs.GetString(SavedArtworkIdsKey, "");
            if (string.IsNullOrWhiteSpace(stored))
                return new string[0];

            string[] parts = stored.Split(',');
            var ids = new List<string>();
            for (int i = 0; i < parts.Length; i++)
            {
                string id = parts[i] == null ? "" : parts[i].Trim();
                if (id.Length > 0)
                    ids.Add(id);
            }

            return ids.ToArray();
        }

        static ArtworkData Find(IReadOnlyList<ArtworkData> catalogue, string identifier)
        {
            for (int i = 0; i < catalogue.Count; i++)
            {
                ArtworkData work = catalogue[i];
                if (work != null && string.Equals(work.Identifier, identifier, StringComparison.Ordinal))
                    return work;
            }

            return null;
        }

        static int IndexOf(ArtworkData artwork, List<ArtworkData> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                ArtworkData saved = list[i];
                if (saved != null && string.Equals(saved.Identifier, artwork.Identifier, StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }

        int IndexOf(ArtworkData artwork)
        {
            if (artwork == null || shortlist == null)
                return -1;

            for (int i = 0; i < shortlist.Count; i++)
            {
                ArtworkData saved = shortlist[i];
                if (saved != null && string.Equals(saved.Identifier, artwork.Identifier, StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }
    }
}
