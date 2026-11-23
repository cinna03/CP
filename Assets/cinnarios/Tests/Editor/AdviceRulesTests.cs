using System.Collections.Generic;
using Cinnarios.Catalogue.Data;
using Cinnarios.Catalogue.Shop;
using Cinnarios.Painting.Data;
using Cinnarios.Placement.UI;
using NUnit.Framework;

namespace Cinnarios.Tests
{
    public sealed class AdviceRulesTests
    {
        [Test]
        public void TooBigAsksForSmallerPrints()
        {
            DecisionTalk.Turn turn = DecisionTalk.Hear("it is too big", Sample("436535", 93.4f, 73.2f, 420m));

            Assert.IsTrue(turn.WantsSuggestions);
            Assert.AreEqual(WallAdvice.Aspect.Size, turn.Aspect);
            Assert.AreEqual(WallAdvice.Lean.Smaller, turn.Lean);
            Assert.IsFalse(turn.WantsNewSpot);
        }

        [Test]
        public void MoveItAsksForANewSpotOfTheSamePainting()
        {
            DecisionTalk.Turn turn = DecisionTalk.Hear("move it", Sample("436535", 93.4f, 73.2f, 420m));

            Assert.IsTrue(turn.WantsNewSpot);
            Assert.IsFalse(turn.WantsSuggestions);
        }

        [Test]
        public void SmallerSuggestionsStayOffTheCurrentPrintAndAreSmaller()
        {
            ShopArtworkData current = Shop("436535", 100f, 80f, "42000");
            List<ShopArtworkData> catalogue = new List<ShopArtworkData>
            {
                current,
                Shop("1", 40f, 50f, "20000"),
                Shop("2", 30f, 40f, "18000"),
                Shop("3", 200f, 150f, "90000"),
            };

            List<WallAdvice.Suggestion> suggestions = WallAdvice.Suggest(
                catalogue, current, WallAdvice.Aspect.Size, WallAdvice.Lean.Smaller);

            Assert.AreEqual(3, suggestions.Count);
            foreach (WallAdvice.Suggestion suggestion in suggestions)
            {
                Assert.AreNotEqual(current.sourceId, suggestion.Artwork.sourceId);
            }

            Assert.IsTrue(suggestions[0].Direct);
            Assert.Less(suggestions[0].Artwork.widthCm * suggestions[0].Artwork.heightCm, 100f * 80f);
            Assert.IsTrue(suggestions[1].Direct);
            Assert.Less(suggestions[1].Artwork.widthCm * suggestions[1].Artwork.heightCm, 100f * 80f);
        }

        static ArtworkRecord Sample(string sourceId, float widthCm, float heightCm, decimal dollars)
        {
            return new ArtworkRecord(
                sourceId, "Wheat Field with Cypresses", "Vincent van Gogh", "1889", "The Met",
                "Oil on canvas", PaintingMedium.Canvas,
                new PaintingDimensions(widthCm, heightCm, 2f),
                "https://example.com/print.jpg", "https://example.com/thumb.jpg",
                dollars, "USD");
        }

        static ShopArtworkData Shop(string sourceId, float widthCm, float heightCm, string cents)
        {
            return new ShopArtworkData
            {
                sourceId = sourceId,
                title = sourceId,
                artist = "Artist",
                widthCm = widthCm,
                heightCm = heightCm,
                depthCm = 2f,
                priceAmount = cents,
            };
        }
    }
}
