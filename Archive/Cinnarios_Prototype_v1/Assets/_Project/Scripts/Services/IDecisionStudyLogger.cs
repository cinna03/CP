using System;
using System.Collections.Generic;

namespace Capstone.App.Services
{
    /// <summary>
    /// One confidence answer from a wall visit. A skipped check-in keeps a score of 0.
    /// </summary>
    [Serializable]
    public sealed class DecisionConfidenceEntry
    {
        public string EntranceCode = "";
        public int TotalUniqueUsersOnDevice;
        public int UserSessionNumber;
        public string SessionId = "";
        public string Timestamp = "";
        public int TotalWorksTried;
        public int KeptWorksCount;
        public string FinalSelectedArtworkTitle = "";
        public string FinalChosenDimensionsCm = "";
        public string FinalChosenFrame = "";
        public int QuestionsAskedInSession;
        public string LearnedRoomContext = "Unspecified";
        public int ConfidenceScore;
        public string PrimaryConfidenceDriver = "Unspecified";
    }

    /// <summary>
    /// Records confidence answers on this device. The wall card only supplies the entry.
    /// </summary>
    public interface IDecisionStudyLogger
    {
        string SessionId { get; }
        void LogConfidenceDecision(DecisionConfidenceEntry entry);
        IReadOnlyList<DecisionConfidenceEntry> GetAllEntries();
        float GetAverageConfidenceScore();
    }
}
