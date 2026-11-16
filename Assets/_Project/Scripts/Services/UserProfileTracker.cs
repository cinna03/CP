using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Capstone.App.Services
{
    /// <summary>
    /// Stores the entrance-code registry in Application.persistentDataPath/UserProfiles/users_registry.json.
    /// </summary>
    [Serializable]
    public sealed class UserProfileTracker : IUserProfileTracker
    {
        const string FolderName = "UserProfiles";
        const string FileName = "users_registry.json";
        const string PrefsKey = "Capstone_UserRegistry";

        public string CurrentEntranceCode
        {
            get { return Read().ActiveCode ?? ""; }
        }

        public int TotalUniqueUsers
        {
            get
            {
                RegistryFile file = Read();
                return file.Users == null ? 0 : file.Users.Count;
            }
        }

        public int CurrentUserSessionCount
        {
            get
            {
                RegistryFile file = Read();
                UserProfileRecord record = Find(file, file.ActiveCode);
                return record == null ? 0 : record.TotalSessionsCount;
            }
        }

        public UserProfileRecord Register(string entranceCode)
        {
            string code = Normalize(entranceCode);
            if (code.Length == 0)
                throw new ArgumentException("An entrance code is required.", nameof(entranceCode));

            RegistryFile file = Read();
            if (file.Users == null)
                file.Users = new List<UserProfileRecord>();

            string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            UserProfileRecord record = Find(file, code);
            if (record == null)
            {
                record = new UserProfileRecord
                {
                    EntranceCode = code,
                    FirstVisitAt = now,
                    TotalSessionsCount = 0
                };
                file.Users.Add(record);
            }

            record.TotalSessionsCount += 1;
            record.LastVisitAt = now;
            file.ActiveCode = code;
            Write(file);
            return record;
        }

        public void ClearActiveUser()
        {
            RegistryFile file = Read();
            file.ActiveCode = "";
            Write(file);
        }

        /// <summary>The next unused study code: P01, then P02, and so on.</summary>
        public string NextParticipantCode()
        {
            RegistryFile file = Read();
            int highest = 0;
            if (file.Users != null)
            {
                for (int i = 0; i < file.Users.Count; i++)
                {
                    UserProfileRecord record = file.Users[i];
                    if (record == null)
                        continue;
                    int number = ParticipantNumber(Normalize(record.EntranceCode));
                    if (number > highest)
                        highest = number;
                }
            }

            return "P" + (highest + 1).ToString("00");
        }

        public static void Flush()
        {
            Write(Read());
        }

        public static string Normalize(string entranceCode)
        {
            if (string.IsNullOrWhiteSpace(entranceCode))
                return "";

            var kept = new StringBuilder();
            string trimmed = entranceCode.Trim();
            for (int i = 0; i < trimmed.Length; i++)
            {
                char letter = trimmed[i];
                if (char.IsWhiteSpace(letter) || letter == '-')
                    continue;
                kept.Append(char.ToUpperInvariant(letter));
            }

            return kept.ToString();
        }

        static int ParticipantNumber(string code)
        {
            if (code.Length < 2 || code[0] != 'P')
                return 0;

            int number;
            return int.TryParse(code.Substring(1), out number) ? number : 0;
        }

        static UserProfileRecord Find(RegistryFile file, string code)
        {
            if (file == null || file.Users == null || string.IsNullOrEmpty(code))
                return null;

            string key = Normalize(code);
            for (int i = 0; i < file.Users.Count; i++)
            {
                UserProfileRecord record = file.Users[i];
                if (record != null && Normalize(record.EntranceCode) == key)
                    return record;
            }

            return null;
        }

        static RegistryFile Read()
        {
            try
            {
                string path = Path.Combine(Folder(), FileName);
                if (SafeFile.Exists(path))
                {
                    RegistryFile file = SafeFile.ReadJson<RegistryFile>(path);
                    if (file != null)
                    {
                        if (file.Users == null)
                            file.Users = new List<UserProfileRecord>();
                        if (file.ActiveCode == null)
                            file.ActiveCode = "";
                        return file;
                    }
                }
            }
            catch (Exception error)
            {
                Debug.LogWarning("The user registry could not be read. A saved copy will be tried. " + error.Message);
            }

            RegistryFile backup = ReadPrefs();
            return backup ?? new RegistryFile();
        }

        static RegistryFile ReadPrefs()
        {
            if (!string.IsNullOrEmpty(DirectoryOverride))
                return null;

            try
            {
                string json = PlayerPrefs.GetString(PrefsKey, "");
                if (string.IsNullOrEmpty(json))
                    return null;

                RegistryFile file = JsonUtility.FromJson<RegistryFile>(json);
                if (file == null)
                    return null;
                if (file.Users == null)
                    file.Users = new List<UserProfileRecord>();
                if (file.ActiveCode == null)
                    file.ActiveCode = "";
                return file;
            }
            catch (Exception error)
            {
                Debug.LogWarning("The saved user list could not be read. " + error.Message);
                return null;
            }
        }

        static void Write(RegistryFile file)
        {
            if (file == null)
                file = new RegistryFile();
            string json = JsonUtility.ToJson(file, true);
            try
            {
                Directory.CreateDirectory(Folder());
                SafeFile.WriteAllText(Path.Combine(Folder(), FileName), json);
            }
            catch (Exception error)
            {
                Debug.LogWarning("The user registry could not be saved. " + error.Message);
            }

            if (!string.IsNullOrEmpty(DirectoryOverride))
                return;

            try
            {
                PlayerPrefs.SetString(PrefsKey, json);
                PlayerPrefs.Save();
            }
            catch (Exception error)
            {
                Debug.LogWarning("The user list could not be stored on this device. " + error.Message);
            }
        }

        public static string DirectoryOverride;

        static string Folder()
        {
            string root = string.IsNullOrEmpty(DirectoryOverride) ? Application.persistentDataPath : DirectoryOverride;
            return Path.Combine(root, FolderName);
        }

        [Serializable]
        sealed class RegistryFile
        {
            public string ActiveCode = "";
            public List<UserProfileRecord> Users = new List<UserProfileRecord>();
        }
    }

    /// <summary>The note the catalogue shows once after the art finder.</summary>
    public static class ArtFinderArrival
    {
        public static bool Pending;
        public static bool PinMatches;
        public static string Message = "";
    }
}
