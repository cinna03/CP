using System;
using System.Collections.Generic;
using Capstone.App.Data;
using UnityEngine;

namespace Capstone.App.Services
{
    /// <summary>
    /// The numbers and the recent works the studio card shows. The menu only draws them.
    /// </summary>
    public sealed class StudioDigest
    {
        public int Explored;
        public int Hearts;
        public int Photos;
        public readonly List<StudioBar> Bars = new List<StudioBar>();
        public readonly List<ArtworkData> Recent = new List<ArtworkData>();
        public string FavoriteLine = "";
        public Sprite FavoriteThumb;
        public string GalleryLine = "";

        public static StudioDigest Read(ArtworkSessionState session, IReadOnlyList<WallSnapshotRecord> photos)
        {
            var digest = new StudioDigest();
            var seen = new HashSet<string>();
            var weights = new Dictionary<string, float>();

            if (session != null)
            {
                digest.Hearts = session.Shortlist != null ? session.Shortlist.Count : 0;
                if (session.Tried != null)
                {
                    for (int i = 0; i < session.Tried.Count; i++)
                    {
                        TriedPiece piece = session.Tried[i];
                        if (piece == null || piece.Work == null)
                            continue;
                        if (seen.Add(piece.Work.Identifier))
                            digest.Explored++;
                        Note(weights, piece.Work);
                    }

                    for (int i = session.Tried.Count - 1; i >= 0 && digest.Recent.Count < 3; i--)
                    {
                        TriedPiece piece = session.Tried[i];
                        if (piece == null || piece.Work == null || Listed(digest.Recent, piece.Work))
                            continue;
                        digest.Recent.Add(piece.Work);
                    }
                }

                if (session.Shortlist != null)
                {
                    for (int i = 0; i < session.Shortlist.Count; i++)
                        Note(weights, session.Shortlist[i]);
                    if (session.Shortlist.Count > 0)
                    {
                        ArtworkData last = session.Shortlist[session.Shortlist.Count - 1];
                        if (last != null)
                        {
                            digest.FavoriteLine = last.Title;
                            digest.FavoriteThumb = last.ArtworkSprite;
                        }
                    }

                    for (int i = session.Shortlist.Count - 1; i >= 0 && digest.Recent.Count < 3; i--)
                    {
                        ArtworkData work = session.Shortlist[i];
                        if (work != null && !Listed(digest.Recent, work))
                            digest.Recent.Add(work);
                    }
                }

                AddWeights(weights, session.Taste != null ? session.Taste.LikedStyles : null);
                AddWeights(weights, session.Taste != null ? session.Taste.LikedMoods : null);
                AddWeights(weights, session.Taste != null ? session.Taste.LikedColours : null);
                IncludeSaved(digest, seen, weights);
            }

            digest.Photos = photos != null ? photos.Count : 0;
            if (photos != null && photos.Count > 0 && photos[0] != null)
            {
                string title = string.IsNullOrEmpty(photos[0].ArtworkTitle) ? "Wall photo" : photos[0].ArtworkTitle;
                digest.GalleryLine = title + "\n" + photos[0].CapturedAt;
            }

            var ranked = new List<KeyValuePair<string, float>>(weights);
            ranked.Sort((a, b) => b.Value.CompareTo(a.Value));
            int bars = ranked.Count < 3 ? ranked.Count : 3;
            for (int i = 0; i < bars; i++)
                digest.Bars.Add(new StudioBar(ranked[i].Key, ranked[i].Value));

            return digest;
        }

        static void IncludeSaved(StudioDigest digest, HashSet<string> seen, Dictionary<string, float> weights)
        {
            string code = new UserProfileTracker().CurrentEntranceCode;
            LocalProfile saved = LocalProfileStore.Load(code);
            if (saved == null)
                return;

            CatalogueShelf shelf = Resources.Load<CatalogueShelf>("CatalogueShelf");
            IReadOnlyList<ArtworkData> works = shelf != null ? shelf.Works : null;
            if (saved.Explored != null)
            {
                for (int i = 0; i < saved.Explored.Count; i++)
                {
                    string id = saved.Explored[i];
                    if (string.IsNullOrEmpty(id) || !seen.Add(id))
                        continue;
                    digest.Explored++;
                }

                for (int i = saved.Explored.Count - 1; i >= 0 && digest.Recent.Count < 3; i--)
                {
                    ArtworkData work = Find(works, saved.Explored[i]);
                    if (work != null && !Listed(digest.Recent, work))
                        digest.Recent.Add(work);
                }
            }

            AddStored(weights, saved.KeptStyles);
            AddStored(weights, saved.KeptMoods);
            AddStored(weights, saved.KeptColours);
        }

        static ArtworkData Find(IReadOnlyList<ArtworkData> works, string identifier)
        {
            if (works == null || string.IsNullOrEmpty(identifier))
                return null;
            for (int i = 0; i < works.Count; i++)
            {
                ArtworkData work = works[i];
                if (work != null && string.Equals(work.Identifier, identifier, StringComparison.Ordinal))
                    return work;
            }

            return null;
        }

        static void AddStored(Dictionary<string, float> weights, List<StoredWeight> notes)
        {
            if (notes == null)
                return;
            for (int i = 0; i < notes.Count; i++)
            {
                StoredWeight note = notes[i];
                if (note == null)
                    continue;
                Add(weights, note.Tag, note.Weight);
            }
        }

        static void Note(Dictionary<string, float> weights, ArtworkData work)
        {
            if (work == null)
                return;
            Add(weights, work.Style, 1f);
            Add(weights, work.Mood, 1f);
            Add(weights, work.Colour, 1f);
        }

        static void AddWeights(Dictionary<string, float> weights, IReadOnlyDictionary<string, float> source)
        {
            if (source == null)
                return;
            foreach (KeyValuePair<string, float> pair in source)
                Add(weights, pair.Key, pair.Value);
        }

        static void Add(Dictionary<string, float> weights, string tag, float amount)
        {
            if (string.IsNullOrWhiteSpace(tag) || amount == 0f)
                return;
            float current;
            weights.TryGetValue(tag.Trim(), out current);
            weights[tag.Trim()] = current + amount;
        }

        static bool Listed(List<ArtworkData> works, ArtworkData work)
        {
            for (int i = 0; i < works.Count; i++)
            {
                if (works[i] != null && works[i].Identifier == work.Identifier)
                    return true;
            }

            return false;
        }
    }

    public readonly struct StudioBar
    {
        public StudioBar(string label, float weight)
        {
            Label = label ?? "";
            Weight = weight;
        }

        public string Label { get; }
        public float Weight { get; }
    }
}
