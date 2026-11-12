using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Puts the bubble picture on a Ball under the opening panel so it can be swapped in the Inspector.
[InitializeOnLoad]
static class PlaceBubble
{
    const string Done = "Intro.BubblePlaced";
    const string Picture = "Assets/Resources/Bubble.png";

    static PlaceBubble()
    {
        EditorApplication.delayCall += Place;
    }

    static void Place()
    {
        if (SessionState.GetBool(Done, false) || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Picture);
        if (sprite == null)
            return;

        int placed = 0;
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            if (canvas.gameObject.name != "Canvas")
                continue;

            Transform panel = OpeningPanel(canvas.transform);
            if (panel == null)
                continue;

            Transform found = panel.Find("Ball");
            Image image;
            if (found == null)
            {
                var obj = new GameObject("Ball", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Undo.RegisterCreatedObjectUndo(obj, "Add bubble");
                obj.transform.SetParent(panel, false);
                RectTransform rect = obj.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(220f, 220f);
                rect.anchoredPosition = new Vector2(0f, 180f);
                image = obj.GetComponent<Image>();
                image.raycastTarget = false;
            }
            else
            {
                image = found.GetComponent<Image>();
                if (image == null)
                    image = Undo.AddComponent<Image>(found.gameObject);
            }

            image.sprite = sprite;
            image.preserveAspect = true;
            image.color = Color.white;
            EditorUtility.SetDirty(image);

            IntroReveal reveal = canvas.GetComponent<IntroReveal>();
            if (reveal != null)
            {
                SerializedObject serial = new SerializedObject(reveal);
                serial.FindProperty("bubble").objectReferenceValue = image;
                serial.ApplyModifiedProperties();
            }

            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            placed++;
        }

        if (placed > 0)
            SessionState.SetBool(Done, true);
    }

    static Transform OpeningPanel(Transform canvas)
    {
        Transform named = canvas.Find("Beginning Panel");
        if (named != null)
            return named;

        foreach (Transform child in canvas)
        {
            if (child.GetComponentInChildren<RawImage>(true) != null)
                continue;
            if (child.GetComponentInChildren<TMP_Text>(true) != null)
                return child;
        }

        return null;
    }
}
