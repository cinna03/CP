using System.Collections.Generic;
using UnityEngine;

namespace Capstone.App.Data
{
    /// <summary>
    /// The artwork list the catalogue page reads. It lives in Resources so Play can find it without a scene reference.
    /// </summary>
    [CreateAssetMenu(fileName = "CatalogueShelf", menuName = "Capstone/Catalogue Shelf")]
    public sealed class CatalogueShelf : ScriptableObject
    {
        [SerializeField] List<ArtworkData> works = new List<ArtworkData>();
        [SerializeField] UIThemeData theme;
        [SerializeField] ArtworkSessionState session;
        [SerializeField] List<FrameOption> frames = new List<FrameOption>();

        public IReadOnlyList<ArtworkData> Works => works;
        public UIThemeData Theme => theme;
        public ArtworkSessionState Session => session;
        public IReadOnlyList<FrameOption> Frames => frames;
    }
}
