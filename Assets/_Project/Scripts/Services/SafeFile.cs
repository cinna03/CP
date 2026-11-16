using System;
using System.IO;
using UnityEngine;

namespace Capstone.App.Services
{
    /// <summary>
    /// Crash-safe file storage for everything the app keeps on the phone.
    /// A write goes to a temporary file first and then replaces the old file, keeping the
    /// previous version as "&lt;name&gt;.bak". A reader that finds the main file missing,
    /// empty, or unreadable falls back to that backup, so a crash or a full disk during a
    /// study session cannot wipe the participant data that was already saved.
    /// </summary>
    public static class SafeFile
    {
        public const string BackupSuffix = ".bak";
        const string TempSuffix = ".tmp";

        public static void WriteAllText(string path, string contents)
        {
            Commit(path, temp => File.WriteAllText(temp, contents ?? ""));
        }

        public static void WriteAllBytes(string path, byte[] contents)
        {
            Commit(path, temp => File.WriteAllBytes(temp, contents ?? Array.Empty<byte>()));
        }

        /// <summary>The main file's text, or the backup's when the main file is missing or empty. Null when neither exists.</summary>
        public static string ReadAllText(string path)
        {
            string main = TryRead(path);
            if (!string.IsNullOrWhiteSpace(main))
                return main;
            string backup = TryRead(path + BackupSuffix);
            return string.IsNullOrWhiteSpace(backup) ? main : backup;
        }

        /// <summary>
        /// Reads JSON into T from the main file and, if that fails to parse or is rejected
        /// by <paramref name="isValid"/>, from the backup. Returns null when neither works.
        /// </summary>
        public static T ReadJson<T>(string path, Func<T, bool> isValid = null) where T : class
        {
            T value = Parse<T>(TryRead(path), isValid);
            if (value != null)
                return value;
            T recovered = Parse<T>(TryRead(path + BackupSuffix), isValid);
            if (recovered != null)
                Debug.LogWarning("SafeFile: recovered " + Path.GetFileName(path) + " from its backup.");
            return recovered;
        }

        public static bool Exists(string path)
        {
            return File.Exists(path) || File.Exists(path + BackupSuffix);
        }

        static T Parse<T>(string json, Func<T, bool> isValid) where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;
            try
            {
                T value = JsonUtility.FromJson<T>(json);
                if (value == null)
                    return null;
                return isValid == null || isValid(value) ? value : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        static string TryRead(string path)
        {
            try
            {
                return File.Exists(path) ? File.ReadAllText(path) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        static void Commit(string path, Action<string> writeTemp)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("A file path is required.", nameof(path));
            string folder = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(folder))
                Directory.CreateDirectory(folder);

            string temp = path + TempSuffix;
            string backup = path + BackupSuffix;
            writeTemp(temp);

            if (File.Exists(path))
            {
                File.Copy(path, backup, true);
                File.Delete(path);
            }
            File.Move(temp, path);
        }
    }
}
