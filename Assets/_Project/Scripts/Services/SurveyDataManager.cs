using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Capstone.App.Services
{
    /// <summary>
    /// Holds the four pre scores and the four post scores for the active entrance code.
    /// Both saves write the same CSV row: EntranceCode, PreQ1..PreQ4, PostQ1..PostQ4.
    /// </summary>
    public sealed class SurveyDataManager : MonoBehaviour
    {
        public const int QuestionCount = 4;
        const string FolderName = "StudyLogs";
        const string FileName = "survey_pairs.csv";
        const string Header = "EntranceCode,PreQ1,PreQ2,PreQ3,PreQ4,PostQ1,PostQ2,PostQ3,PostQ4";

        public static SurveyDataManager Instance { get; private set; }

        readonly int[] pre = new int[QuestionCount];
        readonly int[] post = new int[QuestionCount];

        public static SurveyDataManager Ensure()
        {
            if (Instance != null)
                return Instance;

            var host = new GameObject("Survey data");
            DontDestroyOnLoad(host);
            Instance = host.AddComponent<SurveyDataManager>();
            return Instance;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void ClearRound()
        {
            for (int i = 0; i < QuestionCount; i++)
            {
                pre[i] = 0;
                post[i] = 0;
            }
        }

        public void RecordPreAnswer(int index, int score)
        {
            if (index < 0 || index >= QuestionCount)
                return;
            pre[index] = Mathf.Clamp(score, 1, 5);
        }

        public void RecordPostAnswer(int index, int score)
        {
            if (index < 0 || index >= QuestionCount)
                return;
            post[index] = Mathf.Clamp(score, 1, 5);
        }

        public void SavePreSurveyImmediately()
        {
            Write(true);
        }

        public void SavePostSurveyImmediately()
        {
            Write(false);
        }

        void Write(bool prePass)
        {
            try
            {
                string code = new UserProfileTracker().CurrentEntranceCode;
                if (string.IsNullOrEmpty(code))
                    code = "UNKNOWN";

                string folder = Path.Combine(Application.persistentDataPath, FolderName);
                Directory.CreateDirectory(folder);
                string path = Path.Combine(folder, FileName);
                List<string[]> rows = ReadRows(path);
                string[] row = Find(rows, code);
                if (row == null)
                {
                    row = new string[9];
                    row[0] = code;
                    for (int i = 1; i < row.Length; i++)
                        row[i] = "";
                    rows.Add(row);
                }

                int[] source = prePass ? pre : post;
                int start = prePass ? 1 : 5;
                for (int i = 0; i < QuestionCount; i++)
                    row[start + i] = source[i] > 0 ? source[i].ToString() : "";

                var file = new StringBuilder();
                file.Append(Header).Append('\n');
                for (int i = 0; i < rows.Count; i++)
                {
                    string[] line = rows[i];
                    if (line == null || line.Length == 0 || string.IsNullOrEmpty(line[0]))
                        continue;
                    file.Append(line[0]);
                    for (int c = 1; c < 9; c++)
                    {
                        string cell = c < line.Length && line[c] != null ? line[c] : "";
                        file.Append(',').Append(cell);
                    }

                    file.Append('\n');
                }

                SafeFile.WriteAllText(path, file.ToString());
            }
            catch (Exception error)
            {
                Debug.LogWarning("The survey could not be saved. " + error.Message);
            }
        }

        static List<string[]> ReadRows(string path)
        {
            var rows = new List<string[]>();
            if (!SafeFile.Exists(path))
                return rows;

            string[] lines = (SafeFile.ReadAllText(path) ?? "").Replace("\r", "").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (i == 0 || string.IsNullOrWhiteSpace(lines[i]))
                    continue;
                rows.Add(lines[i].Split(','));
            }

            return rows;
        }

        static string[] Find(List<string[]> rows, string code)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                string[] row = rows[i];
                if (row != null && row.Length > 0 && string.Equals(row[0], code, StringComparison.OrdinalIgnoreCase))
                    return row;
            }

            return null;
        }
    }
}
