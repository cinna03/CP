using Capstone.App.Data;
using Capstone.App.Services;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Capstone.App.Tests
{
    public class StorytellingTests
    {
        readonly ArtworkQueryService replies = new ArtworkQueryService();

        static ArtworkData Work(string artist, string story, string curator, string artistNote, string inspiration)
        {
            ArtworkData work = ScriptableObject.CreateInstance<ArtworkData>();
            var data = new SerializedObject(work);
            data.FindProperty("identifier").stringValue = "w1";
            data.FindProperty("title").stringValue = "Work";
            data.FindProperty("artist").stringValue = artist;
            data.FindProperty("storyNote").stringValue = story;
            data.FindProperty("curatorNote").stringValue = curator;
            data.FindProperty("artistNote").stringValue = artistNote;
            data.FindProperty("inspirationNote").stringValue = inspiration;
            data.ApplyModifiedPropertiesWithoutUndo();
            return work;
        }

        [Test]
        public void TheStoryIsTheCuratorNoteAndSaysSo()
        {
            string story = replies.DescribeStory(Work("", "For a living room.", "Two doves under roses.", "", ""));
            Assert.AreEqual(ArtworkQueryService.CuratorLabel + "Two doves under roses.", story);
        }

        [Test]
        public void WithoutACuratorNoteTheStoredNoteIsUsed()
        {
            Assert.AreEqual("For a living room.", replies.DescribeStory(Work("", "For a living room.", "", "", "")));
        }

        [Test]
        public void AMissingArtistNoteIsReportedNotInvented()
        {
            string reply = replies.DescribeArtist(Work("A. Painter", "", "Note.", "", ""));
            StringAssert.Contains("A. Painter", reply);
            StringAssert.Contains(ArtworkQueryService.NoArtistNote, reply);
        }

        [Test]
        public void TheArtistsOwnInspirationWinsOverTheCurator()
        {
            Assert.AreEqual("My grandmother's garden.", replies.DescribeInspiration(Work("", "", "Curator view.", "", "My grandmother's garden.")));
        }

        [Test]
        public void WithoutTheArtistTheInspirationReplyIsHonestAndLabelled()
        {
            string reply = replies.DescribeInspiration(Work("", "", "Curator view.", "", ""));
            StringAssert.StartsWith(ArtworkQueryService.NoInspiration, reply);
            StringAssert.Contains(ArtworkQueryService.CuratorLabel, reply);
        }
    }
}
