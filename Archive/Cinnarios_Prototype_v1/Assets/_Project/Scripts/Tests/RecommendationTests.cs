using System.Collections.Generic;
using Capstone.App.Data;
using Capstone.App.Services;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Capstone.App.Tests
{
    public class RecommendationTests
    {
        [Test]
        public void KeepPrefersTheCalmerSmallerWorkForABedroom()
        {
            ArtworkData current = Work("current", "Quiet", "Calm", "Blue", 100f, 80f);
            ArtworkData small = Work("small", "Quiet", "Calm", "Blue", 30f, 30f);
            ArtworkData large = Work("large", "Quiet", "Calm", "Blue", 100f, 80f);
            var taste = new UserTasteProfile();
            taste.Remember(UserTasteProfile.Bedroom, "", "");
            taste.NoteDecision("", "Calm", "", 2f);
            var engine = new RecommendationEngine();

            IReadOnlyList<ArtworkData> three = engine.Three(new[] { large, small }, current, VisitDecision.Keep, null, taste);

            Assert.AreEqual("small", three[0].Identifier);
        }

        [Test]
        public void DismissInvertsTheMatchAndDropsDismissedIds()
        {
            ArtworkData current = Work("current", "Quiet", "Calm", "Blue", 40f, 50f);
            ArtworkData alike = Work("alike", "Quiet", "Calm", "Blue", 40f, 50f);
            ArtworkData other = Work("other", "Bold", "Bright", "Red", 40f, 50f);
            ArtworkData blocked = Work("blocked", "Bold", "Bright", "Red", 40f, 50f);
            var engine = new RecommendationEngine();

            IReadOnlyList<ArtworkData> three = engine.Three(new[] { alike, blocked, other }, current, VisitDecision.Dismiss, new[] { "blocked" }, null);

            Assert.AreEqual("other", three[0].Identifier);
            Assert.AreEqual(2, three.Count);
            for (int i = 0; i < three.Count; i++)
                Assert.AreNotEqual("blocked", three[i].Identifier);
        }

        static ArtworkData Work(string id, string style, string mood, string colour, float width, float height)
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
            sizes.GetArrayElementAtIndex(0).vector2Value = new Vector2(width, height);
            data.ApplyModifiedPropertiesWithoutUndo();
            return work;
        }
    }
}
