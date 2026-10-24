using System.Collections.Generic;
using Capstone.Catalogue;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Capstone.Placement
{
    /// <summary>
    /// Places one sample artwork on a detected vertical wall and removes it in the same session.
    /// A second Place moves that same quad. No wall means a retry message and no invented size.
    /// </summary>
    public sealed class ArtworkPlacementController : MonoBehaviour
    {
        public const string WaitingMessage = "Point the centre of the screen at a wall, then press Place.";
        public const string NoWallMessage = "Move closer to a clearer wall and try again.";
        public const string PlacedMessage = "Placed on the wall.";
        public const string MovedMessage = "Moved on the wall. The size is unchanged.";
        public const string RemovedMessage = "Removed. You can place it again.";

        static readonly List<ARRaycastHit> Hits = new List<ARRaycastHit>();

        ARRaycastManager raycastManager;
        Camera arCamera;
        GameObject placed;
        Text status;
        Material surface;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureInScene()
        {
            if (FindAnyObjectByType<ArtworkPlacementController>() != null)
                return;
            if (FindAnyObjectByType<ARRaycastManager>() == null)
                return;

            GameObject host = new GameObject("Artwork Placement");
            host.AddComponent<ArtworkPlacementController>();
        }

        void Awake()
        {
            raycastManager = FindAnyObjectByType<ARRaycastManager>();
            arCamera = Camera.main;
            EnsureVerticalPlanes();
            SilenceTemplateSpawner();
            surface = CreateSurface();
            BuildControls();
        }

        public void Place()
        {
            if (!TryHitVerticalWall(out ARRaycastHit hit, out ARPlane plane))
            {
                status.text = NoWallMessage;
                return;
            }

            Artwork artwork = SampleArtwork.Create();
            bool alreadyPlaced = placed != null;
            if (!alreadyPlaced)
                placed = CreateQuad();

            WallPose pose = ArtworkWallPose.At(
                artwork,
                hit.pose.position,
                plane.normal,
                arCamera != null ? arCamera.transform.position : hit.pose.position + plane.normal);
            placed.transform.SetPositionAndRotation(pose.Position, pose.Rotation);
            placed.transform.localScale = pose.Scale;
            status.text = alreadyPlaced ? MovedMessage : PlacedMessage;
        }

        public void Remove()
        {
            if (placed == null)
            {
                status.text = RemovedMessage;
                return;
            }

            Destroy(placed);
            placed = null;
            status.text = RemovedMessage;
        }

        static void EnsureVerticalPlanes()
        {
            ARPlaneManager planes = FindAnyObjectByType<ARPlaneManager>();
            if (planes == null)
                return;

            planes.requestedDetectionMode |= PlaneDetectionMode.Vertical;
        }

        static void SilenceTemplateSpawner()
        {
            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour != null && behaviour.GetType().Name == "ARInteractorSpawnTrigger")
                    behaviour.enabled = false;
            }
        }

        bool TryHitVerticalWall(out ARRaycastHit hit, out ARPlane plane)
        {
            hit = default;
            plane = null;
            if (raycastManager == null)
                return false;

            Vector2 centre = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            if (!raycastManager.Raycast(centre, Hits, TrackableType.PlaneWithinPolygon))
                return false;

            foreach (ARRaycastHit candidate in Hits)
            {
                if (candidate.trackable is ARPlane candidatePlane
                    && candidatePlane.alignment == PlaneAlignment.Vertical)
                {
                    hit = candidate;
                    plane = candidatePlane;
                    return true;
                }
            }

            return false;
        }

        GameObject CreateQuad()
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Sample artwork";
            Collider collider = quad.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = surface;
            return quad;
        }

        Material CreateSurface()
        {
            Material template = Resources.Load<Material>("SampleArtworkSurface");
            Material material = template != null
                ? new Material(template)
                : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            Texture2D image = SampleTexture();
            material.SetTexture("_BaseMap", image);
            material.SetTexture("_MainTex", image);
            material.SetColor("_BaseColor", Color.white);
            return material;
        }

        static Texture2D SampleTexture()
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color blue = new Color(0.15f, 0.35f, 0.75f, 1f);
            Color white = Color.white;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool border = x < 3 || y < 3 || x >= size - 3 || y >= size - 3;
                    bool topBand = y >= size - 14;
                    texture.SetPixel(x, y, border || topBand ? white : blue);
                }
            }

            texture.Apply();
            return texture;
        }

        void BuildControls()
        {
            GameObject canvasObject = new GameObject("Artwork placement controls");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            canvasObject.AddComponent<GraphicRaycaster>();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            status = CreateLabel(canvasObject.transform, font, WaitingMessage, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), 36);
            CreateLabel(
                canvasObject.transform,
                font,
                "Sample wall work. 40 cm wide, 50 cm tall. White band is the top.",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -160f),
                28);
            CreateButton(canvasObject.transform, font, "Place", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 40f), Place);
            CreateButton(canvasObject.transform, font, "Remove", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 40f), Remove);
        }

        static Text CreateLabel(Transform parent, Font font, string message, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, int fontSize)
        {
            GameObject label = new GameObject(message);
            label.transform.SetParent(parent, false);
            Text text = label.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = message;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(900f, 120f);
            rect.anchoredPosition = position;
            return text;
        }

        static void CreateButton(Transform parent, Font font, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObject = new GameObject(label);
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin;
            rect.sizeDelta = new Vector2(420f, 140f);
            rect.anchoredPosition = position;

            GameObject caption = new GameObject("Label");
            caption.transform.SetParent(buttonObject.transform, false);
            Text text = caption.AddComponent<Text>();
            text.font = font;
            text.fontSize = 48;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }
    }
}
