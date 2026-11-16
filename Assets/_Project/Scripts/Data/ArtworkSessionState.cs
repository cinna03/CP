using System;
using System.Collections.Generic;
using Capstone.App.Services;
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
        public static bool PersistOutsidePlay;

        static bool CanPersist
        {
            get { return Application.isPlaying || PersistOutsidePlay; }
        }

        [SerializeField] ArtworkData selectedArtwork;
        [SerializeField] int sizeIndex;
        [SerializeField] FrameOption selectedFrame;
        [SerializeField] List<ArtworkData> shortlist = new List<ArtworkData>();
        bool restoring;
        List<string> dismissed = new List<string>();
        List<KeptChoice> kept = new List<KeptChoice>();
        List<AskedReply> answers = new List<AskedReply>();
        List<TriedPiece> tried = new List<TriedPiece>();
        readonly UserTasteProfile taste = new UserTasteProfile();

        /// <summary>Raised after the selection or the shortlist changes.</summary>
        public event Action Changed;

        public ArtworkData SelectedArtwork => selectedArtwork;
        public int SizeIndex => sizeIndex;
        public FrameOption SelectedFrame => selectedFrame;
        public IReadOnlyList<ArtworkData> Shortlist => shortlist;
        public bool HasDecision { get; private set; }
        public VisitDecision LastDecision { get; private set; }

        /// <summary>Keeps the latest wall decision for this visit. The recommendation weights stay in the engine.</summary>
        public void RecordDecision(VisitDecision decision)
        {
            LastDecision = decision;
            HasDecision = true;
        }

        public IReadOnlyList<string> DismissedIdentifiers => dismissed;

        public IReadOnlyList<KeptChoice> Kept => kept;

        /// <summary>Remembers a work the buyer set aside during this visit. It is not suggested again until the next visit.</summary>
        public void NoteDismissed(ArtworkData artwork)
        {
            if (artwork == null || string.IsNullOrEmpty(artwork.Identifier))
                return;
            if (dismissed == null)
                dismissed = new List<string>();
            if (!Listed(dismissed, artwork.Identifier))
                dismissed.Add(artwork.Identifier);
        }

        public bool WasDismissed(ArtworkData artwork)
        {
            return artwork != null && dismissed != null && Listed(dismissed, artwork.Identifier);
        }

        /// <summary>Stores the work on the wall, with the size and frame the buyer had chosen.</summary>
        public void RememberKept(ArtworkData artwork, int index, FrameOption frame)
        {
            if (artwork == null)
                return;
            if (kept == null)
                kept = new List<KeptChoice>();

            for (int i = 0; i < kept.Count; i++)
            {
                if (kept[i] != null && kept[i].Work != null && string.Equals(kept[i].Work.Identifier, artwork.Identifier, StringComparison.Ordinal))
                {
                    kept[i] = new KeptChoice(artwork, index, frame);
                    return;
                }
            }

            kept.Add(new KeptChoice(artwork, index, frame));
        }

        /// <summary>Clears this visit. Saved favorites and gallery photos stay on the device.</summary>
        public void ClearVisit()
        {
            if (tried != null)
                tried.Clear();
            if (kept != null)
                kept.Clear();
            if (answers != null)
                answers.Clear();
            if (dismissed != null)
                dismissed.Clear();
            if (taste != null)
                taste.Clear();
            if (CanPersist)
                LocalProfileStore.CommitVisit(new UserProfileTracker().CurrentEntranceCode);
            HasDecision = false;
            Changed?.Invoke();
        }

        /// <summary>Drops the in-memory visit so the next entrance code starts clean. Saved hearts stay in their own key.</summary>
        public void ReleaseForNextUser()
        {
            selectedArtwork = null;
            sizeIndex = 0;
            selectedFrame = null;
            if (shortlist != null)
                shortlist.Clear();
            ClearVisit();
        }

        static string FavoritesKey()
        {
            string code = new UserProfileTracker().CurrentEntranceCode;
            if (string.IsNullOrEmpty(code))
                return "";
            return "Favorites_" + code;
        }

        public IReadOnlyList<AskedReply> Answers => answers;
        public IReadOnlyList<TriedPiece> Tried => tried;
        public UserTasteProfile Taste => taste;

        /// <summary>Keeps one question and its reply for this visit, so the wall can show them again.</summary>
        public void RememberAnswer(string title, string question, string reply)
        {
            if (answers == null)
                answers = new List<AskedReply>();
            answers.Add(new AskedReply(title ?? "", question ?? "", reply ?? ""));
        }

        /// <summary>Records a work placed on the wall. A later Keep, Save, or Dismiss replaces Viewing.</summary>
        public void NoteTried(ArtworkData artwork, int sizeIndex, FrameOption frame, string status)
        {
            if (artwork == null || string.IsNullOrEmpty(artwork.Identifier))
                return;
            if (CanPersist)
                LocalProfileStore.RememberExplored(new UserProfileTracker().CurrentEntranceCode, artwork.Identifier);
            if (tried == null)
                tried = new List<TriedPiece>();

            string next = string.IsNullOrWhiteSpace(status) ? "Viewing" : status.Trim();
            for (int i = 0; i < tried.Count; i++)
            {
                TriedPiece piece = tried[i];
                if (piece == null || piece.Work == null)
                    continue;
                if (!string.Equals(piece.Work.Identifier, artwork.Identifier, StringComparison.Ordinal))
                    continue;

                piece.Work = artwork;
                piece.SizeIndex = sizeIndex < 0 ? 0 : sizeIndex;
                piece.Frame = frame;
                if (next != "Viewing")
                    piece.Status = next;
                return;
            }

            tried.Add(new TriedPiece
            {
                Work = artwork,
                SizeIndex = sizeIndex < 0 ? 0 : sizeIndex,
                Frame = frame,
                Status = next
            });
        }

        public void Select(ArtworkData artwork, int index, FrameOption frame)
        {
            selectedArtwork = artwork;
            sizeIndex = index < 0 ? 0 : index;
            selectedFrame = frame;
            NoteTried(artwork, sizeIndex, frame, "Viewing");
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
            if (!CanPersist || catalogue == null || catalogue.Count == 0)
                return;

            if (FavoritesKey().Length == 0)
            {
                if (shortlist != null)
                    shortlist.Clear();
                Changed?.Invoke();
                return;
            }

            string[] savedIds = ReadIds();
            LocalProfile saved = CanPersist ? LocalProfileStore.Load(new UserProfileTracker().CurrentEntranceCode) : null;
            if (saved != null && saved.Favorites != null && saved.Favorites.Count > 0)
                savedIds = saved.Favorites.ToArray();
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
            {
                ApplyStoredVisit(saved);
                return;
            }

            shortlist.Clear();
            for (int i = 0; i < restored.Count; i++)
                shortlist.Add(restored[i]);

            Remember();
            ApplyStoredVisit(saved);
            Changed?.Invoke();
        }

        void ApplyStoredVisit(LocalProfile saved)
        {
            if (saved == null || taste == null)
                return;
            taste.ApplyVisit(saved.VisitRoom, saved.VisitLighting, saved.VisitFurniture, saved.VisitStyles, saved.VisitMoods, saved.VisitColours);
        }

        public void FlushSaved()
        {
            Remember();
        }

        void Remember()
        {
            if (!CanPersist || shortlist == null || FavoritesKey().Length == 0)
                return;

            try
            {
                var ids = new List<string>();
                for (int i = 0; i < shortlist.Count; i++)
                {
                    ArtworkData work = shortlist[i];
                    if (work == null || string.IsNullOrEmpty(work.Identifier))
                        continue;
                    ids.Add(work.Identifier);
                }

                PlayerPrefs.SetString(FavoritesKey(), string.Join(",", ids));
                PlayerPrefs.Save();
                LocalProfileStore.ReplaceFavorites(new UserProfileTracker().CurrentEntranceCode, ids);
            }
            catch (Exception error)
            {
                Debug.LogWarning("Saved works could not be written. " + error.Message);
            }
        }

        static string[] ReadIds()
        {
            string key = FavoritesKey();
            if (key.Length == 0)
                return new string[0];

            try
            {
                string stored = PlayerPrefs.GetString(key, "");
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
            catch (Exception error)
            {
                Debug.LogWarning("Saved works could not be read. " + error.Message);
                return new string[0];
            }
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

        static bool Listed(List<string> ids, string identifier)
        {
            for (int i = 0; i < ids.Count; i++)
            {
                if (string.Equals(ids[i], identifier, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }

    /// <summary>One finalist from this visit: the work, the print, and the frame.</summary>
    public sealed class KeptChoice
    {
        public KeptChoice(ArtworkData work, int sizeIndex, FrameOption frame)
        {
            Work = work;
            SizeIndex = sizeIndex < 0 ? 0 : sizeIndex;
            Frame = frame;
        }

        public ArtworkData Work { get; }
        public int SizeIndex { get; }
        public FrameOption Frame { get; }
    }

    /// <summary>One question asked on the wall and the reply that was shown.</summary>
    public sealed class AskedReply
    {
        public AskedReply(string title, string question, string reply)
        {
            Title = title ?? "";
            Question = question ?? "";
            Reply = reply ?? "";
        }

        public string Title { get; }
        public string Question { get; }
        public string Reply { get; }
    }

    /// <summary>One work that was placed on the wall during this visit.</summary>
    public sealed class TriedPiece
    {
        public ArtworkData Work;
        public int SizeIndex;
        public FrameOption Frame;
        public string Status = "Viewing";
    }
}
