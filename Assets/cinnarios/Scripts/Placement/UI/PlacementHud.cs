using UnityEngine;
using UnityEngine.UIElements;

namespace Cinnarios.Placement.UI
{
    /// <summary>
    /// The on-screen hint and the Lock, Move and Scan again buttons, built in code with
    /// UI Toolkit. It only shows what makes sense for the current stage and passes button
    /// presses to the controller; it holds no placement logic itself.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class PlacementHud : MonoBehaviour, IScreenHitTest
    {
        [SerializeField] private WallPlacementController controller;

        private VisualElement _root;
        private Label _hint;
        private Button _lockButton;
        private Button _moveButton;
        private Button _scanAgainButton;

        private void OnEnable()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            Build();

            if (controller != null)
            {
                controller.StateChanged += Render;
                Render(controller.State);
            }
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.StateChanged -= Render;
            }
        }

        public bool IsOverUi(Vector2 screenPosition)
        {
            if (_root?.panel == null)
            {
                return false;
            }

            // Screen positions start at the bottom-left; UI Toolkit panels start at the top-left.
            Vector2 flipped = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
            Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(_root.panel, flipped);
            VisualElement picked = _root.panel.Pick(panelPosition);
            return picked != null && picked != _root;
        }

        private void Build()
        {
            _root.Clear();
            _root.pickingMode = PickingMode.Ignore; // empty space passes touches through to the wall

            var bottomArea = new VisualElement { pickingMode = PickingMode.Ignore };
            bottomArea.style.position = Position.Absolute;
            bottomArea.style.left = 0;
            bottomArea.style.right = 0;
            bottomArea.style.bottom = 0;
            bottomArea.style.paddingBottom = 60;
            bottomArea.style.paddingLeft = 24;
            bottomArea.style.paddingRight = 24;
            bottomArea.style.alignItems = Align.Center;

            _hint = new Label { pickingMode = PickingMode.Ignore };
            _hint.style.backgroundColor = new Color(0f, 0f, 0f, 0.6f);
            _hint.style.color = Color.white;
            _hint.style.fontSize = 32;
            _hint.style.whiteSpace = WhiteSpace.Normal;
            _hint.style.unityTextAlign = TextAnchor.MiddleCenter;
            SetPadding(_hint, 16, 28);
            SetRadius(_hint, 28);
            _hint.style.marginBottom = 20;

            var buttonRow = new VisualElement { pickingMode = PickingMode.Ignore };
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.justifyContent = Justify.Center;

            _lockButton = CreateButton("Lock", () => controller.Lock());
            _moveButton = CreateButton("Move", () => controller.Move());
            _scanAgainButton = CreateButton("Scan again", () => controller.ScanAgain());

            buttonRow.Add(_lockButton);
            buttonRow.Add(_moveButton);
            buttonRow.Add(_scanAgainButton);
            bottomArea.Add(_hint);
            bottomArea.Add(buttonRow);
            _root.Add(bottomArea);
        }

        private void Render(PlacementState state)
        {
            switch (state)
            {
                case PlacementState.Scanning:
                    Show("Move your phone slowly to find a wall", lockVisible: false, moveVisible: false, scanAgainVisible: false);
                    break;
                case PlacementState.Aiming:
                    Show("Tap to hang the painting here", lockVisible: false, moveVisible: false, scanAgainVisible: false);
                    break;
                case PlacementState.Positioning:
                    Show("Drag to slide it along the wall, then Lock", lockVisible: true, moveVisible: false, scanAgainVisible: true);
                    break;
                case PlacementState.Locked:
                    Show("Locked on this wall", lockVisible: false, moveVisible: true, scanAgainVisible: true);
                    break;
            }
        }

        private void Show(string hint, bool lockVisible, bool moveVisible, bool scanAgainVisible)
        {
            _hint.text = hint;
            SetVisible(_lockButton, lockVisible);
            SetVisible(_moveButton, moveVisible);
            SetVisible(_scanAgainButton, scanAgainVisible);
        }

        private static Button CreateButton(string text, System.Action onClick)
        {
            var button = new Button(onClick) { text = text };
            button.style.height = 96;
            button.style.minWidth = 200;
            button.style.marginLeft = 12;
            button.style.marginRight = 12;
            button.style.fontSize = 32;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.color = Color.white;
            button.style.backgroundColor = new Color(0f, 0f, 0f, 0.75f);
            button.style.borderTopWidth = 0;
            button.style.borderBottomWidth = 0;
            button.style.borderLeftWidth = 0;
            button.style.borderRightWidth = 0;
            SetRadius(button, 48);
            SetPadding(button, 0, 32);
            return button;
        }

        private static void SetVisible(VisualElement element, bool visible) =>
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        private static void SetPadding(VisualElement element, float vertical, float horizontal)
        {
            element.style.paddingTop = vertical;
            element.style.paddingBottom = vertical;
            element.style.paddingLeft = horizontal;
            element.style.paddingRight = horizontal;
        }

        private static void SetRadius(VisualElement element, float radius)
        {
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
        }
    }
}
