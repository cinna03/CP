using System;
using Cinnarios.Painting.Data;
using Cinnarios.Painting.Presentation;
using Cinnarios.Painting.Validation;
using Cinnarios.Placement.Input;
using Cinnarios.Placement.Session;
using Cinnarios.Placement.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Cinnarios.Placement
{
    /// <summary>
    /// Runs one hanging session: aim at a wall, tap to hang the painting there, drag to slide
    /// it along that wall, Lock it, Move it again, or Scan again to start over. It coordinates
    /// the placement pieces (wall finder, slider, anchor, plane search, gestures) and does
    /// none of their work itself.
    /// </summary>
    public sealed class WallPlacementController : MonoBehaviour
    {
        // Keeps the painting's back a millimetre off the wall so the two surfaces never flicker.
        private const float WallClearanceMetres = 0.001f;

        [SerializeField] private ARWallRaycaster wallRaycaster;
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private PaintingView paintingPrefab;
        [Tooltip("The on-screen buttons and hint. Presses on them never count as wall taps or drags.")]
        [SerializeField] private PlacementHud hud;
        [Tooltip("Higher follows the aim faster; lower is steadier.")]
        [SerializeField, Min(1f)] private float smoothingSharpness = 12f;
        [Tooltip("How far a finger must move, in pixels, before a press counts as a drag rather than a tap.")]
        [SerializeField, Min(1f)] private float dragThresholdPixels = 12f;

        private readonly PlacementStateMachine _stateMachine = new PlacementStateMachine();
        private readonly WallSlider _slider = new WallSlider();
        private readonly PlacementAnchor _anchor = new PlacementAnchor();
        private IWallHitProvider _walls;
        private IScreenHitTest _uiHitTest;
        private PlaneDetectionToggle _planeSearch;
        private PointerGestureTracker _gestures;
        private PoseSmoother _smoother;
        private PaintingView _painting;
        private Camera _camera;
        private bool _listeningForPlanes;
        private bool _coachingReady;

        public PlacementState State => _stateMachine.Current;

        /// <summary>The painting holder, for read-only diagnostics.</summary>
        public Transform PaintingTransform => _painting != null ? _painting.transform : null;

        /// <summary>Raised whenever the placement stage changes, for the hint and buttons.</summary>
        public event Action<PlacementState> StateChanged;

        /// <summary>Raised when a finger holds still on the painting that is already on the wall.</summary>
        public event Action HeldOnPainting;

        private void Awake()
        {
            _walls = wallRaycaster;
            _uiHitTest = hud;
            _planeSearch = new PlaneDetectionToggle(planeManager);
            _gestures = new PointerGestureTracker(dragThresholdPixels);
            _smoother = new PoseSmoother(smoothingSharpness);
            _painting = Instantiate(paintingPrefab);
            _painting.gameObject.SetActive(false);
            _stateMachine.Changed += (_, next) => StateChanged?.Invoke(next);
        }

        private void OnEnable()
        {
            _coachingReady = false;
            if (planeManager == null)
            {
                return;
            }

            // A floor gives ARKit a stable room, which makes the wall appear sooner.
            // The floor itself is not drawn, and it is not a place a painting can hang.
            planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal | PlaneDetectionMode.Vertical;
            if (!_listeningForPlanes)
            {
                planeManager.trackablesChanged.AddListener(HideFloors);
                _listeningForPlanes = true;
            }
        }

        private void Start()
        {
            _camera = Camera.main;
        }

        private void OnDisable()
        {
            _coachingReady = false;
        }

        private void Update()
        {
            if (!_coachingReady)
            {
                _coachingReady = WallCoaching.AskForAWall();
            }

            if (_painting.Spec == null)
            {
                return;
            }

            PointerGesture gesture = _gestures.Update(IsOverUi);

            switch (_stateMachine.Current)
            {
                case PlacementState.Scanning:
                case PlacementState.Aiming:
                    UpdateAiming(gesture);
                    break;
                case PlacementState.Positioning:
                    UpdateSliding(gesture);
                    break;
                case PlacementState.Locked:
                    NoticeHold(gesture);
                    break; // fixed on the wall until Move or Scan again
            }
        }

        // A still press on the canvas opens its details. A press on the wall does not.
        private void NoticeHold(PointerGesture gesture)
        {
            if (gesture != PointerGesture.Hold || _camera == null || _painting == null)
            {
                return;
            }

            Ray ray = _camera.ScreenPointToRay(_gestures.Position);
            if (Physics.Raycast(ray, out RaycastHit hit, 30f) && hit.transform == _painting.transform)
            {
                HeldOnPainting?.Invoke();
            }
        }

        /// <summary>
        /// Shows a painting in the same holder. Position and stage stay as they are, and the new
        /// painting uses its own real size, which is what swapping will rely on.
        /// </summary>
        public bool ShowPainting(PaintingSpec recorded)
        {
            if (!PaintingPreparation.TryPrepare(recorded, out PaintingSpec ready, out ImageFitResult fit))
            {
                Debug.LogWarning($"Painting not shown: {fit}", this);
                return false;
            }

            _painting.Show(ready, fit.CropRect);
            return true;
        }

        /// <summary>
        /// Shows a plain canvas at the painting's true size while its image is still loading,
        /// so it can be hung straight away. Skips the image fit check, since the stand-in
        /// image says nothing about the painting's proportions.
        /// </summary>
        public void ShowPlaceholder(PaintingSpec placeholder)
        {
            _painting.Show(placeholder, ImageFitResult.FullImage);
        }

        /// <summary>Fixes the painting where it is: anchors it and stops searching for walls.</summary>
        public void Lock()
        {
            if (!_stateMachine.TryTransition(PlacementState.Locked))
            {
                return;
            }

            _anchor.Attach(_painting.transform);
            _planeSearch.SetSearching(false);
        }

        /// <summary>Lets the painting be slid again on the same wall. Wall search stays off.</summary>
        public void Move()
        {
            if (!_stateMachine.TryTransition(PlacementState.Positioning))
            {
                return;
            }

            // The anchor would fight the drag, so it is removed before sliding starts.
            _anchor.Detach(_painting.transform);
            _smoother.Reset();
            _smoother.Step(new Pose(_painting.transform.position, _painting.transform.rotation), 0f);
        }

        /// <summary>A new painting from the shop starts its own search. The previous lock is dropped.</summary>
        public void BeginNewVisit()
        {
            _anchor.Detach(_painting.transform);
            _slider.Clear();
            _gestures.Reset();
            _smoother.Reset();
            _painting.gameObject.SetActive(false);
            _planeSearch.SetSearching(true);
            _stateMachine.ForceScanning();
        }

        /// <summary>Takes the painting off the wall and starts placement from the beginning.</summary>
        public void ScanAgain()
        {
            if (!_stateMachine.TryTransition(PlacementState.Scanning))
            {
                return;
            }

            _anchor.Detach(_painting.transform);
            _slider.Clear();
            _gestures.Reset();
            _smoother.Reset();
            _painting.gameObject.SetActive(false);
            _planeSearch.SetSearching(true);
        }

        private void UpdateAiming(PointerGesture gesture)
        {
            var screenCentre = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            if (!_walls.TryGetWallHit(screenCentre, out Pose wallPose))
            {
                if (_stateMachine.TryTransition(PlacementState.Scanning))
                {
                    _painting.gameObject.SetActive(false);
                    _smoother.Reset();
                }

                return;
            }

            _stateMachine.TryTransition(PlacementState.Aiming);
            Pose smoothed = _smoother.Step(PaintingPoseOnWall(wallPose), Time.deltaTime);
            _painting.transform.SetPositionAndRotation(smoothed.position, smoothed.rotation);
            _painting.gameObject.SetActive(true);

            if (gesture == PointerGesture.Tap)
            {
                HangHere(smoothed);
            }
        }

        // The tap only hangs the painting: no anchor yet, the person is still choosing the spot.
        private void HangHere(Pose pose)
        {
            if (!_stateMachine.TryTransition(PlacementState.Positioning))
            {
                return;
            }

            _painting.transform.SetPositionAndRotation(pose.position, pose.rotation);
            _slider.Capture(pose);
            _smoother.Reset();
            _smoother.Step(pose, 0f);
        }

        // Only a drag moves the painting; between drags it stays fixed in the room.
        private void UpdateSliding(PointerGesture gesture)
        {
            switch (gesture)
            {
                case PointerGesture.DragStarted:
                    _slider.BeginDrag(_gestures.Position, _camera, _painting.transform.position);
                    break;
                case PointerGesture.Dragging:
                    if (_slider.TryDrag(_gestures.Position, _camera, out Vector3 target))
                    {
                        Pose smoothed = _smoother.Step(new Pose(target, _slider.Rotation), Time.deltaTime);
                        _painting.transform.SetPositionAndRotation(smoothed.position, _slider.Rotation);
                    }

                    break;
            }
        }

        // Turns a point on the wall into the painting's pose: facing out of the wall, kept
        // upright, and raised to gallery height when the floor has been found.
        private Pose PaintingPoseOnWall(Pose wallPose)
        {
            Vector3 wallNormal = wallPose.up;
            wallNormal.y = 0f; // walls are vertical; ignore small tilts so the painting hangs straight
            wallNormal.Normalize();

            // The painting's front faces -Z, so its forward (+Z) must point into the wall.
            // It stays on the point being looked at. A detected floor helps tracking and
            // does not slide the painting up or down.
            Quaternion rotation = Quaternion.LookRotation(-wallNormal, Vector3.up);
            Vector3 position = wallPose.position + wallNormal * WallClearanceMetres;
            return new Pose(position, rotation);
        }

        private static void HideFloors(ARTrackablesChangedEventArgs<ARPlane> changes)
        {
            foreach (ARPlane plane in changes.added)
            {
                ShowOnlyIfWall(plane);
            }

            foreach (ARPlane plane in changes.updated)
            {
                ShowOnlyIfWall(plane);
            }
        }

        private static void ShowOnlyIfWall(ARPlane plane)
        {
            if (plane.GetComponent<Session.QuietPlaneMark>() == null)
            {
                plane.gameObject.AddComponent<Session.QuietPlaneMark>();
            }

            bool wall = plane.alignment == PlaneAlignment.Vertical;
            Collider collider = plane.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = wall;
            }
        }

        // Presses on the on-screen buttons (UI Toolkit) or any uGUI element are not wall gestures.
        private bool IsOverUi(Vector2 screenPosition)
        {
            if (_uiHitTest != null && _uiHitTest.IsOverUi(screenPosition))
            {
                return true;
            }

            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
