using UnityEditor;

namespace Capstone.Placement.EditorTools
{
    /// <summary>
    /// Keeps the practice screen out of Play unless this menu is checked.
    /// The flag is applied when the editor scripts load, which is before the scene starts.
    /// </summary>
    [InitializeOnLoad]
    static class PracticeVisitPlayGate
    {
        const string MenuPath = "Tools/Capstone/Show Practice Visit On Play";
        const string Key = "Capstone.ShowPracticeVisit";

        static PracticeVisitPlayGate()
        {
            Apply();
        }

        [MenuItem(MenuPath)]
        static void Toggle()
        {
            EditorPrefs.SetBool(Key, !EditorPrefs.GetBool(Key, false));
            Apply();
        }

        [MenuItem(MenuPath, true)]
        static bool ShowChecked()
        {
            Menu.SetChecked(MenuPath, EditorPrefs.GetBool(Key, false));
            return true;
        }

        static void Apply()
        {
            PracticeVisit.ShowOnPlay = EditorPrefs.GetBool(Key, false);
        }
    }
}
