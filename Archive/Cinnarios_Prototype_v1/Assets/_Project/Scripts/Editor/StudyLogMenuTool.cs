using System.Globalization;
using System.IO;
using Capstone.App.Services;
using UnityEditor;
using UnityEngine;

namespace Capstone.App.EditorTools
{
    /// <summary>
    /// Opens the local confidence log so the study file can be shown during a presentation.
    /// </summary>
    public static class StudyLogMenuTool
    {
        [MenuItem("Tools/Capstone/Open Study Log Folder")]
        public static void OpenStudyLogFolder()
        {
            string folder = Path.Combine(Application.persistentDataPath, "StudyLogs");
            Directory.CreateDirectory(folder);
            float average = new DecisionStudyLogger().GetAverageConfidenceScore();
            Debug.Log("Average confidence score: " + average.ToString("0.00", CultureInfo.InvariantCulture));
            EditorUtility.RevealInFinder(folder);
        }
    }
}
