using Cinnarios.Painting.Data;
using Cinnarios.Painting.Presentation;
using Cinnarios.Painting.Validation;
using UnityEngine;

namespace Cinnarios.Sandbox
{
    /// <summary>
    /// Test harness only, not part of the app: shows one painting asset in the Editor so the
    /// validation and geometry can be checked before AR.
    /// </summary>
    [RequireComponent(typeof(PaintingView))]
    public sealed class PaintingSandboxPreview : MonoBehaviour
    {
        [SerializeField] private PaintingSpecAsset painting;

        private void Start() => Rebuild();

        [ContextMenu("Rebuild Painting")]
        public void Rebuild()
        {
            if (painting == null)
            {
                Debug.LogWarning("PaintingSandboxPreview: assign a painting asset first.", this);
                return;
            }

            PaintingSpec recorded = painting.ToSpec();
            if (!PaintingPreparation.TryPrepare(recorded, out PaintingSpec ready, out ImageFitResult fit))
            {
                Debug.LogWarning($"Not built: {fit}", this);
                return;
            }

            GetComponent<PaintingView>().Show(ready, fit.CropRect);
            Vector3 sizeCm = GetComponent<MeshFilter>().sharedMesh.bounds.size * 100f;
            Debug.Log($"Built {ready}. Image fit: {fit}. Mesh measures {sizeCm.x:0.0} x {sizeCm.y:0.0} x {sizeCm.z:0.0} cm.", this);
        }
    }
}
