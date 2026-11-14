using Capstone.App.Data;
using UnityEngine;

namespace Capstone.App.AR
{
    /// <summary>
    /// Places the session's chosen work on a quad. The catalogue stays in the session asset.
    /// Scale is the chosen centimetres divided by 100. A frame material is swapped beside that, and an empty frame stays hidden.
    /// </summary>
    public sealed class ARArtworkPresenter : MonoBehaviour
    {
        [SerializeField] ArtworkSessionState session;
        [SerializeField] Transform artworkQuad;
        [SerializeField] Renderer artworkRenderer;
        [SerializeField] Transform frameRoot;
        [SerializeField] Renderer frameRenderer;

        Material artworkMaterial;

        void OnEnable()
        {
            if (session != null)
                session.Changed += Apply;
            Apply();
        }

        void OnDisable()
        {
            if (session != null)
                session.Changed -= Apply;
        }

        void OnDestroy()
        {
            if (artworkMaterial != null)
                Destroy(artworkMaterial);
        }

        public void Apply()
        {
            if (session == null || session.SelectedArtwork == null)
                return;

            ArtworkData work = session.SelectedArtwork;
            Vector2 metres = work.GetDimensionsInMeters(session.SizeIndex);
            if (artworkQuad != null && metres.x > 0f && metres.y > 0f)
                artworkQuad.localScale = new Vector3(metres.x, metres.y, 1f);
            if (frameRoot != null && metres.x > 0f && metres.y > 0f)
                frameRoot.localScale = new Vector3(metres.x, metres.y, 1f);

            ApplyPicture(work.ArtworkSprite);
            ApplyFrame(session.SelectedFrame);
        }

        void ApplyPicture(Sprite sprite)
        {
            if (artworkRenderer == null || sprite == null || sprite.texture == null)
                return;

            if (artworkMaterial == null)
                artworkMaterial = artworkRenderer.material;

            Texture texture = sprite.texture;
            if (artworkMaterial.HasProperty("_BaseMap"))
                artworkMaterial.SetTexture("_BaseMap", texture);
            artworkMaterial.mainTexture = texture;
        }

        void ApplyFrame(FrameOption frame)
        {
            if (frameRenderer == null)
                return;

            bool framed = frame != null && frame.Frame3DMaterial != null;
            frameRenderer.enabled = framed;
            if (framed)
                frameRenderer.sharedMaterial = frame.Frame3DMaterial;
        }
    }
}
