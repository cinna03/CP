using Cinnarios.Painting.Data;
using Cinnarios.Painting.Geometry;
using UnityEngine;

namespace Cinnarios.Sandbox
{
    /// <summary>
    /// Test harness only, not part of the app: builds one 3D painting in the Editor
    /// from a test image and real dimensions, so the geometry can be checked before AR.
    /// Showing the image here is a temporary shortcut until the Surface step exists.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class PaintingSandboxPreview : MonoBehaviour
    {
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

        [Header("Artwork")]
        [SerializeField] private Texture2D image;
        [SerializeField] private string title = "Repose";
        [SerializeField] private string artist = "John White Alexander";

        [Header("Real size (cm)")]
        [SerializeField, Min(0.1f)] private float widthCm = 161.6f;
        [SerializeField, Min(0.1f)] private float heightCm = 132.7f;
        [Tooltip("Leave at 0 to use the default depth for the medium.")]
        [SerializeField, Min(0f)] private float depthCm;
        [SerializeField] private PaintingMedium medium = PaintingMedium.Canvas;

        private Mesh _generatedMesh;

        private void Start() => Rebuild();

        private void OnDestroy() => DestroyGeneratedMesh();

        [ContextMenu("Rebuild Painting")]
        public void Rebuild()
        {
            if (image == null)
            {
                Debug.LogWarning("PaintingSandboxPreview: assign a test image first.", this);
                return;
            }

            float depth = depthCm > 0f ? depthCm : medium.DefaultDepthCm();
            var spec = new PaintingSpec(
                "sandbox", title, artist, string.Empty, string.Empty,
                image, new PaintingDimensions(widthCm, heightCm, depth), medium);

            Mesh mesh = PaintingMeshBuilderFactory.For(spec.Medium).Build(spec.Dimensions);
            ReplaceMesh(mesh);
            ShowImageOnFront(spec.Image);

            Vector3 builtSizeCm = mesh.bounds.size * 100f;
            Debug.Log($"Built {spec}. Mesh measures {builtSizeCm.x:0.0} x {builtSizeCm.y:0.0} x {builtSizeCm.z:0.0} cm.", this);
        }

        private void ReplaceMesh(Mesh mesh)
        {
            DestroyGeneratedMesh();
            _generatedMesh = mesh;
            _generatedMesh.hideFlags = HideFlags.DontSave; // generated, so never saved into the scene file
            GetComponent<MeshFilter>().sharedMesh = _generatedMesh;
        }

        private void ShowImageOnFront(Texture2D frontImage)
        {
            var meshRenderer = GetComponent<MeshRenderer>();
            var block = new MaterialPropertyBlock();
            meshRenderer.GetPropertyBlock(block, (int)PaintingSubmesh.Front);
            block.SetTexture(BaseMapId, frontImage);
            meshRenderer.SetPropertyBlock(block, (int)PaintingSubmesh.Front);
        }

        private void DestroyGeneratedMesh()
        {
            if (_generatedMesh == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_generatedMesh);
            }
            else
            {
                DestroyImmediate(_generatedMesh);
            }

            _generatedMesh = null;
        }
    }
}
