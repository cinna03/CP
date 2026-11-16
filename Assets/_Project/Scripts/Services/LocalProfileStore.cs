using System;
using System.Collections.Generic;
using System.IO;
using Capstone.App.Data;
using UnityEngine;

namespace Capstone.App.Services
{
    /// <summary>
    /// One JSON file per entrance code in Application.persistentDataPath/UserProfiles.
    /// Favorites are also kept in PlayerPrefs so the existing per-code keys still work.
    /// </summary>
    public static class LocalProfileStore
    {
        const int CurrentSchema = 1;

        public static void ReplaceFavorites(string code, IList<string> identifiers)
        {
            if (!CanWrite(code))
                return;

            LocalProfile profile = LoadOrCreate(code);
            profile.Favorites = Copy(identifiers);
            Write(profile);
        }

        public static void RememberExplored(string code, string identifier)
        {
            if (!CanWrite(code) || string.IsNullOrWhiteSpace(identifier))
                return;

            LocalProfile profile = LoadOrCreate(code);
            string id = identifier.Trim();
            if (!Listed(profile.Explored, id))
                profile.Explored.Add(id);
            Write(profile);
        }

        public static void AddTaste(string code, string style, string mood, string colour, float change)
        {
            if (!CanWrite(code) || change == 0f)
                return;

            LocalProfile profile = LoadOrCreate(code);
            Add(profile.VisitStyles, style, change);
            Add(profile.VisitMoods, mood, change);
            Add(profile.VisitColours, colour, change);
            Write(profile);
        }

        public static void RememberPlace(string code, string room, string lighting, string furniture)
        {
            if (!CanWrite(code))
                return;

            LocalProfile profile = LoadOrCreate(code);
            if (profile.VisitRoom.Length == 0 && !string.IsNullOrWhiteSpace(room))
                profile.VisitRoom = room.Trim();
            if (profile.VisitLighting.Length == 0 && !string.IsNullOrWhiteSpace(lighting))
                profile.VisitLighting = lighting.Trim();
            if (profile.VisitFurniture.Length == 0 && !string.IsNullOrWhiteSpace(furniture))
                profile.VisitFurniture = furniture.Trim();
            Write(profile);
        }

        /// <summary>Folds the open visit into the kept history, then clears the open visit.</summary>
        public static void CommitVisit(string code)
        {
            if (!CanWrite(code))
                return;

            LocalProfile profile = LoadOrCreate(code);
            Fold(profile.KeptStyles, profile.VisitStyles);
            Fold(profile.KeptMoods, profile.VisitMoods);
            Fold(profile.KeptColours, profile.VisitColours);
            profile.VisitStyles.Clear();
            profile.VisitMoods.Clear();
            profile.VisitColours.Clear();
            profile.VisitRoom = "";
            profile.VisitLighting = "";
            profile.VisitFurniture = "";
            Write(profile);
        }

        public static LocalProfile Load(string code)
        {
            string normalized = UserProfileTracker.Normalize(code);
            if (normalized.Length == 0)
                return null;

            string path = PathFor(normalized);
            LocalProfile profile = Read(path);
            if (profile != null)
                return profile;
            return Read(path + ".bak");
        }

        static bool CanWrite(string code)
        {
            return UserProfileTracker.Normalize(code).Length > 0
                && (Application.isPlaying || ArtworkSessionState.PersistOutsidePlay);
        }

        static LocalProfile LoadOrCreate(string code)
        {
            string normalized = UserProfileTracker.Normalize(code);
            LocalProfile profile = Load(normalized);
            if (profile == null)
                profile = new LocalProfile();
            profile.SchemaVersion = CurrentSchema;
            profile.EntranceCode = normalized;
            Prepare(profile);
            return profile;
        }

        static void Write(LocalProfile profile)
        {
            if (profile == null)
                return;

            try
            {
                string folder = Folder();
                Directory.CreateDirectory(folder);
                string path = Path.Combine(folder, profile.EntranceCode + "_profile.json");
                string temp = path + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(profile, true));
                string backup = path + ".bak";
                if (File.Exists(path))
                {
                    if (File.Exists(backup))
                        File.Delete(backup);
                    File.Replace(temp, path, backup);
                }
                else
                {
                    File.Move(temp, path);
                }
            }
            catch (Exception error)
            {
                Debug.LogWarning("The local profile could not be saved. " + error.Message);
            }
        }

        static LocalProfile Read(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;

            try
            {
                LocalProfile profile = JsonUtility.FromJson<LocalProfile>(File.ReadAllText(path));
                if (profile == null)
                    return null;
                Prepare(profile);
                if (profile.SchemaVersion < CurrentSchema)
                    profile.SchemaVersion = CurrentSchema;
                return profile;
            }
            catch (Exception error)
            {
                Debug.LogWarning("A local profile could not be read. " + error.Message);
                return null;
            }
        }

        static void Prepare(LocalProfile profile)
        {
            if (profile.Favorites == null)
                profile.Favorites = new List<string>();
            if (profile.Explored == null)
                profile.Explored = new List<string>();
            if (profile.VisitRoom == null)
                profile.VisitRoom = "";
            if (profile.VisitLighting == null)
                profile.VisitLighting = "";
            if (profile.VisitFurniture == null)
                profile.VisitFurniture = "";
            if (profile.KeptStyles == null)
                profile.KeptStyles = new List<StoredWeight>();
            if (profile.KeptMoods == null)
                profile.KeptMoods = new List<StoredWeight>();
            if (profile.KeptColours == null)
                profile.KeptColours = new List<StoredWeight>();
            if (profile.VisitStyles == null)
                profile.VisitStyles = new List<StoredWeight>();
            if (profile.VisitMoods == null)
                profile.VisitMoods = new List<StoredWeight>();
            if (profile.VisitColours == null)
                profile.VisitColours = new List<StoredWeight>();
        }

        static string PathFor(string code)
        {
            return Path.Combine(Folder(), code + "_profile.json");
        }

        static string Folder()
        {
            string root = string.IsNullOrEmpty(UserProfileTracker.DirectoryOverride)
                ? Application.persistentDataPath
                : UserProfileTracker.DirectoryOverride;
            return Path.Combine(root, "UserProfiles");
        }

        static List<string> Copy(IList<string> identifiers)
        {
            var copy = new List<string>();
            if (identifiers == null)
                return copy;
            for (int i = 0; i < identifiers.Count; i++)
            {
                string id = identifiers[i] == null ? "" : identifiers[i].Trim();
                if (id.Length > 0 && !Listed(copy, id))
                    copy.Add(id);
            }

            return copy;
        }

        static bool Listed(List<string> ids, string id)
        {
            for (int i = 0; i < ids.Count; i++)
            {
                if (string.Equals(ids[i], id, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        static void Add(List<StoredWeight> weights, string tag, float change)
        {
            if (weights == null || string.IsNullOrWhiteSpace(tag) || change == 0f)
                return;

            string name = tag.Trim();
            for (int i = 0; i < weights.Count; i++)
            {
                StoredWeight note = weights[i];
                if (note == null || !string.Equals(note.Tag, name, StringComparison.OrdinalIgnoreCase))
                    continue;
                note.Weight += change;
                return;
            }

            weights.Add(new StoredWeight { Tag = name, Weight = change });
        }

        static void Fold(List<StoredWeight> kept, List<StoredWeight> visit)
        {
            if (kept == null || visit == null)
                return;
            for (int i = 0; i < visit.Count; i++)
            {
                StoredWeight note = visit[i];
                if (note == null)
                    continue;
                Add(kept, note.Tag, note.Weight);
            }
        }
    }

    [Serializable]
    public sealed class LocalProfile
    {
        public int SchemaVersion = 1;
        public string EntranceCode = "";
        public List<string> Favorites = new List<string>();
        public List<string> Explored = new List<string>();
        public string VisitRoom = "";
        public string VisitLighting = "";
        public string VisitFurniture = "";
        public List<StoredWeight> KeptStyles = new List<StoredWeight>();
        public List<StoredWeight> KeptMoods = new List<StoredWeight>();
        public List<StoredWeight> KeptColours = new List<StoredWeight>();
        public List<StoredWeight> VisitStyles = new List<StoredWeight>();
        public List<StoredWeight> VisitMoods = new List<StoredWeight>();
        public List<StoredWeight> VisitColours = new List<StoredWeight>();
    }

    [Serializable]
    public sealed class StoredWeight
    {
        public string Tag = "";
        public float Weight;
    }
}
