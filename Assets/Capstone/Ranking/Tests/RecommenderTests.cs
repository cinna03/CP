using System.Collections.Generic;
using Capstone.Catalogue;
using NUnit.Framework;

namespace Capstone.Ranking.Tests
{
    public class RecommenderTests
    {
        [Test]
        public void TheClosestTagOverlapRanksFirst()
        {
            PreferenceProfile profile = PreferenceProfile.FromInitialTags("sample", "calm", "blue");

            List<Recommendation> ranked = Recommender.RankThree(SampleCatalogue.All, profile, "sample-wall");

            Assert.AreEqual(3, ranked.Count);
            Assert.AreEqual("sample-calm-blue", ranked[0].Artwork.Id);
            Assert.AreEqual(3, ranked[0].Score);
            Assert.AreEqual(1, ranked[0].Rank);
            Assert.AreEqual("sample-quiet", ranked[1].Artwork.Id);
            Assert.AreEqual(2, ranked[1].Score);
            Assert.AreEqual("sample-wide", ranked[2].Artwork.Id);
            Assert.AreEqual(2, ranked[2].Score);
        }

        [Test]
        public void TheActedOnWorkIsLeftOut()
        {
            PreferenceProfile profile = PreferenceProfile.FromInitialTags("sample", "calm", "blue");

            List<Recommendation> ranked = Recommender.RankThree(SampleCatalogue.All, profile, "sample-wall");

            foreach (Recommendation recommendation in ranked)
                Assert.AreNotEqual("sample-wall", recommendation.Artwork.Id);
        }

        [Test]
        public void AnEqualScoreUsesTheArtworkId()
        {
            PreferenceProfile profile = PreferenceProfile.FromInitialTags("sample", "calm", "blue");
            profile.UpdateFromFeedback(SampleCatalogue.All[0], FeedbackAction.Dismiss);

            List<Recommendation> ranked = Recommender.RankThree(SampleCatalogue.All, profile, "sample-wall");

            Assert.AreEqual(0, ranked[0].Score);
            Assert.AreEqual("sample-bold", ranked[0].Artwork.Id);
            Assert.AreEqual("sample-calm-blue", ranked[1].Artwork.Id);
            Assert.AreEqual("sample-quiet", ranked[2].Artwork.Id);
        }

        [Test]
        public void KeepStrengthensTheSharedTags()
        {
            FeedbackLoop loop = new FeedbackLoop(PreferenceProfile.FromInitialTags("sample", "calm", "blue"));
            loop.Accept(SampleCatalogue.All[0]);

            List<Recommendation> ranked = Recommender.RankThree(SampleCatalogue.All, loop.Profile, "sample-wall");

            Assert.AreEqual("Keep", loop.Profile.LastAction);
            Assert.AreEqual(2, loop.Profile.StyleWeight("sample"));
            Assert.AreEqual("sample-calm-blue", ranked[0].Artwork.Id);
            Assert.AreEqual(6, ranked[0].Score);
        }

        [Test]
        public void SaveStrengthensTheSameWayAsKeep()
        {
            FeedbackLoop saved = new FeedbackLoop(PreferenceProfile.FromInitialTags("sample", "calm", "blue"));
            FeedbackLoop kept = new FeedbackLoop(PreferenceProfile.FromInitialTags("sample", "calm", "blue"));
            saved.Save(SampleCatalogue.All[0]);
            kept.Accept(SampleCatalogue.All[0]);

            Assert.AreEqual(
                Recommender.Score(SampleCatalogue.All[2], kept.Profile),
                Recommender.Score(SampleCatalogue.All[2], saved.Profile));
        }

        [Test]
        public void TwoDismissalsDropTheClosestMatchOutOfTheTopThree()
        {
            FeedbackLoop loop = new FeedbackLoop(PreferenceProfile.FromInitialTags("sample", "calm", "blue"));
            loop.Dismiss(SampleCatalogue.All[0]);
            loop.Dismiss(SampleCatalogue.All[0]);

            List<Recommendation> ranked = Recommender.RankThree(SampleCatalogue.All, loop.Profile, "sample-wall");

            Assert.AreEqual("sample-bold", ranked[0].Artwork.Id);
            Assert.AreEqual(0, ranked[0].Score);
            Assert.AreEqual("sample-quiet", ranked[1].Artwork.Id);
            Assert.AreEqual(-2, ranked[1].Score);
            foreach (Recommendation recommendation in ranked)
                Assert.AreNotEqual("sample-calm-blue", recommendation.Artwork.Id);
        }

        [Test]
        public void AlternativesWeakenTheSameWayAsDismiss()
        {
            FeedbackLoop alternatives = new FeedbackLoop(PreferenceProfile.FromInitialTags("sample", "calm", "blue"));
            FeedbackLoop dismissed = new FeedbackLoop(PreferenceProfile.FromInitialTags("sample", "calm", "blue"));
            alternatives.RequestAlternatives(SampleCatalogue.All[0]);
            dismissed.Dismiss(SampleCatalogue.All[0]);

            Assert.AreEqual(
                alternatives.Profile.ColourWeight("blue"),
                dismissed.Profile.ColourWeight("blue"));
            Assert.AreEqual(0, alternatives.Profile.ColourWeight("blue"));
        }

        [Test]
        public void AColourConcernStrengthensThatColour()
        {
            PreferenceProfile profile = PreferenceProfile.FromInitialTags("sample", "calm", "blue");
            profile.NoteConcern("colour", SampleCatalogue.All[0]);

            List<Recommendation> ranked = Recommender.RankThree(SampleCatalogue.All, profile, "sample-wall");

            Assert.AreEqual("colour", profile.LastConcern);
            Assert.AreEqual(2, profile.ColourWeight("blue"));
            Assert.AreEqual(1, profile.StyleWeight("sample"));
            Assert.AreEqual("sample-calm-blue", ranked[0].Artwork.Id);
            Assert.AreEqual(4, ranked[0].Score);
        }

        [Test]
        public void ASizeConcernIsStoredAndDoesNotChangeTheTags()
        {
            PreferenceProfile profile = PreferenceProfile.FromInitialTags("sample", "calm", "blue");
            profile.NoteConcern("size", SampleCatalogue.All[0]);

            Assert.AreEqual("size", profile.LastConcern);
            Assert.AreEqual(1, profile.StyleWeight("sample"));
            Assert.AreEqual(1, profile.MoodWeight("calm"));
            Assert.AreEqual(1, profile.ColourWeight("blue"));
        }

        [Test]
        public void FewerThanThreeOthersReturnsFewer()
        {
            List<Artwork> catalogue = new List<Artwork> { SampleCatalogue.All[0], SampleCatalogue.All[4] };

            List<Recommendation> ranked = Recommender.RankThree(catalogue, PreferenceProfile.FromInitialTags("sample", "calm", "blue"), "sample-wall");

            Assert.AreEqual(1, ranked.Count);
            Assert.AreEqual("sample-bold", ranked[0].Artwork.Id);
            Assert.AreEqual(0, ranked[0].Score);
        }

        [Test]
        public void TheSameProfileAlwaysReturnsTheSameThree()
        {
            PreferenceProfile profile = PreferenceProfile.FromInitialTags("sample", "calm", "blue");

            List<Recommendation> first = Recommender.RankThree(SampleCatalogue.All, profile, "sample-wall");
            List<Recommendation> second = Recommender.RankThree(SampleCatalogue.All, profile, "sample-wall");

            Assert.AreEqual(first[0].Artwork.Id, second[0].Artwork.Id);
            Assert.AreEqual(first[1].Artwork.Id, second[1].Artwork.Id);
            Assert.AreEqual(first[2].Artwork.Id, second[2].Artwork.Id);
        }

        [Test]
        public void TheBuyerCanChooseOneOfTheThree()
        {
            List<Recommendation> ranked = Recommender.RankThree(
                SampleCatalogue.All,
                PreferenceProfile.FromInitialTags("sample", "calm", "blue"),
                "sample-wall");

            Assert.AreEqual("sample-calm-blue", Recommender.Choose(ranked, 1).Id);
            Assert.AreEqual("sample-quiet", Recommender.Choose(ranked, 2).Id);
            Assert.AreEqual("sample-wide", Recommender.Choose(ranked, 3).Id);
        }

        [Test]
        public void ARankThatWasNotOfferedIsRejected()
        {
            List<Recommendation> ranked = Recommender.RankThree(
                SampleCatalogue.All,
                PreferenceProfile.FromInitialTags("sample", "calm", "blue"),
                "sample-wall");

            Assert.IsNull(Recommender.Choose(ranked, 0));
            Assert.IsNull(Recommender.Choose(ranked, 4));
            Assert.IsNull(Recommender.Choose(null, 1));
        }
    }
}
