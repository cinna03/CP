using UnityEditor;
using UnityEngine;

// Puts the four menu cards on MainMenuPanel once, so they can be edited in the scene.
[InitializeOnLoad]
static class MainMenuPlace
{
    static MainMenuPlace()
    {
        EditorApplication.delayCall += Place;
    }

    static void Place()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        RectTransform[] rects = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include);
        for (int i = 0; i < rects.Length; i++)
        {
            if (rects[i] == null || rects[i].name != "MainMenuPanel")
                continue;
            if (EditorUtility.IsPersistent(rects[i]))
                continue;

            MainMenu.Ensure(rects[i]);
            return;
        }
    }
}
