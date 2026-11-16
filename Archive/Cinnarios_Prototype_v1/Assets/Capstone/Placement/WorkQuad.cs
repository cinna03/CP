using Capstone.Catalogue;
using UnityEngine;

namespace Capstone.Placement
{
    /// <summary>
    /// Sets a quad to the work's stored metres. The editor wall and the phone raycast both use this.
    /// </summary>
    public static class WorkQuad
    {
        public static void Fit(Transform quad, Artwork work)
        {
            if (quad == null || work == null)
                return;

            quad.localScale = new Vector3(work.WidthMetres, work.HeightMetres, 1f);
        }
    }
}
