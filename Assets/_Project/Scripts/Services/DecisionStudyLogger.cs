using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Capstone.App.Services
{
    /// <summary>
    /// Appends each confidence answer to a CSV file and keeps the same rows in a JSON summary.
    /// One session id lasts for this launch of the app.
    /// </summary>
    [Serializable]
    public sealed class DecisionStudyLogger : IDecisionStudyLogger
    {
        const string FolderName = "StudyLogs";
        const string CsvName = "confidence_study_log.csv";
        const string JsonName = "confidence_study_summary.json";
        const string Header = "EntranceCode,TotalUniqueUsersOnDevice,UserSessionNumber,SessionId,Timestamp,TotalWorksTried,KeptWorksCount,FinalSelectedArtworkTitle,FinalChosenDimensionsCm,FinalChosenFrame,QuestionsAskedInSession,LearnedRoomContext,ConfidenceScore,PrimaryConfidenceDriver";

        static readonly string launchId = Guid.NewGuid().ToString("N");

        List<DecisionConfidenceEntry> entries;
        bool loaded;

        public string SessionId
        {
            get { return launchId; }
        }

        public void LogConfidenceDecision(DecisionConfidenceEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            Ensure();
            if (string.IsNullOrEmpty(entry.SessionId))
                entry.SessionId = launchId;
            if (string.IsNullOrEmpty(entry.Timestamp))
                entry.Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            if (string.IsNullOrEmpty(entry.LearnedRoomContext))
                entry.LearnedRoomContext = "Unspecified";
            if (string.IsNullOrEmpty(entry.PrimaryConfidenceDriver))
                entry.PrimaryConfidenceDriver = "Unspecified";

            var profiles = new UserProfileTracker();
            if (string.IsNullOrEmpty(entry.EntranceCode))
                entry.EntranceCode = profiles.CurrentEntranceCode;
            entry.TotalUniqueUsersOnDevice = profiles.TotalUniqueUsers;
            entry.UserSessionNumber = profiles.CurrentUserSessionCount;

            entries.Add(entry);
            try
            {
                Directory.CreateDirectory(Folder());
                WriteCsv();
                File.WriteAllText(JsonPath(), JsonUtility.ToJson(new EntryFile { Items = entries }, true));
            }
            catch (Exception error)
            {
                Debug.LogWarning("The study log could not be saved. " + error.Message);
            }
        }

        public IReadOnlyList<DecisionConfidenceEntry> GetAllEntries()
        {
            Ensure();
            return entries;
        }

        public float GetAverageConfidenceScore()
        {
            Ensure();
            int count = 0;
            int total = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                DecisionConfidenceEntry entry = entries[i];
                if (entry == null || entry.ConfidenceScore < 1 || entry.ConfidenceScore > 5)
                    continue;
                total += entry.ConfidenceScore;
                count++;
            }

            if (count == 0)
                return 0f;
            return total / (float)count;
        }

        void Ensure()
        {
            if (loaded)
                return;

            loaded = true;
            entries = new List<DecisionConfidenceEntry>();
            if (!File.Exists(JsonPath()))
                return;

            try
            {
                EntryFile file = JsonUtility.FromJson<EntryFile>(File.ReadAllText(JsonPath()));
                if (file != null && file.Items != null)
                    entries = file.Items;
            }
            catch (Exception error)
            {
                Debug.LogWarning("The study summary could not be read. " + error.Message);
            }
        }

        void WriteCsv()
        {
            var file = new StringBuilder();
            file.Append(Header).Append('\n');
            for (int i = 0; i < entries.Count; i++)
            {
                DecisionConfidenceEntry entry = entries[i];
                if (entry == null)
                    continue;
                file.Append(Cell(entry.EntranceCode)).Append(',');
                file.Append(entry.TotalUniqueUsersOnDevice.ToString()).Append(',');
                file.Append(entry.UserSessionNumber.ToString()).Append(',');
                file.Append(Cell(entry.SessionId)).Append(',');
                file.Append(Cell(entry.Timestamp)).Append(',');
                file.Append(entry.TotalWorksTried.ToString()).Append(',');
                file.Append(entry.KeptWorksCount.ToString()).Append(',');
                file.Append(Cell(entry.FinalSelectedArtworkTitle)).Append(',');
                file.Append(Cell(entry.FinalChosenDimensionsCm)).Append(',');
                file.Append(Cell(entry.FinalChosenFrame)).Append(',');
                file.Append(entry.QuestionsAskedInSession.ToString()).Append(',');
                file.Append(Cell(entry.LearnedRoomContext)).Append(',');
                file.Append(entry.ConfidenceScore.ToString()).Append(',');
                file.Append(Cell(entry.PrimaryConfidenceDriver)).Append('\n');
            }

            try
            {
                File.WriteAllText(CsvPath(), file.ToString());
            }
            catch (Exception error)
            {
                Debug.LogWarning("The study log could not be saved. " + error.Message);
            }
        }

        static string Cell(string value)
        {
            string text = value ?? "";
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }

        static string Folder()
        {
            return Path.Combine(Application.persistentDataPath, FolderName);
        }

        static string CsvPath()
        {
            return Path.Combine(Folder(), CsvName);
        }

        static string JsonPath()
        {
            return Path.Combine(Folder(), JsonName);
        }

        [Serializable]
        sealed class EntryFile
        {
            public List<DecisionConfidenceEntry> Items = new List<DecisionConfidenceEntry>();
        }
    }
}
