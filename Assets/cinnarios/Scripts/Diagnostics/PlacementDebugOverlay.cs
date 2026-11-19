using System.Text;
using Cinnarios.Launch;
using Cinnarios.Placement;
using UnityEngine;

namespace Cinnarios.Diagnostics
{
    /// <summary>
    /// TEMPORARY test aid, to be removed before release. Prints live placement facts on the
    /// screen so one iPhone build can show what is going on: the stage, the load status, the
    /// camera's roll, which way the painting's "up" points in the world, and the size it was
    /// built at. It only reads values and draws text; it never changes anything.
    /// </summary>
    public sealed class PlacementDebugOverlay : MonoBehaviour
    {
        [SerializeField] private WallPlacementController placement;
        [SerializeField] private ArtworkLaunchLoader loader;

        private readonly StringBuilder _text = new StringBuilder();
        private GUIStyle _style;
        private GUIStyle _shadow;

        private void OnGUI()
        {
            if (_style == null)
            {
                int fontSize = Mathf.Max(14, Mathf.RoundToInt(Screen.height * 0.016f));
                _style = new GUIStyle(GUI.skin.label) { fontSize = fontSize, wordWrap = true };
                _style.normal.textColor = Color.yellow;
                _shadow = new GUIStyle(_style);
                _shadow.normal.textColor = Color.black;
            }

            _text.Clear();
            if (placement != null)
            {
                _text.AppendLine($"Stage: {placement.State}");
            }

            if (loader != null)
            {
                _text.AppendLine($"Load: {loader.Status}");
            }

            Camera camera = Camera.main;
            if (camera != null)
            {
                _text.AppendLine($"Camera roll: {SignedAngle(camera.transform.eulerAngles.z):0} deg (about 0 when the phone is upright)");
            }

            Transform painting = placement != null ? placement.PaintingTransform : null;
            if (painting != null && painting.gameObject.activeInHierarchy)
            {
                _text.AppendLine($"Painting up: {painting.up.ToString("F2")} (should be 0.00, 1.00, 0.00)");
                var meshFilter = painting.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    Vector3 sizeCm = meshFilter.sharedMesh.bounds.size * 100f;
                    _text.AppendLine($"Built size: {sizeCm.x:0.0} x {sizeCm.y:0.0} cm (width x height)");
                }
            }

            _text.Append($"Screen: {Screen.width} x {Screen.height}, {Screen.orientation}");

            var area = new Rect(24f, Screen.height * 0.06f, Screen.width - 48f, Screen.height * 0.4f);
            GUI.Label(new Rect(area.x + 2f, area.y + 2f, area.width, area.height), _text.ToString(), _shadow);
            GUI.Label(area, _text.ToString(), _style);
        }

        private static float SignedAngle(float degrees) => degrees > 180f ? degrees - 360f : degrees;
    }
}
