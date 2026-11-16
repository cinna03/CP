using System.IO;
using Capstone.App.Data;
using Capstone.App.Services;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Capstone.App.Tests
{
    public class UserProfileTrackerTests
    {
        string folder;
        ArtworkSessionState session;
        ArtworkData first;
        ArtworkData second;

        [SetUp]
        public void SetUp()
        {
            folder = Path.Combine(Application.temporaryCachePath, "CapstoneProfileTests");
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
            Directory.CreateDirectory(folder);
            UserProfileTracker.DirectoryOverride = folder;
            ArtworkSessionState.PersistOutsidePlay = true;
            session = ScriptableObject.CreateInstance<ArtworkSessionState>();
            first = Work("first");
            second = Work("second");
            PlayerPrefs.DeleteKey("Favorites_P01");
            PlayerPrefs.DeleteKey("Favorites_P02");
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey("Favorites_P01");
            PlayerPrefs.DeleteKey("Favorites_P02");
            ArtworkSessionState.PersistOutsidePlay = false;
            UserProfileTracker.DirectoryOverride = null;
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }

        [Test]
        public void SpacedAndHyphenatedCodesMatch()
        {
            Assert.AreEqual("P01", UserProfileTracker.Normalize(" p-01 "));
            Assert.AreEqual(UserProfileTracker.Normalize("P01"), UserProfileTracker.Normalize(" p-01 "));
        }

        [Test]
        public void AutoIdsIncreaseByOne()
        {
            var profiles = new UserProfileTracker();

            Assert.AreEqual("P01", profiles.NextParticipantCode());
            profiles.Register(" p-01 ");
            Assert.AreEqual("P02", profiles.NextParticipantCode());
        }

        [Test]
        public void NormalizedCodesKeepSeparateFavorites()
        {
            var profiles = new UserProfileTracker();
            ArtworkData[] catalogue = { first, second };

            profiles.Register(" p-01 ");
            session.SetShortlisted(first, true);
            profiles.Register("p-02");
            session.RestoreSaved(catalogue);
            session.SetShortlisted(second, true);
            profiles.Register("P01");
            session.RestoreSaved(catalogue);

            Assert.AreEqual(1, session.Shortlist.Count);
            Assert.AreEqual("first", session.Shortlist[0].Identifier);
            Assert.AreEqual("P01", profiles.CurrentEntranceCode);
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
