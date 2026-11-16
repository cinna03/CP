using System;
using System.Collections.Generic;
using System.IO;
using Capstone.App.Data;
using Capstone.App.Services;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Capstone.App.Tests
{
    public class UserIsolationTests
    {
        string folder;
        ArtworkSessionState session;
        ArtworkData first;
        ArtworkData second;

        [SetUp]
        public void SetUp()
        {
            folder = Path.Combine(Application.temporaryCachePath, "CapstoneUserTests");
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
            Directory.CreateDirectory(folder);
            UserProfileTracker.DirectoryOverride = folder;
            ArtworkSessionState.PersistOutsidePlay = true;
            session = ScriptableObject.CreateInstance<ArtworkSessionState>();
            first = Work("first");
            second = Work("second");
            PlayerPrefs.DeleteKey("Favorites_USERA");
            PlayerPrefs.DeleteKey("Favorites_USERB");
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey("Favorites_USERA");
            PlayerPrefs.DeleteKey("Favorites_USERB");
            ArtworkSessionState.PersistOutsidePlay = false;
            UserProfileTracker.DirectoryOverride = null;
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }

        [Test]
        public void TwoCodesKeepSeparateHeartsAndSessionCounts()
        {
            var profiles = new UserProfileTracker();
            ArtworkData[] catalogue = { first, second };

            profiles.Register("usera");
            session.SetShortlisted(first, true);

            profiles.Register("userb");
            session.RestoreSaved(catalogue);
            session.SetShortlisted(second, true);
            session.RestoreSaved(catalogue);
            Assert.AreEqual(1, session.Shortlist.Count);
            Assert.AreEqual("second", session.Shortlist[0].Identifier);

            profiles.Register("usera");
            session.RestoreSaved(catalogue);

            Assert.AreEqual(1, session.Shortlist.Count);
            Assert.AreEqual("first", session.Shortlist[0].Identifier);
            Assert.AreEqual(2, profiles.TotalUniqueUsers);
            Assert.AreEqual(2, profiles.CurrentUserSessionCount);
            string registry = File.ReadAllText(Path.Combine(folder, "UserProfiles", "users_registry.json"));
            RegistryPeek saved = JsonUtility.FromJson<RegistryPeek>(registry);
            Assert.AreEqual(2, saved.Users.Count);
            Assert.AreEqual(1, CountFor(saved, "USERB"));
        }

        static int CountFor(RegistryPeek saved, string code)
        {
            for (int i = 0; i < saved.Users.Count; i++)
            {
                if (saved.Users[i].EntranceCode == code)
                    return saved.Users[i].TotalSessionsCount;
            }

            return 0;
        }

        [Serializable]
        class RegistryPeek
        {
            public List<UserProfileRecord> Users;
        }

        static ArtworkData Work(string id)
        {
            ArtworkData work = ScriptableObject.CreateInstance<ArtworkData>();
            SerializedObject data = new SerializedObject(work);
            data.FindProperty("identifier").stringValue = id;
            data.FindProperty("title").stringValue = id;
            SerializedProperty sizes = data.FindProperty("availableSizes");
            sizes.arraySize = 1;
            sizes.GetArrayElementAtIndex(0).vector2Value = new Vector2(40f, 50f);
            data.ApplyModifiedPropertiesWithoutUndo();
            return work;
        }
    }
}
