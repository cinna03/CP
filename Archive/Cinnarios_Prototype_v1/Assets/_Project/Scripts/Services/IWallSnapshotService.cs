using System;
using System.Collections.Generic;
using Capstone.App.Data;
using UnityEngine;

namespace Capstone.App.Services
{
    /// <summary>
    /// One saved try-on photo. The image file lives beside the manifest, under WallGallery.
    /// </summary>
    [Serializable]
    public sealed class WallSnapshotRecord
    {
        public string SnapshotId = "";
        public string ImageFileName = "";
        public string ArtworkIdentifier = "";
        public string ArtworkTitle = "";
        public string DimensionsLabel = "";
        public int SizeIndex;
        public string FrameName = "";
        public string CapturedAt = "";
    }

    /// <summary>
    /// Saves and reloads wall photos on this device. The gallery page only asks for records and textures.
    /// </summary>
    public interface IWallSnapshotService
    {
        WallSnapshotRecord SaveSnapshot(Texture2D screenshot, ArtworkData artwork, int sizeIndex, string frameName);
        IReadOnlyList<WallSnapshotRecord> GetAllSnapshots();
        Texture2D LoadSnapshotTexture(WallSnapshotRecord record);
        bool DeleteSnapshot(string snapshotId);
    }
}
