using System;
using System.Collections.Generic;
using UnityEngine;

namespace Capstone.App.Data
{
    /// <summary>
    /// The work, size, and frame chosen for the wall, plus the works saved during this visit.
    /// Views write this asset. The wall reads it. Neither side looks the other up.
    /// </summary>
    [CreateAssetMenu(fileName = "ArtworkSession", menuName = "Capstone/Artwork Session")]
    public sealed class ArtworkSessionState : ScriptableObject
    {
        [SerializeField] ArtworkData selectedArtwork;
        [SerializeField] int sizeIndex;
        [SerializeField] FrameOption selectedFrame;
        [SerializeField] List<ArtworkData> shortlist = new List<ArtworkData>();

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

            Changed?.Invoke();
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
