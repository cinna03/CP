using NUnit.Framework;

namespace Capstone.Session.Tests
{
    public class PreferenceQuizTests
    {
        [Test]
        public void AStyleAnswerStoresOnlyThatTag()
        {
            PreferenceQuiz quiz = new PreferenceQuiz();

            quiz.Answer("bold");

            Assert.AreEqual("bold", quiz.Style);
            Assert.AreEqual(PreferenceQuiz.Unknown, quiz.Mood);
            Assert.AreEqual(PreferenceQuiz.Unknown, quiz.Colour);
            Assert.AreEqual(1, quiz.Index);
            Assert.IsFalse(quiz.Finished);
        }

        [Test]
        public void AnUnknownAnswerLeavesThatCategoryOpen()
        {
            PreferenceQuiz quiz = new PreferenceQuiz();

            quiz.Answer(PreferenceQuiz.Unknown);
            quiz.Answer("calm");
            quiz.Answer(PreferenceQuiz.Unknown);

            Assert.AreEqual(PreferenceQuiz.Unknown, quiz.Style);
            Assert.AreEqual("calm", quiz.Mood);
            Assert.AreEqual(PreferenceQuiz.Unknown, quiz.Colour);
            Assert.IsTrue(quiz.Finished);
            Assert.IsTrue(quiz.AnyAnswered());
        }

        [Test]
        public void ThreeUnknownAnswersStoreNothing()
        {
            PreferenceQuiz quiz = new PreferenceQuiz();

            quiz.Answer(PreferenceQuiz.Unknown);
            quiz.Answer("  ");
            quiz.Answer(null);

            Assert.IsTrue(quiz.Finished);
            Assert.IsFalse(quiz.AnyAnswered());
        }

        [Test]
        public void ALaterAnswerDoesNotOverwriteAFinishedQuiz()
        {
            PreferenceQuiz quiz = new PreferenceQuiz();
            quiz.Answer("sample");
            quiz.Answer("quiet");
            quiz.Answer("red");

            quiz.Answer("blue");

            Assert.AreEqual("sample", quiz.Style);
            Assert.AreEqual("quiet", quiz.Mood);
            Assert.AreEqual("red", quiz.Colour);
        }

        [Test]
        public void RestartClearsTheAnswersAndReturnsToTheFirstQuestion()
        {
            PreferenceQuiz quiz = new PreferenceQuiz();
            quiz.Answer("bold");
            quiz.Answer("bright");

            quiz.Restart();

            Assert.AreEqual(0, quiz.Index);
            Assert.IsFalse(quiz.AnyAnswered());
            Assert.AreEqual("What kind of piece are you after?", quiz.Prompt());
        }

        [Test]
        public void EachQuestionOffersABlankTagLast()
        {
            PreferenceQuiz.Choice[] style = PreferenceQuiz.StyleChoices();
            PreferenceQuiz.Choice[] mood = PreferenceQuiz.MoodChoices();
            PreferenceQuiz.Choice[] colour = PreferenceQuiz.ColourChoices();

            Assert.AreEqual("sample", style[0].Tag);
            Assert.AreEqual("bold", style[1].Tag);
            Assert.AreEqual(PreferenceQuiz.Unknown, style[style.Length - 1].Tag);
            Assert.AreEqual("I don't know", style[style.Length - 1].Label);

            Assert.AreEqual("calm", mood[0].Tag);
            Assert.AreEqual("quiet", mood[1].Tag);
            Assert.AreEqual("bright", mood[2].Tag);
            Assert.AreEqual(PreferenceQuiz.Unknown, mood[mood.Length - 1].Tag);

            Assert.AreEqual("blue", colour[0].Tag);
            Assert.AreEqual("orange", colour[1].Tag);
            Assert.AreEqual("red", colour[2].Tag);
            Assert.AreEqual(PreferenceQuiz.Unknown, colour[colour.Length - 1].Tag);
        }
    }
}
