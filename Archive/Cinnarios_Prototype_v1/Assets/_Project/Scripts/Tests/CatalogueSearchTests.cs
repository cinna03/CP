using System.Collections.Generic;
using Capstone.App.Data;
using Capstone.App.Services;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Capstone.App.Tests
{
    public class CatalogueSearchTests
    {
        [Test]
        public void TwoWordsRankADualMatchAboveASingleMatch()
        {
            ArtworkData both = Work("both", "Quiet", "Calm", "Blue");
            ArtworkData calm = Work("calm", "Quiet", "Calm", "Red");
            ArtworkData blue = Work("blue", "Bold", "Bright", "Blue");
            var service = new CatalogueFilterService();

            IReadOnlyList<ArtworkData> found = service.Matching(new[] { calm, blue, both }, new CatalogueQuery { Search = "calm blue" });

            Assert.AreEqual(3, found.Count);
            Assert.AreEqual("both", found[0].Identifier);
        }

        [Test]
        public void UnknownWordsReturnNoWorks()
        {
            ArtworkData calm = Work("calm", "Quiet", "Calm", "Blue");
            var service = new CatalogueFilterService();

            IReadOnlyList<ArtworkData> found = service.Matching(new[] { calm }, new CatalogueQuery { Search = "zzqxv" });

            Assert.AreEqual(0, found.Count);
        }

        static ArtworkData Work(string id, string style, string mood, string colour)
        {
            ArtworkData work = ScriptableObject.CreateInstance<ArtworkData>();
            SerializedObject data = new SerializedObject(work);
            data.FindProperty("identifier").stringValue = id;
            data.FindProperty("title").stringValue = id;
            data.FindProperty("style").stringValue = style;
            data.FindProperty("mood").stringValue = mood;
            data.FindProperty("colour").stringValue = colour;
            SerializedProperty sizes = data.FindProperty("availableSizes");
            sizes.arraySize = 1;
            sizes.GetArrayElementAtIndex(0).vector2Value = new Vector2(40f, 50f);
            data.ApplyModifiedPropertiesWithoutUndo();
            return work;
        }
    }
}
