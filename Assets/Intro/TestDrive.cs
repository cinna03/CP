using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// The live camera looks for walls. A tap puts the painting on one, and a drag slides it.
public class TestDrive : MonoBehaviour
{
    const float Width = 0.5f;
    const float Height = 0.64f;

    ARPlaneManager planes;
    ARRaycastManager rays;
    readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
    Transform piece;
    TMP_Text hint;
    Vector2 pressedAt;
    bool wasDown;
    bool dragging;

    public static void Begin()
    {
        if (FindAnyObjectByType<TestDrive>() != null)
            return;

        var host = new GameObject("Test Drive");
        host.AddComponent<TestDrive>();
    }

    void Awake()
    {
        planes = FindAnyObjectByType<ARPlaneManager>();
        rays = FindAnyObjectByType<ARRaycastManager>();
        if (planes != null)
        {
            planes.enabled = true;
            planes.requestedDetectionMode = PlaneDetectionMode.Vertical;
        }

        if (rays != null)
            rays.enabled = true;

        HideOtherCanvases();
        hint = Hint();
    }

    void Update()
    {
        if (planes != null)
        {
            planes.requestedDetectionMode = PlaneDetectionMode.Vertical;
            foreach (ARPlane plane in planes.trackables)
            {
                bool wall = plane != null && plane.alignment == PlaneAlignment.Vertical && plane.subsumedBy == null;
                if (plane == null)
                    continue;
                Renderer[] drawn = plane.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < drawn.Length; i++)
                    drawn[i].enabled = wall;
            }
        }

        bool down = Pointer(out Vector2 point);
        bool began = down && !wasDown;
        bool ended = !down && wasDown;
        if (began)
        {
            pressedAt = point;
            dragging = false;
        }

        bool moved = down && (point - pressedAt).sqrMagnitude > 18f * 18f;
        if (down && piece != null && moved && Wall(point, out ARRaycastHit hit))
        {
            dragging = true;
            Stick(hit);
        }

        if (ended && !dragging && Wall(point, out ARRaycastHit tap))
            Stick(tap);

        wasDown = down;
        if (hint != null)
            hint.text = piece != null ? "Drag to move it" : WallSeen() ? "Tap the wall to place it" : "Move across a wall";
    }

    bool Wall(Vector2 screen, out ARRaycastHit wall)
    {
        wall = default;
        if (rays == null || !rays.Raycast(screen, hits, TrackableType.PlaneWithinPolygon))
            return false;

        for (int i = 0; i < hits.Count; i++)
        {
            ARPlane plane = planes != null ? planes.GetPlane(hits[i].trackableId) : null;
            if (plane != null && plane.alignment == PlaneAlignment.Vertical)
            {
                wall = hits[i];
                return true;
            }
        }

        for (int i = 0; i < hits.Count; i++)
        {
            if (Mathf.Abs(Vector3.Dot(hits[i].pose.up, Vector3.up)) < 0.35f)
            {
                wall = hits[i];
                return true;
            }
        }

        return false;
    }

    bool WallSeen()
    {
        if (planes == null)
            return false;

        foreach (ARPlane plane in planes.trackables)
        {
            if (plane != null && plane.alignment == PlaneAlignment.Vertical && plane.subsumedBy == null)
                return true;
        }

        return false;
    }

    void Stick(ARRaycastHit hit)
    {
        Camera camera = Camera.main;
        Vector3 normal = hit.pose.up;
        if (camera != null && Vector3.Dot(normal, camera.transform.position - hit.pose.position) < 0f)
            normal = -normal;

        if (piece == null)
            piece = Piece();

        piece.SetPositionAndRotation(
            hit.pose.position + normal * 0.01f,
            Quaternion.LookRotation(normal, Vector3.up));
    }

    static Transform Piece()
    {
        var face = GameObject.CreatePrimitive(PrimitiveType.Quad);
        face.name = "Painting";
        face.transform.localScale = new Vector3(Width, Height, 1f);
        Paint(face.GetComponent<Renderer>(), new Color32(247, 236, 232, 255));

        var frame = GameObject.CreatePrimitive(PrimitiveType.Quad);
        frame.name = "Frame";
        frame.transform.SetParent(face.transform, false);
        frame.transform.localPosition = new Vector3(0f, 0f, -0.004f);
        frame.transform.localScale = new Vector3(1.08f, 1.08f, 1f);
        Paint(frame.GetComponent<Renderer>(), new Color32(28, 28, 30, 255));
        var blocker = frame.GetComponent<Collider>();
        if (blocker != null)
            Destroy(blocker);

        return face.transform;
    }

    static void Paint(Renderer renderer, Color color)
    {
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

    static TMP_Text Hint()
    {
        var host = new GameObject("DriveHint", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = host.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 700;
        canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1
            | AdditionalCanvasShaderChannels.Normal
            | AdditionalCanvasShaderChannels.Tangent;

        var scaler = host.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0f;

        var bar = new GameObject("Bar", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        bar.transform.SetParent(host.transform, false);
        var barRect = bar.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.5f, 0f);
        barRect.anchorMax = new Vector2(0.5f, 0f);
        barRect.pivot = new Vector2(0.5f, 0f);
        barRect.sizeDelta = new Vector2(860f, 120f);
        barRect.anchoredPosition = new Vector2(0f, 72f);
        var image = bar.GetComponent<UnityEngine.UI.Image>();
        image.color = new Color(1f, 1f, 1f, 0.92f);
        image.raycastTarget = false;

        var label = new GameObject("Hint", typeof(RectTransform));
        label.transform.SetParent(bar.transform, false);
        var labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(24f, 12f);
        labelRect.offsetMax = new Vector2(-24f, -12f);
        var text = label.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = 40f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color32(28, 28, 30, 255);
        text.raycastTarget = false;
        text.text = "Move across a wall";
        return text;
    }

    static void HideOtherCanvases()
    {
        Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || !canvas.gameObject.scene.IsValid() || canvas.name == "DriveHint")
                continue;
            canvas.gameObject.SetActive(false);
        }
    }

    static bool Pointer(out Vector2 point)
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            point = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            point = Mouse.current.position.ReadValue();
            return true;
        }

        point = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        return false;
    }
}
