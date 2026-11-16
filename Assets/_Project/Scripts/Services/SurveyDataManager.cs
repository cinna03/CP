using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Capstone.App.Data;
using UnityEngine;

namespace Capstone.App.Services
{
    /// <summary>
    /// Records the before/after assessment and keeps every buyer's sessions on the phone (StudyLogs/):
    ///   study_store.json      the working store (read back on launch, written atomically)
    ///   study_sessions.json   the agreed flat export, one object per session, nulls for gaps
    ///   study_sessions.csv    the same rows for Excel
    /// The "after" answers attach to the buyer's latest session that has no "after" yet.
    /// </summary>
    public sealed class SurveyDataManager : MonoBehaviour
    {
        const string FolderName = "StudyLogs";
        const string StoreFile = "study_store.json";
        const string ExportFile = "study_sessions.json";
        const string CsvFile = "study_sessions.csv";

        [Serializable]
        sealed class Store
        {
            public List<StudySession> sessions = new List<StudySession>();
        }

        public static SurveyDataManager Instance { get; private set; }

        readonly Dictionary<string, int> answers = new Dictionary<string, int>();
        Store store;

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

        /// <summary>Remembers an answer for this visit (saved with SaveBefore / SaveAfter).</summary>
        public void Record(StudyQuestion question, int answer)
        {
            if (question != null && question.Accepts(answer))
                answers[question.Id] = answer;
        }

        public int Answer(StudyQuestion question)
        {
            return question != null && answers.TryGetValue(question.Id, out int value) ? value : 0;
        }

        /// <summary>True when this buyer already has a session with "before" answers and no "after" yet.</summary>
        public bool BeforeAlreadyGiven()
        {
            return Open() != null;
        }

        /// <summary>The buyer's own blocker from the open session (or this visit), as its code.</summary>
        public string PreBlocker()
        {
            int here = Answer(StudyQuestions.PreBlocker);
            if (here > 0)
                return StudyQuestions.PreBlocker.Key(here);
            StudySession open = Open();
            return open != null ? open.pre_blocker : null;
        }

        public StudySession SaveBefore()
        {
            StudySession session = StudySession.Start(CurrentUser());
            session.pre_timestamp = StudySession.Now();
            session.pre_confidence = Answer(StudyQuestions.PreConfidence);
            session.pre_size_clarity = Answer(StudyQuestions.PreSizeClarity);
            session.pre_blocker = Key(StudyQuestions.PreBlocker);
            session.pre_intent = Key(StudyQuestions.PreIntent);
            Load().sessions.Add(session);
            Persist();
            return session;
        }

        public StudySession SaveAfter(ICollection<string> outlierTags, string comment)
        {
            StudySession session = Open();
            if (session == null)
            {
                session = StudySession.Start(CurrentUser());
                Load().sessions.Add(session);
            }

            session.post_timestamp = StudySession.Now();
            session.post_confidence = Answer(StudyQuestions.PostConfidence);
            session.post_size_clarity = Answer(StudyQuestions.PostSizeClarity);
            session.post_blocker_resolution = Key(StudyQuestions.BlockerResolution);
            session.post_helpful_feature = Key(StudyQuestions.PostHelpfulFeature);
            session.outlier_tags = outlierTags != null ? new List<string>(outlierTags) : new List<string>();
            session.optional_comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
            Persist();
            ClearAfterAnswers();
            return session;
        }

        void ClearAfterAnswers()
        {
            answers.Remove(StudyQuestions.PostConfidence.Id);
            answers.Remove(StudyQuestions.PostSizeClarity.Id);
            answers.Remove(StudyQuestions.BlockerResolution.Id);
            answers.Remove(StudyQuestions.PostHelpfulFeature.Id);
        }

        /// <summary>Every saved session, newest last.</summary>
        public IReadOnlyList<StudySession> Sessions => Load().sessions;

        StudySession Open()
        {
            string user = CurrentUser();
            List<StudySession> sessions = Load().sessions;
            for (int i = sessions.Count - 1; i >= 0; i--)
            {
                StudySession s = sessions[i];
                if (s != null && string.Equals(s.user_id, user, StringComparison.OrdinalIgnoreCase) && s.HasBefore && !s.HasAfter)
                    return s;
            }

            return null;
        }

        string Key(StudyQuestion question)
        {
            string key = question.Key(Answer(question));
            return string.IsNullOrEmpty(key) ? null : key;
        }

        Store Load()
        {
            if (store != null)
                return store;
            store = SafeFile.ReadJson<Store>(Path.Combine(Folder(), StoreFile), s => s != null && s.sessions != null) ?? new Store();
            return store;
        }

        void Persist()
        {
            try
            {
                string folder = Folder();
                SafeFile.WriteAllText(Path.Combine(folder, StoreFile), JsonUtility.ToJson(store, true));

                var json = new StringBuilder("[\n");
                var csv = new StringBuilder(StudySession.CsvHeader).Append('\n');
                for (int i = 0; i < store.sessions.Count; i++)
                {
                    json.Append("  ").Append(store.sessions[i].ToJson()).Append(i < store.sessions.Count - 1 ? ",\n" : "\n");
                    csv.Append(store.sessions[i].ToCsv()).Append('\n');
                }

                SafeFile.WriteAllText(Path.Combine(folder, ExportFile), json.Append("]\n").ToString());
                SafeFile.WriteAllText(Path.Combine(folder, CsvFile), csv.ToString());
            }
            catch (Exception error)
            {
                Debug.LogWarning("The study answers could not be saved. " + error.Message);
            }
        }

        static string CurrentUser()
        {
            string code = new UserProfileTracker().CurrentEntranceCode;
            return string.IsNullOrEmpty(code) ? "UNKNOWN" : code;
        }

        static string Folder()
        {
            string folder = Path.Combine(Application.persistentDataPath, FolderName);
            Directory.CreateDirectory(folder);
            return folder;
        }
    }
}
