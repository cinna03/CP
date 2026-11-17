using System;
using Cinnarios.Painting.Data;
using Cinnarios.Painting.Presentation;
using Cinnarios.Painting.Validation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;

namespace Cinnarios.Placement
{
    /// <summary>
    /// Runs the hanging flow. While aiming, the painting previews on the wall under the centre
    /// of the screen, smoothed and at gallery height. A tap anchors it there. It coordinates the
    /// other placement classes but does none of their work itself.
    /// </summary>
    public sealed class WallPlacementController : MonoBehaviour
    {
        // Keeps the painting's back a millimetre off the wall so the two surfaces never flicker.
        private const float WallClearanceMetres = 0.001f;

        [SerializeField] private ARWallRaycaster wallRaycaster;
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private PaintingView paintingPrefab;
        [SerializeField] private PaintingSpecAsset startingPainting;
        [Tooltip("Higher follows the aim faster; lower is steadier.")]
        [SerializeField, Min(1f)] private float smoothingSharpness = 12f;

        private readonly PlacementStateMachine _stateMachine = new PlacementStateMachine();
        private readonly HangingHeightRule _hangingHeight = new HangingHeightRule();
        private IWallHitProvider _walls;
        private PoseSmoother _smoother;
        private PaintingView _painting;
        private GameObject _anchorObject;

        public PlacementState State => _stateMachine.Current;

        /// <summary>Raised whenever the placement stage changes, for UI hints.</summary>
        public event Action<PlacementState> StateChanged;

        private void Awake()
        {
            _walls = wallRaycaster;
            _smoother = new PoseSmoother(smoothingSharpness);
            _painting = Instantiate(paintingPrefab);
            _painting.gameObject.SetActive(false);
            _stateMachine.Changed += (_, next) => StateChanged?.Invoke(next);
        }

        private void Start()
        {
            if (startingPainting != null)
            {
                ShowPainting(startingPainting.ToSpec());
            }
        }

        private void Update()
        {
            if (_stateMachine.Current == PlacementState.Placed || _painting.Spec == null)
            {
                return;
            }

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

            if (TapStartedThisFrame())
            {
                Place(smoothed);
            }
        }

        /// <summary>
        /// Shows a painting in the same holder. When it is already on the wall, the new one
        /// appears in the same spot, centred, which is what makes compare-in-place work.
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

        /// <summary>Takes the painting off the wall so it can be aimed and placed again.</summary>
        public void Reposition()
        {
            if (!_stateMachine.TryTransition(PlacementState.Scanning))
            {
                return;
            }

            _painting.transform.SetParent(null, true);
            if (_anchorObject != null)
            {
                Destroy(_anchorObject);
                _anchorObject = null;
            }

            _painting.gameObject.SetActive(false);
            _smoother.Reset();
        }

        private void Place(Pose pose)
        {
            if (!_stateMachine.TryTransition(PlacementState.Placed))
            {
                return;
            }

            // An anchor tells AR to keep this exact spot fixed in the real room as tracking improves.
            _anchorObject = new GameObject("Painting Anchor");
            _anchorObject.transform.SetPositionAndRotation(pose.position, pose.rotation);
            _anchorObject.AddComponent<ARAnchor>();
            _painting.transform.SetParent(_anchorObject.transform, true);
        }

        // Turns a point on the wall into the painting's pose: facing out of the wall, kept
        // upright, and raised to gallery height when the floor has been found.
        private Pose PaintingPoseOnWall(Pose wallPose)
        {
            Vector3 wallNormal = wallPose.up;
            wallNormal.y = 0f; // walls are vertical; ignore small tilts so the painting hangs straight
            wallNormal.Normalize();

            // The painting's front faces -Z, so its forward (+Z) must point into the wall.
            Quaternion rotation = Quaternion.LookRotation(-wallNormal, Vector3.up);
            Vector3 position = wallPose.position + wallNormal * WallClearanceMetres;

            if (FloorHeightEstimator.TryEstimate(planeManager, out float floorHeight))
            {
                float paintingHeight = _painting.Spec.Dimensions.SizeInMetres.y;
                position.y = _hangingHeight.CentreHeight(floorHeight, paintingHeight);
            }

            return new Pose(position, rotation);
        }

        private static bool TapStartedThisFrame()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame)
            {
                return false;
            }

            // Ignore taps on buttons and other UI.
            return EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject();
        }
    }
}
