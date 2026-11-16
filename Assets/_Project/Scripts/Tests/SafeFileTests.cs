using System;
using System.IO;
using Capstone.App.Services;
using NUnit.Framework;

namespace Capstone.App.Tests
{
    public class SafeFileTests
    {
        [Serializable]
        class Record
        {
            public string Code = "";
            public int Count;
        }

        string folder;

        [SetUp]
        public void SetUp()
        {
            folder = Path.Combine(Path.GetTempPath(), "SafeFileTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }

        [Test]
        public void WrittenTextReadsBack()
        {
            string path = Path.Combine(folder, "a.json");
            SafeFile.WriteAllText(path, "hello");
            Assert.AreEqual("hello", SafeFile.ReadAllText(path));
            Assert.IsFalse(File.Exists(path + ".tmp"));
        }

        [Test]
        public void OverwritingKeepsThePreviousVersionAsBackup()
        {
            string path = Path.Combine(folder, "a.json");
            SafeFile.WriteAllText(path, "first");
            SafeFile.WriteAllText(path, "second");
            Assert.AreEqual("second", File.ReadAllText(path));
            Assert.AreEqual("first", File.ReadAllText(path + SafeFile.BackupSuffix));
        }

        [Test]
        public void CorruptedJsonRecoversFromBackup()
        {
            string path = Path.Combine(folder, "log.json");
            SafeFile.WriteAllText(path, UnityEngine.JsonUtility.ToJson(new Record { Code = "AZURE", Count = 3 }));
            SafeFile.WriteAllText(path, UnityEngine.JsonUtility.ToJson(new Record { Code = "AZURE", Count = 4 }));
            File.WriteAllText(path, "{ this is not json");
            Record record = SafeFile.ReadJson<Record>(path);
            Assert.IsNotNull(record);
            Assert.AreEqual(3, record.Count);
        }

        [Test]
        public void AnEmptyMainFileFallsBackToBackupText()
        {
            string path = Path.Combine(folder, "a.csv");
            SafeFile.WriteAllText(path, "row1");
            SafeFile.WriteAllText(path, "row1\nrow2");
            File.WriteAllText(path, "");
            Assert.AreEqual("row1", SafeFile.ReadAllText(path));
        }

        [Test]
        public void AMissingFileReadsAsNull()
        {
            string path = Path.Combine(folder, "missing.json");
            Assert.IsNull(SafeFile.ReadJson<Record>(path));
            Assert.IsNull(SafeFile.ReadAllText(path));
            Assert.IsFalse(SafeFile.Exists(path));
        }

        [Test]
        public void AValidatorCanRejectTheMainFile()
        {
            string path = Path.Combine(folder, "a.json");
            SafeFile.WriteAllText(path, UnityEngine.JsonUtility.ToJson(new Record { Code = "AZURE", Count = 1 }));
            SafeFile.WriteAllText(path, UnityEngine.JsonUtility.ToJson(new Record { Code = "", Count = 2 }));
            Record record = SafeFile.ReadJson<Record>(path, r => !string.IsNullOrEmpty(r.Code));
            Assert.AreEqual(1, record.Count);
        }
    }
}
