using System;

namespace Capstone.App.Services
{
    /// <summary>
    /// One person on this device, identified by their entrance code.
    /// </summary>
    [Serializable]
    public sealed class UserProfileRecord
    {
        public string EntranceCode = "";
        public string FirstVisitAt = "";
        public string LastVisitAt = "";
        public int TotalSessionsCount;
    }

    /// <summary>
    /// Remembers who has entered a code on this device, and which code is active now.
    /// </summary>
    public interface IUserProfileTracker
    {
        string CurrentEntranceCode { get; }
        int TotalUniqueUsers { get; }
        int CurrentUserSessionCount { get; }
        UserProfileRecord Register(string entranceCode);
        void ClearActiveUser();
    }
}
