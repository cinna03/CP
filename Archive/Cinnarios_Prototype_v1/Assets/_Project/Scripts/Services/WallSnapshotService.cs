using System;
using System.Collections.Generic;
using System.IO;
using Capstone.App.Data;
using UnityEngine;

namespace Capstone.App.Services
{
    /// <summary>
    /// Writes try-on photos and a JSON manifest under Application.persistentDataPath/WallGallery.
    /// </summary>
    [Serializable]
    public sealed class WallSnapshotService : IWallSnapshotService
    {
        const string FolderName = "WallGallery";
        const string ManifestName = "snapshots.json";

        public WallSnapshotRecord SaveSnapshot(Texture2D screenshot, ArtworkData artwork, int sizeIndex, string frameName)
        {
            if (screenshot == null)
                throw new ArgumentNullException(nameof(screenshot));

            try
            {
                string folder = Folder();
                string id = Guid.NewGuid().ToString("N");
                string fileName = id + ".png";
                SafeFile.WriteAllBytes(Path.Combine(folder, fileName), ImageConversion.EncodeToPNG(screenshot));

            var record = new WallSnapshotRecord
            {
                SnapshotId = id,
                ImageFileName = fileName,
                ArtworkIdentifier = artwork != null ? artwork.Identifier : "",
                ArtworkTitle = artwork != null ? artwork.Title : "",
                DimensionsLabel = artwork != null ? artwork.FormatSizeLabel(sizeIndex) : "",
                SizeIndex = sizeIndex < 0 ? 0 : sizeIndex,
                FrameName = string.IsNullOrWhiteSpace(frameName) ? "Unframed" : frameName.Trim(),
                CapturedAt = DateTime.Now.ToString("d MMM yyyy, HH:mm")
            };

            SnapshotFile file = Read();
            file.Items.Insert(0, record);
            Write(file);
            return record;
            }
            catch (Exception error)
            {
                Debug.LogWarning("The wall photo could not be saved. " + error.Message);
                return null;
            }
        }

        public IReadOnlyList<WallSnapshotRecord> GetAllSnapshots()
        {
            SnapshotFile file = Read();
            return file.Items;
        }

        public Texture2D LoadSnapshotTexture(WallSnapshotRecord record)
        {
            if (record == null || string.IsNullOrEmpty(record.ImageFileName))
                return null;

            try
            {
                string path = Path.Combine(Folder(), record.ImageFileName);
                if (!File.Exists(path))
                    return null;

                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path)))
                {
                    UnityEngine.Object.Destroy(texture);
                    return null;
                }

                return texture;
            }
            catch (Exception error)
            {
                Debug.LogWarning("A saved wall photo could not be opened. " + error.Message);
                return null;
            }
        }

        public bool DeleteSnapshot(string snapshotId)
        {
            if (string.IsNullOrEmpty(snapshotId))
                return false;

            SnapshotFile file = Read();
            try
            {
            for (int i = 0; i < file.Items.Count; i++)
            {
                WallSnapshotRecord record = file.Items[i];
                if (record == null || record.SnapshotId != snapshotId)
                    continue;

                string path = Path.Combine(Folder(), record.ImageFileName ?? "");
                if (File.Exists(path))
                    File.Delete(path);
                file.Items.RemoveAt(i);
                Write(file);
                return true;
            }
            }
            catch (Exception error)
            {
                Debug.LogWarning("A wall photo could not be removed. " + error.Message);
            }

            return false;
        }

        static string Folder()
        {
            string code = new UserProfileTracker().CurrentEntranceCode;
            if (string.IsNullOrEmpty(code))
                code = "_none";
            string path = Path.Combine(Application.persistentDataPath, FolderName, code);
            Directory.CreateDirectory(path);
            return path;
        }

        static string ManifestPath()
        {
            return Path.Combine(Folder(), ManifestName);
        }

        static SnapshotFile Read()
        {
            try
            {
                string path = ManifestPath();
                if (!SafeFile.Exists(path))
                    return new SnapshotFile();

                SnapshotFile file = SafeFile.ReadJson<SnapshotFile>(path);
                if (file == null || file.Items == null)
                    return new SnapshotFile();
                return file;
            }
            catch (Exception error)
            {
                Debug.LogWarning("The gallery list could not be read. " + error.Message);
                return new SnapshotFile();
            }
        }

        static void Write(SnapshotFile file)
        {
            try
            {
                SafeFile.WriteAllText(ManifestPath(), JsonUtility.ToJson(file, true));
            }
            catch (Exception error)
            {
                Debug.LogWarning("The gallery list could not be saved. " + error.Message);
            }
        }

        [Serializable]
        sealed class SnapshotFile
        {
            public List<WallSnapshotRecord> Items = new List<WallSnapshotRecord>();
        }
    }
}
