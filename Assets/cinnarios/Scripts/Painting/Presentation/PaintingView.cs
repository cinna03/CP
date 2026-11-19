using Cinnarios.Painting.Data;
using Cinnarios.Painting.Geometry;
using UnityEngine;

namespace Cinnarios.Painting.Presentation
{
    /// <summary>
    /// The component on the Painting prefab: the reusable "holder" that turns a painting
    /// description into something visible. It builds the mesh for the painting's size and puts
    /// the image on the front. Showing a different painting reuses the same holder, which is
    /// what lets paintings be swapped in place.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class PaintingView : MonoBehaviour
    {
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseMapTilingOffsetId = Shader.PropertyToID("_BaseMap_ST");

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Mesh _generatedMesh;

        /// <summary>The painting currently shown, or null.</summary>
        public PaintingSpec Spec { get; private set; }

        private void Awake() => CacheComponents();

        private void OnDestroy() => DestroyGeneratedMesh();

        /// <param name="cropRect">The part of the image to show, in 0..1 image coordinates.</param>
        public void Show(PaintingSpec spec, Rect cropRect)
        {
            CacheComponents();
            Spec = spec;

            Mesh mesh = PaintingMeshBuilderFactory.For(spec.Medium).Build(spec.Dimensions);
            ReplaceMesh(mesh);
            ApplyImage(spec.Image, cropRect);
            FitCollider(mesh);
        }

        // The hold gesture needs something to hit. The box matches the real-size mesh.
        private void FitCollider(Mesh mesh)
        {
            var collider = GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<BoxCollider>();
            }

            collider.center = mesh.bounds.center;
            collider.size = mesh.bounds.size;
        }

        private void CacheComponents()
        {
            if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
            if (_meshRenderer == null) _meshRenderer = GetComponent<MeshRenderer>();
        }

        private void ReplaceMesh(Mesh mesh)
        {
            DestroyGeneratedMesh();
            _generatedMesh = mesh;
            _generatedMesh.hideFlags = HideFlags.DontSave; // generated, so never saved into a scene
            _meshFilter.sharedMesh = _generatedMesh;
        }

        private void ApplyImage(Texture2D image, Rect cropRect)
        {
            var block = new MaterialPropertyBlock();
            _meshRenderer.GetPropertyBlock(block, (int)PaintingSubmesh.Front);
            block.SetTexture(BaseMapId, image);
            // Tiling = how much of the image to show, offset = where that part starts.
            block.SetVector(BaseMapTilingOffsetId, new Vector4(cropRect.width, cropRect.height, cropRect.x, cropRect.y));
            _meshRenderer.SetPropertyBlock(block, (int)PaintingSubmesh.Front);
        }

        private void DestroyGeneratedMesh()
        {
            if (_generatedMesh == null) return;

            if (Application.isPlaying) Destroy(_generatedMesh);
            else DestroyImmediate(_generatedMesh);

            _generatedMesh = null;
        }
    }
}
