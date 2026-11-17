using UnityEngine;

namespace Cinnarios.Placement
{
    /// <summary>
    /// The gallery hanging rule: the centre of a painting sits about 145 cm above the floor,
    /// roughly eye level. A painting too tall for that is raised so it stays clear of the floor.
    /// </summary>
    public sealed class HangingHeightRule
    {
        public const float GalleryCentreHeightMetres = 1.45f;
        private const float MinimumGapAboveFloorMetres = 0.05f;

        /// <returns>The world height for the painting's centre.</returns>
        public float CentreHeight(float floorHeight, float paintingHeightMetres)
        {
            float lowestAllowedCentre = paintingHeightMetres * 0.5f + MinimumGapAboveFloorMetres;
            return floorHeight + Mathf.Max(GalleryCentreHeightMetres, lowestAllowedCentre);
        }
    }
}
