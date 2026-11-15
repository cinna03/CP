using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// The plane manager and the raycast manager assigned on the AR rig. The wall visit reads these instead of searching the scene.
/// </summary>
public sealed class WallSessionLinks : MonoBehaviour
{
    public static WallSessionLinks Active { get; private set; }

    [SerializeField] ARPlaneManager planes;
    [SerializeField] ARRaycastManager rays;

    public ARPlaneManager Planes => planes;
    public ARRaycastManager Rays => rays;

    void OnEnable()
    {
        Active = this;
    }

    void OnDisable()
    {
        if (Active == this)
            Active = null;
    }
}
