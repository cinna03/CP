using System.Collections.Generic;
using Capstone.Catalogue;
using NUnit.Framework;

namespace Capstone.Ranking.Tests
{
    public class RankerTests
    {
        [Test]
        public void KeepPutsAMatchingWorkFirst()
        {
            Artwork current = Work("current", "Quiet", "Calm", "Blue");
            VisitWeights weights = new VisitWeights();
            weights.Apply(current, Decision.Keep);

            IReadOnlyList<Artwork> three = Ranker.Three(Sample(current), current, weights);

            Assert.AreEqual(3, three.Count);
            Assert.AreEqual("match", three[0].Id);
            Assert.AreEqual("A matching note.", three[0].StoryNote);
        }

        [Test]
        public void SaveStillPrefersTheSameTags()
        {
            Artwork current = Work("current", "Quiet", "Calm", "Blue");
            VisitWeights weights = new VisitWeights();
            weights.Apply(current, Decision.Save);

            IReadOnlyList<Artwork> three = Ranker.Three(Sample(current), current, weights);

            Assert.AreEqual("match", three[0].Id);
        }

        [Test]
        public void DismissPutsTheMatchingWorkLast()
        {
            Artwork current = Work("current", "Quiet", "Calm", "Blue");
            VisitWeights weights = new VisitWeights();
            weights.Apply(current, Decision.Dismiss);

            IReadOnlyList<Artwork> three = Ranker.Three(Sample(current), current, weights);

            Assert.AreEqual("other", three[0].Id);
            Assert.AreEqual("partial", three[2].Id);
        }

        [Test]
        public void TheCurrentWorkIsNeverIncluded()
        {
            Artwork current = Work("current", "Quiet", "Calm", "Blue");
            VisitWeights weights = new VisitWeights();
            weights.Apply(current, Decision.Keep);

            IReadOnlyList<Artwork> three = Ranker.Three(Sample(current), current, weights);

            for (int i = 0; i < three.Count; i++)
                Assert.AreNotEqual("current", three[i].Id);
        }

        [Test]
        public void ClearingTheVisitForgetsTheDecision()
        {
            Artwork current = Work("current", "Quiet", "Calm", "Blue");
            VisitWeights weights = new VisitWeights();
            weights.Apply(current, Decision.Keep);
            weights.Clear();

            Assert.AreEqual(0f, weights.Score(current));
        }

        static List<Artwork> Sample(Artwork current)
        {
            return new List<Artwork>
            {
                current,
                Work("match", "Quiet", "Calm", "Blue", "A matching note."),
                Work("partial", "Quiet", "Bright", "Red"),
                Work("other", "Bold", "Bright", "Red"),
                Work("spare", "Soft", "Still", "Green")
            };
        }

        static Artwork Work(string id, string style, string mood, string colour, string note = "")
        {
            return new Artwork(id, id, "Artist", 2020, "Paint", 50f, 40f, style, mood, colour, "Mid", id + ".png", note);
        }
    }
}
