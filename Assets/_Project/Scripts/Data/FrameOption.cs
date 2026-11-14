using UnityEngine;

namespace Capstone.App.Data
{
    /// <summary>
    /// A frame the buyer can preview. The frame changes the border and the frame material.
    /// It does not change the stored centimetres.
    /// </summary>
    [CreateAssetMenu(fileName = "Frame", menuName = "Capstone/Frame Option")]
    public sealed class FrameOption : ScriptableObject
    {
        [SerializeField] string frameName = "";
        [SerializeField] Color uiHighlightColor = Color.white;
        [SerializeField] Material frame3DMaterial;

        public string FrameName => frameName ?? "";
        public Color UIHighlightColor => uiHighlightColor;
        public Material Frame3DMaterial => frame3DMaterial;
    }
}
