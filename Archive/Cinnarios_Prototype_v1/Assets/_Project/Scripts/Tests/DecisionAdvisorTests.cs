using Capstone.App.Data;
using Capstone.App.Services;
using NUnit.Framework;

namespace Capstone.App.Tests
{
    public class DecisionAdvisorTests
    {
        [TestCase("i am happy with all of them, which one would you recommend?")]
        [TestCase("Should I buy it?")]
        [TestCase("I can't decide")]
        public void OpenDecisionQuestionsAreRecognised(string asked)
        {
            Assert.IsTrue(DecisionAdvisor.IsAdviceRequest(asked));
        }

        [Test]
        public void ADirectSizeQuestionIsNotAdvice()
        {
            Assert.IsFalse(DecisionAdvisor.IsAdviceRequest("How wide is it?"));
        }
    }
}
