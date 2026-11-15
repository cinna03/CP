using UnityEngine;

/// <summary>
/// Marks the menu panel that is already in the scene, so the menu does not search every canvas.
/// </summary>
public sealed class MenuHome : MonoBehaviour
{
    public static RectTransform Panel { get; private set; }

    void OnEnable()
    {
        Panel = transform as RectTransform;
    }

    void OnDisable()
    {
        if (Panel == (RectTransform)transform)
            Panel = null;
    }
}
