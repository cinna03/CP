using Capstone.App.Data;
using NUnit.Framework;

namespace Capstone.App.Tests
{
    public class StudyQuestionsTests
    {
        [Test]
        public void BeforeHasFourScreensForTheFourIntroSlides()
        {
            Assert.AreEqual(4, StudyQuestions.Before.Length);
            Assert.AreEqual(4, StudyQuestions.After("size").Length);
        }

        [Test]
        public void PairedRatingsUseTheSameSevenPointScale()
        {
            Assert.AreEqual(7, StudyQuestions.PreConfidence.Labels.Count);
            CollectionAssert.AreEqual(StudyQuestions.PreConfidence.Labels, StudyQuestions.PostConfidence.Labels);
            CollectionAssert.AreEqual(StudyQuestions.PreSizeClarity.Labels, StudyQuestions.PostSizeClarity.Labels);
            Assert.AreEqual(4, StudyQuestions.PreConfidence.Default);
        }

        [Test]
        public void SliderWordsMatchTheSpecAtOneFourAndSeven()
        {
            Assert.AreEqual("Very hesitant", StudyQuestions.PreConfidence.Describe(1));
            Assert.AreEqual("On the fence", StudyQuestions.PreConfidence.Describe(4));
            Assert.AreEqual("Completely ready", StudyQuestions.PreConfidence.Describe(7));
            Assert.AreEqual("Pure guesswork", StudyQuestions.PreSizeClarity.Describe(1));
            Assert.AreEqual("Crystal clear", StudyQuestions.PreSizeClarity.Describe(7));
        }

        [Test]
        public void ChoicesExportTheirCodesNotTheirWords()
        {
            Assert.AreEqual("room_fit", StudyQuestions.PreBlocker.Key(2));
            Assert.AreEqual(2, StudyQuestions.PreBlocker.Find("room_fit"));
            Assert.AreEqual("3", StudyQuestions.PreConfidence.Key(3));
            Assert.AreEqual("", StudyQuestions.PreBlocker.Key(9));
        }

        [Test]
        public void TheResolutionScreenQuotesTheBuyersOwnBlocker()
        {
            StringAssert.Contains("getting the size right", StudyQuestions.ResolutionPrompt("size"));
            Assert.AreSame(StudyQuestions.BlockerResolution, StudyQuestions.Resolution("size"));
        }

        [Test]
        public void ABuyerWithNoBlockerIsAskedAboutHesitationWithTheSameCodes()
        {
            Assert.AreSame(StudyQuestions.HesitationCheck, StudyQuestions.Resolution("none"));
            Assert.AreEqual("Did anything during this visit make you hesitate about buying?", StudyQuestions.ResolutionPrompt("none"));
            CollectionAssert.AreEqual(StudyQuestions.BlockerResolution.Keys, StudyQuestions.HesitationCheck.Keys);
            Assert.AreEqual(StudyQuestions.BlockerResolution.Id, StudyQuestions.HesitationCheck.Id);
        }

        [Test]
        public void FollowUpOnlyForLowConfidenceOrUnresolvedDoubt()
        {
            Assert.IsTrue(StudyQuestions.NeedsFollowUp(3, "resolved"));
            Assert.IsTrue(StudyQuestions.NeedsFollowUp(6, "unsure"));
            Assert.IsFalse(StudyQuestions.NeedsFollowUp(4, "somewhat"));
        }
    }

    public class StudySessionTests
    {
        [Test]
        public void DeltasAreAfterMinusBeforeAndNullWhenMissing()
        {
            var s = new StudySession { pre_confidence = 3, post_confidence = 6, pre_size_clarity = 2 };
            Assert.AreEqual(3, s.ConfidenceDelta);
            Assert.IsNull(s.SizeClarityDelta);
        }

        [Test]
        public void ExportUsesTheAgreedFieldsWithNullsForGaps()
        {
            var s = StudySession.Start("AZURE");
            s.pre_confidence = 3;
            s.post_confidence = 6;
            s.pre_blocker = "size";
            string json = s.ToJson();
            StringAssert.Contains("\"user_id\":\"AZURE\"", json);
            StringAssert.Contains("\"confidence_delta\":3", json);
            StringAssert.Contains("\"size_clarity_delta\":null", json);
            StringAssert.Contains("\"outlier_tags\":[]", json);
            StringAssert.Contains("\"optional_comment\":null", json);
            StringAssert.StartsWith("{\"session_id\":\"", json);
        }

        [Test]
        public void CsvQuotesCommentsWithCommas()
        {
            var s = StudySession.Start("AZURE");
            s.optional_comment = "big, but nice";
            StringAssert.Contains("\"big, but nice\"", s.ToCsv());
        }
    }
}
