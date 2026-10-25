using System.Collections.Generic;
using NUnit.Framework;

namespace Capstone.Dialogue.Tests
{
    public class RuleEngineTests
    {
        [Test]
        public void BigMatchesTheEnglishSizeReply()
        {
            DialogueReply reply = RuleEngine.Match("Is it BIG?", StarterRules.All);

            Assert.AreEqual("size", reply.RuleId);
            Assert.AreEqual("en", reply.Language);
            Assert.IsFalse(ContainsDigit(reply.Text));
        }

        [Test]
        public void GrandMatchesTheFrenchSizeReply()
        {
            DialogueReply reply = RuleEngine.Match("C'est grand?", StarterRules.All);

            Assert.AreEqual("size", reply.RuleId);
            Assert.AreEqual("fr", reply.Language);
            StringAssert.StartsWith("La taille", reply.Text);
        }

        [Test]
        public void InganoMatchesTheKinyarwandaSizeReply()
        {
            DialogueReply reply = RuleEngine.Match("ingano?", StarterRules.All);

            Assert.AreEqual("size", reply.RuleId);
            Assert.AreEqual("rw", reply.Language);
        }

        [Test]
        public void EmptyTextReturnsTheFixedPrompt()
        {
            DialogueReply reply = RuleEngine.Match("   ", StarterRules.All);

            Assert.IsFalse(reply.Matched);
            Assert.AreEqual(RuleEngine.FixedPrompt, reply.Text);
        }

        [Test]
        public void AnUnknownQuestionDoesNotInventAFact()
        {
            DialogueReply reply = RuleEngine.Match("What is the artist's dog's name?", StarterRules.All);

            Assert.IsFalse(reply.Matched);
            Assert.AreEqual(RuleEngine.FixedPrompt, reply.Text);
            Assert.IsFalse(reply.Text.Contains("dog"));
        }

        [Test]
        public void TwoRulesAtOnceUseTheHigherPriority()
        {
            DialogueReply reply = RuleEngine.Match("big and expensive", StarterRules.All);

            Assert.AreEqual("size", reply.RuleId);
            Assert.AreEqual("en", reply.Language);
        }

        [Test]
        public void ALanguageTieUsesTheEarliestMatchedWord()
        {
            DialogueRule[] rules =
            {
                new DialogueRule(
                    "size",
                    "size",
                    1,
                    new[] { "size" },
                    "english",
                    new[] { "taille" },
                    "french",
                    new string[0],
                    string.Empty),
            };

            DialogueReply reply = RuleEngine.Match("taille size", rules);

            Assert.AreEqual("fr", reply.Language);
            Assert.AreEqual("french", reply.Text);
        }

        [Test]
        public void ATieUsesTheLanguageOfTheWinningKeywords()
        {
            DialogueReply reply = RuleEngine.Match("taille and price", StarterRules.All);

            Assert.AreEqual("size", reply.RuleId);
            Assert.AreEqual("fr", reply.Language);
        }

        [Test]
        public void TheSameSentenceAlwaysReturnsTheSameReply()
        {
            DialogueReply first = RuleEngine.Match("too small for the wall", StarterRules.All);
            DialogueReply second = RuleEngine.Match("too small for the wall", StarterRules.All);

            Assert.AreEqual(first.RuleId, second.RuleId);
            Assert.AreEqual(first.Language, second.Language);
            Assert.AreEqual(first.Text, second.Text);
        }

        [Test]
        public void MostMatchingWordsWinOverPriority()
        {
            List<DialogueRule> rules = new List<DialogueRule>
            {
                new DialogueRule("size", "size", 5, new[] { "big" }, "size", new string[0], "", new string[0], ""),
                new DialogueRule("price", "price", 1, new[] { "cheap", "cost" }, "price", new string[0], "", new string[0], ""),
            };

            DialogueReply reply = RuleEngine.Match("cheap cost", rules);

            Assert.AreEqual("price", reply.RuleId);
        }

        [Test]
        public void LightingMatchesTheEnglishLightReply()
        {
            DialogueReply reply = RuleEngine.Match("Is the lighting dark?", StarterRules.All);

            Assert.AreEqual("light", reply.RuleId);
            Assert.AreEqual("en", reply.Language);
            Assert.IsFalse(ContainsDigit(reply.Text));
        }

        [Test]
        public void ConvientMatchesTheFrenchFitReply()
        {
            DialogueReply reply = RuleEngine.Match("Est-ce que cela convient?", StarterRules.All);

            Assert.AreEqual("fit", reply.RuleId);
            Assert.AreEqual("fr", reply.Language);
        }

        [Test]
        public void IgisobanuroMatchesTheKinyarwandaMeaningReply()
        {
            DialogueReply reply = RuleEngine.Match("igisobanuro", StarterRules.All);

            Assert.AreEqual("meaning", reply.RuleId);
            Assert.AreEqual("rw", reply.Language);
        }

        [Test]
        public void ATieStillFollowsTheConcernOrder()
        {
            Assert.AreEqual("size", RuleEngine.Match("big and fit", StarterRules.All).RuleId);
            Assert.AreEqual("fit", RuleEngine.Match("fit and expensive", StarterRules.All).RuleId);
        }

        static bool ContainsDigit(string text)
        {
            foreach (char character in text)
            {
                if (char.IsDigit(character))
                    return true;
            }

            return false;
        }
    }
}
