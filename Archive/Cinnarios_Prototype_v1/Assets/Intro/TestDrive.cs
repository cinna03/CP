using System.Collections.Generic;
using Capstone.App.AR;
using Capstone.App.Data;
using Capstone.App.UI;
using Capstone.Catalogue;
using Capstone.Placement;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// The live camera looks for walls. A tap puts the painting on one, and a drag slides it.
public class TestDrive : MonoBehaviour
{
    static readonly Artwork SessionPiece = new Artwork(
        "session", "Session piece", "", 0, "", 64f, 50f, "", "", "", "", "", "");

    static TestDrive live;
    static ARPlaneManager pendingPlanes;
    static ARRaycastManager pendingRays;

    [SerializeField] ARPlaneManager planes;
    [SerializeField] ARRaycastManager rays;
    readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
    Transform piece;
    // The wall session's lifecycle (Idle > Scanning > Settling > Placed), checked against a transition table.
    readonly ARSessionStateMachine wallState = new ARSessionStateMachine();
    // Smooths finger drags so the work glides along the wall instead of jumping with every touch sample.
    readonly PoseSmoother dragSmoother = new PoseSmoother(24f);
    bool dragSmoothing;

    /// <summary>The current stage of the wall session.</summary>
    public WallSessionState WallState => wallState.Current;
    ARArtworkPresenter presenter;
    WallDecisionHUDController decisions;
    TMP_Text hint;
    GameObject hintHost;
    GameObject placeHereButton;
    GameObject lockButton;
    GameObject removeButton;
    GameObject selectedPin;
    WallCoach coach;
    string lastSizeLabel;
    const string CoachedKey = "cinnarios.wallCoached";
    WallPins pins;
    readonly List<Canvas> covered = new List<Canvas>();
    // Other full-screen canvases that were open when the wall visit started; hidden so the camera feed shows.
    readonly List<Canvas> screensBehind = new List<Canvas>();
    public const string RigMissing = "The AR camera rig was not found in the scene, so the wall cannot be tracked.";
    public const string NoAR = "This device does not support AR.";
    Vector2 pressedAt;
    Vector3 slideNormal = Vector3.forward;
    float waiting;
    bool wasDown;
    bool dragging;
    bool onInterface;
    bool grabbed;
    const float ManualPlaceDelay = 4f;
    const float ManualPlaceDistance = 1.6f;
    static readonly Color PlacePink = new Color32(233, 168, 188, 255);

    public static Canvas HintCanvas { get; private set; }
    public const string CameraDenied = "Camera access is required to preview art on your wall. Please enable Camera in iPhone Settings.";
    public const string TrackingPaused = "Wall tracking paused — point slowly at a well-lit wall.";

    public static void Begin()
    {
        if (live != null)
            return;

        if (WallSessionLinks.Active != null)
        {
            pendingPlanes = WallSessionLinks.Active.Planes;
            pendingRays = WallSessionLinks.Active.Rays;
        }

        var host = new GameObject("AR visualization session");
        host.AddComponent<TestDrive>();
    }

    public void Use(ARPlaneManager planeManager, ARRaycastManager raycastManager)
    {
        planes = planeManager;
        rays = raycastManager;
    }

    void Awake()
    {
        live = this;
        if (planes == null)
            planes = pendingPlanes;
        if (rays == null)
            rays = pendingRays;
        pendingPlanes = null;
        pendingRays = null;
        // Fall back to the rig in the scene if the links component did not register in time.
        if (planes == null)
            planes = Object.FindAnyObjectByType<ARPlaneManager>(FindObjectsInactive.Include);
        if (rays == null)
            rays = Object.FindAnyObjectByType<ARRaycastManager>(FindObjectsInactive.Include);
        SilenceTemplateSpawner();
        // Make sure the AR session is running on every visit, not just the first.
        ARSession arSession = Object.FindAnyObjectByType<ARSession>(FindObjectsInactive.Include);
        if (arSession != null)
            arSession.enabled = true;
        if (planes != null)
        {
            planes.enabled = true;
            planes.requestedDetectionMode = PlaneDetectionMode.Vertical;
        }

        if (rays != null)
        {
            rays.enabled = true;
            if (!Application.isEditor && rays.GetComponent<ARPointCloudManager>() == null)
                rays.gameObject.AddComponent<ARPointCloudManager>();
            // Locked works hang on AR anchors, which need an anchor manager on the rig.
            if (!Application.isEditor && rays.GetComponent<ARAnchorManager>() == null)
                rays.gameObject.AddComponent<ARAnchorManager>();
        }

#if !UNITY_EDITOR
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            StartCoroutine(AskForCamera());
#endif

        pins = gameObject.AddComponent<WallPins>();
        coach = new WallCoach(PlayerPrefs.GetInt(CoachedKey, 0) == 1);
        RoomLight.Attach(gameObject);
        CoverMenu();
        hint = Hint(out hintHost);
        AttachVisit();
    }

    void OnDestroy()
    {
        if (presenter != null)
            presenter.ResetRequested -= ResetWall;
        if (live == this)
            live = null;
        if (HintCanvas != null && hintHost != null && HintCanvas.gameObject == hintHost)
            HintCanvas = null;
    }

    System.Collections.IEnumerator AskForCamera()
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        RefreshHint();
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
                // The scan overlay is only for choosing a spot. Switching the whole plane object off is
                // sturdier than its renderers, which the template's visualiser turns back on every frame.
                // Raycasts still hit it: they go to the AR subsystem, not the GameObject.
                bool show = wall && piece == null;
                if (plane.gameObject.activeSelf != show)
                    plane.gameObject.SetActive(show);
            }
        }

        bool down = Pointer(out Vector2 point);
        bool began = down && !wasDown;
        bool ended = !down && wasDown;
        if (began)
        {
            pressedAt = point;
            dragging = false;
            grabbed = piece != null && Touches(piece, point);
            onInterface = OverInterface();
        }

        if (onInterface)
        {
            if (ended)
                onInterface = false;
            wasDown = down;
            return;
        }

        bool moved = down && (point - pressedAt).sqrMagnitude > 18f * 18f;
        // Only a finger that starts on the work moves it, so walking with a thumb on the screen never drags it along.
        if (down && piece != null && grabbed && (dragging || moved))
        {
            dragging = true;
            Slide(point);
        }

        if (ended && !dragging)
        {
            // Tapping a locked work selects it (to remove it); tapping the wall hangs the current work.
            GameObject hitPin = piece == null && pins != null ? pins.Hit(point, Camera.main) : null;
            Select(hitPin);
            if (hitPin == null && TryPlace(point, out Vector3 spot, out Quaternion turn))
                Put(spot, turn);
        }

        KeepSizeTag();

        if (!Application.isEditor && piece == null)
        {
            waiting += Time.unscaledDeltaTime;
            if (placeHereButton != null)
                placeHereButton.SetActive(waiting >= ManualPlaceDelay);
        }
        else if (placeHereButton != null && placeHereButton.activeSelf)
        {
            placeHereButton.SetActive(false);
        }

        if (lockButton != null)
        {
            bool canLock = !Application.isEditor && piece != null && piece.gameObject.activeInHierarchy;
            if (lockButton.activeSelf != canLock)
                lockButton.SetActive(canLock);
        }

        if (ended)
            dragSmoothing = false;
        wasDown = down;
        TrackWallState();
        RefreshHint();
    }

    /// <summary>Feeds what the camera sees into the wall-session state machine.</summary>
    void TrackWallState()
    {
        WallSessionState next = piece != null && piece.gameObject.activeInHierarchy
            ? WallSessionState.Placed
            : WallSeen() ? WallSessionState.Settling : WallSessionState.Scanning;
        if ((wallState.Current == WallSessionState.Idle || wallState.Current == WallSessionState.Placed)
            && next == WallSessionState.Settling)
            wallState.MoveTo(WallSessionState.Scanning);
        wallState.MoveTo(next);
    }

    void RefreshHint()
    {
        if (hint == null)
            return;

        if (!Application.isEditor && (planes == null || rays == null))
        {
            Say(RigMissing);
            return;
        }

        if (!Application.isEditor && ARSession.state == ARSessionState.Unsupported)
        {
            Say(NoAR);
            return;
        }

#if !UNITY_EDITOR
        // Only blame the camera permission once AR itself has had a chance to start.
        if (ARSession.state <= ARSessionState.CheckingAvailability && !Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            Say(CameraDenied);
            return;
        }
#endif

        if (!Application.isEditor && ARSession.state != ARSessionState.SessionTracking && ARSession.state != ARSessionState.None)
        {
            Say(TrackingPaused);
            return;
        }

        // First visit: numbered steps. Afterwards only short status lines, and none while a work is up.
        Say(coach.Line(WallSeen(), piece != null, pins != null ? pins.Count : 0));
    }

    /// <summary>Shows a line in the hint bar, or hides the bar when there is nothing to say.</summary>
    void Say(string line)
    {
        Transform bar = hintHost != null ? hintHost.transform.Find("Bar") : null;
        bool show = !string.IsNullOrEmpty(line);
        if (bar != null && bar.gameObject.activeSelf != show && !(Application.isEditor && presenter != null && presenter.InStudio))
            bar.gameObject.SetActive(show);
        if (show)
            hint.text = line;
    }

    /// <summary>True when the screen point lands on the work (its quad is one unit square, scaled to size).</summary>
    static bool Touches(Transform work, Vector2 screen)
    {
        Camera camera = Camera.main;
        if (camera == null || work == null)
            return false;

        Ray ray = camera.ScreenPointToRay(screen);
        var surface = new Plane(work.forward, work.position);
        if (!surface.Raycast(ray, out float distance))
            return false;

        Vector3 local = work.InverseTransformPoint(ray.GetPoint(distance));
        const float Margin = 0.62f; // a little beyond the edge so the frame is easy to grab
        return Mathf.Abs(local.x) <= Margin && Mathf.Abs(local.y) <= Margin;
    }

    bool TryPlace(Vector2 screen, out Vector3 position, out Quaternion rotation)
    {
        position = default;
        rotation = Quaternion.identity;
        if (VerticalPlane(screen, out ARRaycastHit planeHit))
        {
            PoseOnPlane(planeHit, out position, out rotation);
            return true;
        }

        if (Application.isEditor || rays == null)
            return false;
        if (!rays.Raycast(screen, hits, TrackableType.FeaturePoint) || hits.Count == 0)
            return false;

        position = hits[0].pose.position;
        rotation = UprightFacingBuyer();
        return true;
    }

    bool VerticalPlane(Vector2 screen, out ARRaycastHit wall)
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

    void PoseOnPlane(ARRaycastHit hit, out Vector3 position, out Quaternion rotation)
    {
        Camera camera = Camera.main;
        Vector3 normal = hit.pose.up;
        if (camera != null && Vector3.Dot(normal, camera.transform.position - hit.pose.position) < 0f)
            normal = -normal;
        slideNormal = normal;
        position = hit.pose.position + normal * 0.01f;
        rotation = Quaternion.LookRotation(normal, Vector3.up);
    }

    Quaternion UprightFacingBuyer()
    {
        Camera camera = Camera.main;
        Vector3 intoRoom = camera != null ? Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up) : Vector3.forward;
        if (intoRoom.sqrMagnitude < 0.0001f)
            intoRoom = Vector3.forward;
        intoRoom.Normalize();
        slideNormal = -intoRoom;
        return Quaternion.LookRotation(slideNormal, Vector3.up);
    }

    void PlaceOnWallHere()
    {
        Camera camera = Camera.main;
        if (camera == null)
            return;

        Quaternion rotation = UprightFacingBuyer();
        Vector3 intoRoom = -slideNormal;
        Vector3 position = camera.transform.position + intoRoom * ManualPlaceDistance;
        position.y = camera.transform.position.y;
        Put(position, rotation);
    }

    void Slide(Vector2 screen)
    {
        if (piece == null)
            return;

        Camera camera = Camera.main;
        if (camera == null)
            return;

        Vector3 normal = Vector3.ProjectOnPlane(slideNormal, Vector3.up);
        if (normal.sqrMagnitude < 0.0001f)
            normal = Vector3.ProjectOnPlane(piece.forward, Vector3.up);
        if (normal.sqrMagnitude < 0.0001f)
            normal = Vector3.forward;
        normal.Normalize();

        Ray ray = camera.ScreenPointToRay(screen);
        var plane = new Plane(normal, piece.position);
        if (!plane.Raycast(ray, out float distance))
            return;

        PlacementSettle.Cancel(piece);
        Quaternion facing = Quaternion.LookRotation(normal, Vector3.up);
        if (!dragSmoothing)
        {
            dragSmoother.SnapTo(piece.position, piece.rotation);
            dragSmoothing = true;
        }

        dragSmoother.Step(ray.GetPoint(distance), facing, Time.unscaledDeltaTime);
        Vector3 scale = piece.localScale;
        piece.SetPositionAndRotation(dragSmoother.Position, dragSmoother.Rotation);
        piece.localScale = scale;
    }

    void Put(Vector3 position, Quaternion rotation)
    {
        if (presenter != null)
            presenter.HideStudio();
        if (piece == null)
            piece = presenter != null ? presenter.EnsureArtwork() : Piece();
        if (piece == null)
            return;

        piece.gameObject.SetActive(true);
        RoomLight.Register(piece.GetComponent<Renderer>());
        Vector3 scale = piece.localScale;
        piece.SetPositionAndRotation(position, rotation);
        piece.localScale = scale;
        if (presenter != null)
            presenter.Apply();
        PlacementSettle.Begin(piece, position, rotation);
        waiting = 0f;
        if (placeHereButton != null)
            placeHereButton.SetActive(false);
    }

    void ResetWall()
    {
        Select(null);
        if (pins != null)
            pins.Clear();
        if (!Application.isEditor)
        {
            // Forget the old wall so the phone scans afresh for a new one.
            ARSession arSession = Object.FindAnyObjectByType<ARSession>();
            if (arSession != null)
                arSession.Reset();
        }

        if (Application.isEditor && presenter != null && presenter.InStudio)
        {
            presenter.BeginStudio();
            piece = presenter.ArtworkQuad;
            if (piece != null)
                slideNormal = piece.forward;
            return;
        }

        if (presenter != null && presenter.ArtworkQuad != null)
            presenter.ArtworkQuad.gameObject.SetActive(false);
        else if (piece != null)
            piece.gameObject.SetActive(false);

        piece = null;
        waiting = 0f;
        if (placeHereButton != null)
            placeHereButton.SetActive(false);
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

    static Transform Piece()
    {
        CatalogueShelf shelf = Resources.Load<CatalogueShelf>("CatalogueShelf");
        ArtworkSessionState visit = shelf != null ? shelf.Session : null;
        ArtworkData chosen = visit != null ? visit.SelectedArtwork : null;

        var face = GameObject.CreatePrimitive(PrimitiveType.Quad);
        face.name = "Painting";
        Renderer faceRenderer = face.GetComponent<Renderer>();
        if (chosen != null)
        {
            Vector2 metres = chosen.GetDimensionsInMeters(visit.SizeIndex);
            if (metres.x > 0f && metres.y > 0f)
                face.transform.localScale = new Vector3(metres.x, metres.y, 1f);
            Paint(faceRenderer, Color.white);
            ApplyPicture(faceRenderer, chosen.ArtworkSprite);
        }
        else
        {
            WorkQuad.Fit(face.transform, SessionPiece);
            Paint(faceRenderer, new Color32(247, 236, 232, 255));
        }

        var frame = GameObject.CreatePrimitive(PrimitiveType.Quad);
        frame.name = "Frame";
        frame.transform.SetParent(face.transform, false);
        frame.transform.localPosition = new Vector3(0f, 0f, -0.004f);
        frame.transform.localScale = new Vector3(1.08f, 1.08f, 1f);
        Renderer frameRenderer = frame.GetComponent<Renderer>();
        FrameOption option = visit != null ? visit.SelectedFrame : null;
        bool showFrame = chosen != null && option != null && option.FrameName != "Unframed";
        frameRenderer.enabled = showFrame || chosen == null;
        Paint(frameRenderer, showFrame ? option.UIHighlightColor : new Color32(28, 28, 30, 255));
        var blocker = frame.GetComponent<Collider>();
        if (blocker != null)
            Destroy(blocker);

        return face.transform;
    }

    static void ApplyPicture(Renderer renderer, Sprite sprite)
    {
        if (renderer == null || sprite == null || sprite.texture == null)
            return;

        Material material = renderer.material;
        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", sprite.texture);
        material.mainTexture = sprite.texture;
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

    TMP_Text Hint(out GameObject host)
    {
        host = new GameObject("DriveHint", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Capstone.App.UI.SafeAreaFitter));
        HintCanvas = host.GetComponent<Canvas>();
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

        var backObject = new GameObject("Back", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(Button));
        backObject.transform.SetParent(host.transform, false);
        var backRect = backObject.GetComponent<RectTransform>();
        backRect.anchorMin = backRect.anchorMax = new Vector2(0f, 1f);
        backRect.pivot = new Vector2(0f, 1f);
        backRect.sizeDelta = new Vector2(180f, 72f);
        backRect.anchoredPosition = new Vector2(36f, -48f);
        var backImage = backObject.GetComponent<UnityEngine.UI.Image>();
        backImage.color = new Color(1f, 1f, 1f, 0.92f);
        var backButton = backObject.GetComponent<Button>();
        backButton.targetGraphic = backImage;
        backButton.onClick.AddListener(LeaveToCaller);
        // The wall's own top bar (WallDecisionHUDController) already has Back; this older one sat behind it.
        backObject.SetActive(false);

        var backLabel = new GameObject("Label", typeof(RectTransform));
        backLabel.transform.SetParent(backObject.transform, false);
        var backLabelRect = backLabel.GetComponent<RectTransform>();
        backLabelRect.anchorMin = Vector2.zero;
        backLabelRect.anchorMax = Vector2.one;
        backLabelRect.offsetMin = backLabelRect.offsetMax = Vector2.zero;
        var backText = backLabel.AddComponent<TextMeshProUGUI>();
        if (Capstone.App.UI.AppType.BodyFont != null)
            backText.font = Capstone.App.UI.AppType.BodyFont;
        backText.text = "Back";
        backText.fontSize = 32f;
        backText.fontStyle = FontStyles.Bold;
        backText.alignment = TextAlignmentOptions.Center;
        backText.color = new Color32(28, 28, 30, 255);
        backText.raycastTarget = false;

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
        if (Capstone.App.UI.AppType.BodyFont != null)
            text.font = Capstone.App.UI.AppType.BodyFont;
        text.fontSize = 40f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color32(28, 28, 30, 255);
        text.raycastTarget = false;
        text.text = "Move across a wall";
        placeHereButton = PlaceHereButton(host.transform);
        lockButton = LockButton(host.transform);
        removeButton = RemoveButton(host.transform);
        return text;
    }

    GameObject PlaceHereButton(Transform parent)
    {
        var host = new GameObject("Place on Wall Here", typeof(RectTransform), typeof(Image), typeof(Button));
        host.transform.SetParent(parent, false);
        var rect = host.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(560f, 88f);
        rect.anchoredPosition = new Vector2(0f, -220f);
        Image image = host.GetComponent<Image>();
        image.color = PlacePink;
        Button button = host.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(PlaceOnWallHere);

        var label = new GameObject("Label", typeof(RectTransform));
        label.transform.SetParent(host.transform, false);
        var labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        var caption = label.AddComponent<TextMeshProUGUI>();
        if (Capstone.App.UI.AppType.BodyFont != null)
            caption.font = Capstone.App.UI.AppType.BodyFont;
        caption.text = "Place on Wall Here";
        caption.fontSize = 32f;
        caption.fontStyle = FontStyles.Bold;
        caption.alignment = TextAlignmentOptions.Center;
        caption.color = new Color32(42, 34, 37, 255);
        caption.raycastTarget = false;
        host.SetActive(false);
        return host;
    }

    /// <summary>Fixes the work where it is: it stays put as the buyer walks around, and the next work can be added beside it.</summary>
    void LockHere()
    {
        if (piece == null || pins == null)
            return;

        PlacementSettle.Cancel(piece);
        pins.Pin(piece);
        piece.gameObject.SetActive(false);
        if (!coach.Learned)
        {
            coach.MarkLearned();
            PlayerPrefs.SetInt(CoachedKey, 1);
            PlayerPrefs.Save();
        }
        piece = null;
        dragging = false;
        dragSmoothing = false;
        waiting = 0f;
    }

    /// <summary>Keeps the size tag under the live work in step with the chosen work and print size.</summary>
    void KeepSizeTag()
    {
        if (piece == null || !piece.gameObject.activeInHierarchy)
            return;
        CatalogueShelf shelf = Resources.Load<CatalogueShelf>("CatalogueShelf");
        ArtworkSessionState visit = shelf != null ? shelf.Session : null;
        string label = visit != null && visit.SelectedArtwork != null ? visit.SelectedArtwork.FormatSizeLabel(visit.SizeIndex) : "";
        if (label == lastSizeLabel && piece.GetComponent<SizeTag>() != null)
            return;
        lastSizeLabel = label;
        SizeTag.Show(piece, label);
    }

    void Select(GameObject holder)
    {
        selectedPin = holder;
        if (removeButton != null)
            removeButton.SetActive(holder != null);
    }

    void RemoveSelected()
    {
        if (pins != null && selectedPin != null)
            pins.Remove(selectedPin);
        Select(null);
    }

    GameObject RemoveButton(Transform parent)
    {
        GameObject host = LockButton(parent);
        host.name = "Remove this work";
        Button button = host.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(RemoveSelected);
        TMP_Text caption = host.GetComponentInChildren<TMP_Text>(true);
        if (caption != null)
            caption.text = "Take this work down";
        host.SetActive(false);
        return host;
    }

    GameObject LockButton(Transform parent)
    {
        GameObject host = PlaceHereButton(parent);
        host.name = "Lock it here";
        var rect = host.GetComponent<RectTransform>();
        // Under Reset Wall, clear of the work, so it never covers what the buyer is judging.
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -150f);
        rect.sizeDelta = new Vector2(420f, 84f);
        Image pill = host.GetComponent<Image>();
        Sprite rounded = Capstone.App.UI.UIShapes.Rounded();
        if (rounded != null)
        {
            pill.sprite = rounded;
            pill.type = Image.Type.Sliced;
        }
        Button button = host.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(LockHere);
        TMP_Text caption = host.GetComponentInChildren<TMP_Text>(true);
        if (caption != null)
            caption.text = "Lock it here";
        return host;
    }

    void AttachVisit()
    {
        CatalogueShelf shelf = Resources.Load<CatalogueShelf>("CatalogueShelf");
        ArtworkSessionState visit = shelf != null ? shelf.Session : null;
        var host = new GameObject("Wall visit");
        presenter = host.AddComponent<ARArtworkPresenter>();
        presenter.Bind(visit);
        presenter.ResetRequested += ResetWall;

#if UNITY_EDITOR
        if (!WallSeen())
        {
            presenter.BeginStudio();
            piece = presenter.ArtworkQuad;
            if (piece != null)
                slideNormal = piece.forward;
            Transform bar = hintHost != null ? hintHost.transform.Find("Bar") : null;
            if (bar != null)
                bar.gameObject.SetActive(false);
        }
#endif

        var panel = new GameObject("Wall decision");
        decisions = panel.AddComponent<WallDecisionHUDController>();
        decisions.VisitFinished += Leave;
        decisions.VisitCancelled += LeaveToCaller;
        decisions.Open(presenter, visit, shelf != null ? shelf.Works : null, shelf != null ? shelf.Frames : null);
        decisions.RememberForPhoto(HintCanvas);
    }

    // The AR template ships a tap-to-spawn demo (blue shapes). It competes with the painting for taps, so switch it off.
    static void SilenceTemplateSpawner()
    {
        MonoBehaviour[] all = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            MonoBehaviour part = all[i];
            if (part == null)
                continue;
            string kind = part.GetType().Name;
            if (kind == "ObjectSpawner")
                part.gameObject.SetActive(false);
            else if (kind == "ARInteractorSpawnTrigger")
                part.enabled = false;
        }
    }

    void CoverMenu()
    {
        Hide(MainMenu.OverlayCanvas);
        Hide(MenuPage.Page);
        HideScreensBehind();
    }

    /// <summary>
    /// Hides any other full-screen overlay canvas (catalogue, intro panels, studio screens) so nothing
    /// paints over the live camera feed. They are shown again when the visit ends.
    /// </summary>
    void HideScreensBehind()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || !canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                continue;
            if (!canvas.enabled || covered.Contains(canvas) || canvas.GetComponentInParent<TestDrive>() != null)
                continue;
            // Switch off drawing and taps only. Deactivating the object would stop its scripts and
            // animations mid-way (and anything parented under it), which left the catalogue unresponsive.
            screensBehind.Add(canvas);
            canvas.enabled = false;
            var taps = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (taps != null)
                taps.enabled = false;
        }
    }

    void Hide(Canvas canvas)
    {
        if (canvas == null || !canvas.gameObject.activeInHierarchy)
            return;
        covered.Add(canvas);
        canvas.gameObject.SetActive(false);
    }

    void Leave()
    {
        Exit(false, true);
    }

    void LeaveToCaller()
    {
        Exit(true, false);
    }

    /// <summary>Ends the wall visit. Only Finish Visit (finished) asks the "after" questions; Back just returns.</summary>
    void Exit(bool restorePage, bool finished)
    {
        Canvas page = null;
        for (int i = 0; i < covered.Count; i++)
        {
            Canvas canvas = covered[i];
            if (canvas == null)
                continue;
            if (canvas.name == "MainMenuOverlay")
                continue;
            page = canvas;
        }

        for (int i = 0; i < covered.Count; i++)
        {
            Canvas canvas = covered[i];
            if (canvas == null)
                continue;
            if (canvas.name == "MainMenuOverlay")
                canvas.gameObject.SetActive(true);
            else if (restorePage)
                canvas.gameObject.SetActive(true);
        }

        if (restorePage && page != null)
            page.transform.SetAsLastSibling();
        for (int i = 0; i < screensBehind.Count; i++)
        {
            Canvas canvas = screensBehind[i];
            if (canvas == null)
                continue;
            canvas.enabled = true;
            var taps = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (taps != null)
                taps.enabled = true;
        }
        screensBehind.Clear();

        if (planes != null)
            planes.enabled = false;
        if (presenter != null)
            Destroy(presenter.gameObject);
        else if (piece != null)
            Destroy(piece.gameObject);
        if (decisions != null)
            Destroy(decisions.gameObject);
        if (hintHost != null)
            Destroy(hintHost);
        Destroy(gameObject);
        if (finished)
            SurveyFlow.OpenPost();
    }

    static bool OverInterface()
    {
        if (EventSystem.current == null)
            return false;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            return EventSystem.current.IsPointerOverGameObject(Touchscreen.current.primaryTouch.touchId.ReadValue());

        return EventSystem.current.IsPointerOverGameObject(-1);
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
