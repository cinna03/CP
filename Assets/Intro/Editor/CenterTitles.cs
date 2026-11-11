using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Moves the Cinnarios titles to the middle of their panels once, in the open scene.
[InitializeOnLoad]
static class CenterTitles
{
    const string Done = "Intro.TitlesCentered";

    static CenterTitles()
    {
        EditorApplication.delayCall += Place;
    }

    static void Place()
    {
        if (SessionState.GetBool(Done, false) || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        int moved = 0;
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            if (canvas.gameObject.name != "Canvas")
                continue;

            foreach (Transform panel in canvas.transform)
            {
                TMP_Text title = TitleOn(panel);
                if (title == null)
                    continue;
                Center(title, panel as RectTransform);
                moved++;
                EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            }
        }

        if (moved > 0)
            SessionState.SetBool(Done, true);
    }

    static TMP_Text TitleOn(Transform panel)
    {
        TMP_Text[] texts = panel.GetComponentsInChildren<TMP_Text>(true);
        TMP_Text best = null;
        foreach (TMP_Text text in texts)
        {
            bool named = text.gameObject.name == "App Name";
            bool reads = text.text.IndexOf("cinnario", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (!named && !reads)
                continue;
            if (best == null || named || text.fontSize > best.fontSize)
                best = text;
        }

        return best;
    }

    static void Center(TMP_Text text, RectTransform panel)
    {
        RectTransform rect = text.rectTransform;
        Undo.RecordObject(rect, "Center title");
        Undo.RecordObject(text, "Center title");
        if (rect.parent != panel)
            Undo.SetTransformParent(rect, panel, "Center title");

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        text.alignment = TextAlignmentOptions.Center;
        EditorUtility.SetDirty(rect);
        EditorUtility.SetDirty(text);
    }
}
