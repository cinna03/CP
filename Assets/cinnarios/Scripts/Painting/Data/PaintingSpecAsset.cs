using UnityEngine;

namespace Cinnarios.Painting.Data
{
    /// <summary>
    /// A painting described as a Unity asset, so test and demo artworks can be set up in the
    /// Editor (Create > Cinnarios > Painting) instead of typed into every scene. At runtime the
    /// Met catalogue will create PaintingSpec objects directly; this is for local artworks.
    /// </summary>
    [CreateAssetMenu(fileName = "NewPainting", menuName = "Cinnarios/Painting")]
    public sealed class PaintingSpecAsset : ScriptableObject
    {
        [SerializeField] private string sourceId = "local";
        [SerializeField] private string title = "Untitled";
        [SerializeField] private string artist;
        [SerializeField] private string date;
        [SerializeField, TextArea] private string creditLine;
        [SerializeField] private Texture2D image;

        [Header("Real size (cm)")]
        [SerializeField, Min(0.1f)] private float widthCm = 50f;
        [SerializeField, Min(0.1f)] private float heightCm = 70f;
        [Tooltip("Leave at 0 to use the default depth for the medium.")]
        [SerializeField, Min(0f)] private float depthCm;

        [SerializeField] private PaintingMedium medium = PaintingMedium.Canvas;
        [SerializeField] private SideTreatment sideTreatment = SideTreatment.PlainEdge;

        /// <summary>Turns the asset into the immutable description the rest of the system works with.</summary>
        public PaintingSpec ToSpec()
        {
            float depth = depthCm > 0f ? depthCm : medium.DefaultDepthCm();
            return new PaintingSpec(
                sourceId, title, artist, date, creditLine, image,
                new PaintingDimensions(widthCm, heightCm, depth), medium, sideTreatment);
        }
    }
}
