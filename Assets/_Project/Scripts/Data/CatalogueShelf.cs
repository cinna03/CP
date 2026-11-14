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

        public IReadOnlyList<ArtworkData> Works => works;
    }
}
