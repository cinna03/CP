using Capstone.App.Data;
using Capstone.App.Services;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Capstone.App.Tests
{
    public class ReplyRuleTests
    {
        static ArtworkData Work()
        {
            ArtworkData work = ScriptableObject.CreateInstance<ArtworkData>();
            var data = new SerializedObject(work);
            data.FindProperty("identifier").stringValue = "w1";
            data.FindProperty("title").stringValue = "Two swallows";
            data.FindProperty("artist").stringValue = "A. Painter";
            data.FindProperty("curatorNote").stringValue = "Movement and returning home.";
            data.ApplyModifiedPropertiesWithoutUndo();
            return work;
        }

        [TestCase("Who painted this?", "artist")]
        [TestCase("What inspired it?", "inspiration")]
        [TestCase("What is the meaning?", "story")]
        [TestCase("Igisobanuro cyayo ni ikihe?", "story")]
        [TestCase("Quelle est l'histoire ?", "story")]
        public void EachQuestionFindsItsRule(string question, string expected)
        {
            ReplyRule rule = StoryRules.Find(question);
            Assert.IsNotNull(rule);
            Assert.AreEqual(expected, rule.Name);
        }

        [Test]
        public void AQuestionOutsideStorytellingFindsNoRule()
        {
            Assert.IsNull(StoryRules.Find("How big is it?"));
            Assert.IsNull(StoryRules.Find(""));
        }

        [Test]
        public void EveryRuleAnswersThroughTheSameServicePolymorphically()
        {
            var replies = new ArtworkQueryService();
            ArtworkData work = Work();
            foreach (ReplyRule rule in StoryRules.InOrder)
                Assert.IsFalse(string.IsNullOrWhiteSpace(rule.Answer(work, replies)), rule.Name);
        }

        [Test]
        public void TheStoryRuleReturnsTheLabelledCuratorNote()
        {
            string reply = new StoryRule().Answer(Work(), new ArtworkQueryService());
            Assert.AreEqual(ArtworkQueryService.CuratorLabel + "Movement and returning home.", reply);
        }
    }
}
