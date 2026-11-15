using Capstone.App.Data;
using UnityEngine;

namespace Capstone.App.AR
{
    /// <summary>
    /// Places the session's chosen work on a quad. The catalogue stays in the session asset.
    /// Scale is the chosen centimetres divided by 100. A frame sits just behind that quad.
    /// In the editor, with no tracked wall, the same quad is centred on a studio wall.
    /// </summary>
    public sealed class ARArtworkPresenter : MonoBehaviour
    {
        [SerializeField] ArtworkSessionState session;
        [SerializeField] Transform artworkQuad;
        [SerializeField] Renderer artworkRenderer;
        [SerializeField] Transform frameRoot;
        [SerializeField] Renderer frameRenderer;

        Material artworkMaterial;
        Transform studioWall;
        bool studio;

        public Transform ArtworkQuad => artworkQuad;
        public bool InStudio => studio;

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

        public void Bind(ArtworkSessionState value)
        {
            if (session == value)
                return;

            if (session != null)
                session.Changed -= Apply;
            session = value;
            if (isActiveAndEnabled && session != null)
                session.Changed += Apply;
            Apply();
        }

        /// <summary>Swaps the painting on the wall. The scene stays loaded. Size 0 keeps the default print.</summary>
        public void LoadArtworkIntoWall(ArtworkData artwork)
        {
            LoadArtworkIntoWall(artwork, 0, session != null ? session.SelectedFrame : null);
        }

        /// <summary>Swaps the painting, the print, and the frame without leaving the wall.</summary>
        public void LoadArtworkIntoWall(ArtworkData artwork, int sizeIndex, FrameOption frame)
        {
            if (artwork == null || session == null)
                return;

            session.Select(artwork, sizeIndex, frame);
            if (studio)
                PlaceOnStudio();
        }

        public Transform EnsureArtwork()
        {
            if (artworkQuad == null)
                CreateQuads();
            return artworkQuad;
        }

        public void BeginStudio()
        {
#if UNITY_EDITOR
            studio = true;
            EnsureArtwork();
            PlaceOnStudio();
#endif
        }

        public void HideStudio()
        {
            studio = false;
            if (studioWall != null)
                studioWall.gameObject.SetActive(false);
        }

        public void Apply()
        {
            if (session == null || session.SelectedArtwork == null)
                return;

            EnsureArtwork();
            ArtworkData work = session.SelectedArtwork;
            Vector2 metres = work.GetDimensionsInMeters(session.SizeIndex);
            if (artworkQuad != null && metres.x > 0f && metres.y > 0f)
                artworkQuad.localScale = new Vector3(metres.x, metres.y, 1f);

            ApplyPicture(work.ArtworkSprite);
            ApplyFrame(session.SelectedFrame);
        }

        void PlaceOnStudio()
        {
#if UNITY_EDITOR
            Camera camera = Camera.main;
            if (camera == null)
            {
                var camObject = new GameObject("Studio camera");
                camObject.transform.SetParent(transform, false);
                camObject.transform.position = new Vector3(0f, 1.5f, -2.6f);
                camera = camObject.AddComponent<Camera>();
                camera.tag = "MainCamera";
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color32(244, 240, 238, 255);
            }

            Vector3 forward = camera.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.forward;
            forward.Normalize();

            Vector3 wallPoint = camera.transform.position + forward * 2.6f;
            wallPoint.y = camera.transform.position.y;

            if (studioWall == null)
            {
                var wall = GameObject.CreatePrimitive(PrimitiveType.Quad);
                wall.name = "Studio wall";
                wall.transform.SetParent(transform, false);
                Collider blocker = wall.GetComponent<Collider>();
                if (blocker != null)
                    Destroy(blocker);
                Paint(wall.GetComponent<Renderer>(), new Color32(228, 221, 216, 255));
                studioWall = wall.transform;
            }

            studioWall.gameObject.SetActive(true);
            studioWall.localScale = new Vector3(3.4f, 2.6f, 1f);
            studioWall.SetPositionAndRotation(wallPoint, Quaternion.LookRotation(-forward, Vector3.up));
            artworkQuad.SetPositionAndRotation(wallPoint - forward * 0.03f, Quaternion.LookRotation(-forward, Vector3.up));
            Apply();
#endif
        }

        void CreateQuads()
        {
            var face = GameObject.CreatePrimitive(PrimitiveType.Quad);
            face.name = "Painting";
            face.transform.SetParent(transform, false);
            Collider faceBlocker = face.GetComponent<Collider>();
            if (faceBlocker != null)
                Destroy(faceBlocker);
            artworkQuad = face.transform;
            artworkRenderer = face.GetComponent<Renderer>();
            Paint(artworkRenderer, Color.white);

            var frame = GameObject.CreatePrimitive(PrimitiveType.Quad);
            frame.name = "Frame";
            frame.transform.SetParent(face.transform, false);
            frame.transform.localPosition = new Vector3(0f, 0f, -0.004f);
            frame.transform.localScale = new Vector3(1.08f, 1.08f, 1f);
            Collider frameBlocker = frame.GetComponent<Collider>();
            if (frameBlocker != null)
                Destroy(frameBlocker);
            frameRoot = frame.transform;
            frameRenderer = frame.GetComponent<Renderer>();
            frameRenderer.enabled = false;
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
            artworkMaterial.color = Color.white;
        }

        void ApplyFrame(FrameOption frame)
        {
            if (frameRenderer == null)
                return;

            bool unframed = frame == null || frame.FrameName == "Unframed" || string.IsNullOrEmpty(frame.FrameName);
            frameRenderer.enabled = !unframed;
            if (unframed)
                return;

            if (!studio && frame.Frame3DMaterial != null)
                frameRenderer.sharedMaterial = frame.Frame3DMaterial;
            else
                Paint(frameRenderer, frame.UIHighlightColor);
        }

        static void Paint(Renderer renderer, Color color)
        {
            if (renderer == null)
                return;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            material.color = color;
            renderer.material = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
