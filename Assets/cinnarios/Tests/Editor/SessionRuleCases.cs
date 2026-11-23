using System;
using System.Collections.Generic;
using Cinnarios.Catalogue.Data;
using Cinnarios.Catalogue.Pricing;
using Cinnarios.Catalogue.Shop;
using Cinnarios.Launch;
using Cinnarios.Painting.Data;
using Cinnarios.Placement;
using Cinnarios.Placement.UI;
using NUnit.Framework;

namespace Cinnarios.Tests
{
    public sealed class SizeCases
    {
        [TestCase(1, 1, 1, 0.01, 0.01, 0.01)]
        [TestCase(10, 20, 3, 0.1, 0.2, 0.03)]
        [TestCase(60, 80, 2, 0.6, 0.8, 0.02)]
        [TestCase(100, 50, 4, 1, 0.5, 0.04)]
        [TestCase(100, 100, 5, 1, 1, 0.05)]
        [TestCase(40, 60, 2, 0.4, 0.6, 0.02)]
        [TestCase(200, 100, 2, 2, 1, 0.02)]
        [TestCase(5, 8, 1, 0.05, 0.08, 0.01)]
        public void CentimetresBecomeMetres(double width, double height, double depth, double x, double y, double z)
        {
            var size = new PaintingDimensions((float)width, (float)height, (float)depth);

            Assert.AreEqual((float)x, size.SizeInMetres.x, 0.0001f);
            Assert.AreEqual((float)y, size.SizeInMetres.y, 0.0001f);
            Assert.AreEqual((float)z, size.SizeInMetres.z, 0.0001f);
        }

        [TestCase(0, 80, 2)]
        [TestCase(-1, 80, 2)]
        [TestCase(60, 0, 2)]
        [TestCase(60, -5, 2)]
        [TestCase(60, 80, 0)]
        [TestCase(60, 80, -2)]
        public void ANonPositiveSizeIsRefused(double width, double height, double depth)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new PaintingDimensions((float)width, (float)height, (float)depth));
        }

        [Test]
        public void AMissingNumberIsRefused()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PaintingDimensions(float.NaN, 80f, 2f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PaintingDimensions(60f, float.PositiveInfinity, 2f));
        }
    }

    public sealed class PriceCases
    {
        [TestCase(60, 80, 340)]
        [TestCase(100, 100, 550)]
        [TestCase(50, 50, 250)]
        [TestCase(10, 10, 150)]
        [TestCase(200, 100, 950)]
        [TestCase(30, 40, 200)]
        [TestCase(80, 80, 410)]
        [TestCase(120, 90, 580)]
        [TestCase(40, 60, 250)]
        [TestCase(25, 40, 190)]
        [TestCase(15, 20, 160)]
        [TestCase(90, 90, 470)]
        [TestCase(70, 100, 430)]
        [TestCase(20, 20, 170)]
        public void TheShopPriceFollowsTheArea(double width, double height, int expected)
        {
            decimal price = new AreaPricingRule().PriceFor(new PaintingDimensions((float)width, (float)height, 2f));

            Assert.AreEqual((decimal)expected, price);
        }

        [Test]
        public void AMissingPaintingCannotBePriced()
        {
            Assert.Throws<ArgumentNullException>(() => new AreaPricingRule().PriceFor(null));
        }

        [Test]
        public void ANegativeBasePriceIsRefused()
        {
            Assert.Throws<ArgumentException>(() => new AreaPricingRule(-1m));
        }
    }

    public sealed class TryLinkCases
    {
        const string Shop = "https://cinnarios.onrender.com";

        [TestCase("cinnarios-ar://try/436535?api=https%3A%2F%2Fcinnarios.onrender.com", "436535", Shop)]
        [TestCase("cinnarios://try/436535?api=https%3A%2F%2Fcinnarios.onrender.com", "436535", Shop)]
        [TestCase("cinnarios-ar://try/7?api=http%3A%2F%2Flocalhost%3A3001", "7", "http://localhost:3001")]
        [TestCase("cinnarios-ar://try/123456789012?api=https%3A%2F%2Fcinnarios.onrender.com", "123456789012", Shop)]
        [TestCase("cinnarios-ar://try/0?api=https%3A%2F%2Fcinnarios.onrender.com", "0", Shop)]
        [TestCase("cinnarios-ar://try/436535/?api=https%3A%2F%2Fcinnarios.onrender.com", "436535", Shop)]
        public void ATryLinkKeepsThePaintingAndTheShop(string url, string sourceId, string api)
        {
            bool parsed = TryLink.TryParse(url, out TryLink link, out string error);

            Assert.IsTrue(parsed, error);
            Assert.AreEqual(sourceId, link.SourceId);
            Assert.AreEqual(api, link.ApiBaseUrl);
        }

        [TestCase("cinnarios-ar://own/photo?w=40&h=50&api=https%3A%2F%2Fcinnarios.onrender.com")]
        [TestCase("")]
        [TestCase("cinnarios-ar://try/436535")]
        [TestCase("cinnarios-ar://try/436535?api=ftp%3A%2F%2Ffiles.example")]
        [TestCase("cinnarios-ar://try/not-a-number?api=https%3A%2F%2Fcinnarios.onrender.com")]
        [TestCase("cinnarios-ar://try/1234567890123?api=https%3A%2F%2Fcinnarios.onrender.com")]
        [TestCase("https://cinnarios.onrender.com/try/436535")]
        [TestCase("cinnarios-ar://try/?api=https%3A%2F%2Fcinnarios.onrender.com")]
        public void ALinkThatIsNotATryIsRefused(string url)
        {
            Assert.IsFalse(TryLink.TryParse(url, out _, out _));
        }
    }

    public sealed class LockSequenceCases
    {
        [TestCase(PlacementState.Scanning, PlacementState.Aiming, true)]
        [TestCase(PlacementState.Scanning, PlacementState.Positioning, false)]
        [TestCase(PlacementState.Scanning, PlacementState.Locked, false)]
        [TestCase(PlacementState.Scanning, PlacementState.Scanning, false)]
        [TestCase(PlacementState.Aiming, PlacementState.Scanning, true)]
        [TestCase(PlacementState.Aiming, PlacementState.Positioning, true)]
        [TestCase(PlacementState.Aiming, PlacementState.Locked, false)]
        [TestCase(PlacementState.Aiming, PlacementState.Aiming, false)]
        [TestCase(PlacementState.Positioning, PlacementState.Locked, true)]
        [TestCase(PlacementState.Positioning, PlacementState.Scanning, true)]
        [TestCase(PlacementState.Positioning, PlacementState.Aiming, false)]
        [TestCase(PlacementState.Positioning, PlacementState.Positioning, false)]
        [TestCase(PlacementState.Locked, PlacementState.Positioning, true)]
        [TestCase(PlacementState.Locked, PlacementState.Scanning, true)]
        [TestCase(PlacementState.Locked, PlacementState.Aiming, false)]
        [TestCase(PlacementState.Locked, PlacementState.Locked, false)]
        public void OnlyTheAllowedMovesSucceed(PlacementState from, PlacementState to, bool allowed)
        {
            PlacementStateMachine states = At(from);

            Assert.AreEqual(allowed, states.TryTransition(to));
            Assert.AreEqual(allowed ? to : from, states.Current);
        }

        [Test]
        public void ANewVisitCanReturnToScanningFromLocked()
        {
            PlacementStateMachine states = At(PlacementState.Locked);

            states.ForceScanning();

            Assert.AreEqual(PlacementState.Scanning, states.Current);
        }

        static PlacementStateMachine At(PlacementState state)
        {
            var machine = new PlacementStateMachine();
            if (state == PlacementState.Scanning)
            {
                return machine;
            }

            Assert.IsTrue(machine.TryTransition(PlacementState.Aiming));
            if (state == PlacementState.Aiming)
            {
                return machine;
            }

            Assert.IsTrue(machine.TryTransition(PlacementState.Positioning));
            if (state == PlacementState.Positioning)
            {
                return machine;
            }

            Assert.IsTrue(machine.TryTransition(PlacementState.Locked));
            return machine;
        }
    }

    public sealed class TalkCases
    {
        [TestCase("too big", "smaller")]
        [TestCase("too large", "smaller")]
        [TestCase("smaller please", "smaller")]
        [TestCase("huge", "smaller")]
        [TestCase("overwhelming", "smaller")]
        [TestCase("too wide", "smaller")]
        [TestCase("too tall", "smaller")]
        [TestCase("too small", "larger")]
        [TestCase("larger", "larger")]
        [TestCase("tiny", "larger")]
        [TestCase("more presence", "larger")]
        [TestCase("too little", "larger")]
        [TestCase("expensive", "cheaper")]
        [TestCase("cheaper", "cheaper")]
        [TestCase("I can afford it", "cheaper")]
        [TestCase("my budget", "cheaper")]
        [TestCase("the cost", "cheaper")]
        [TestCase("the price", "cheaper")]
        [TestCase("more expensive", "dearer")]
        [TestCase("a dearer one", "dearer")]
        [TestCase("a higher price", "dearer")]
        [TestCase("the colour", "colour-different")]
        [TestCase("the color", "colour-different")]
        [TestCase("they clash", "colour-different")]
        [TestCase("too pale", "colour-different")]
        [TestCase("same colour", "colour-keep")]
        [TestCase("colour like this", "colour-keep")]
        [TestCase("the lighting", "lighter")]
        [TestCase("a lamp", "lighter")]
        [TestCase("bright room", "lighter")]
        [TestCase("dark room", "deeper")]
        [TestCase("too dark", "deeper")]
        [TestCase("something else", "piece-different")]
        [TestCase("other artist", "piece-different")]
        [TestCase("boring", "piece-different")]
        [TestCase("not the style", "piece-different")]
        [TestCase("same artist", "piece-keep")]
        [TestCase("similar work", "piece-keep")]
        [TestCase("the fit", "fit")]
        [TestCase("the space", "fit")]
        [TestCase("move it", "spot")]
        [TestCase("another spot", "spot")]
        [TestCase("different spot", "spot")]
        [TestCase("place it again", "spot")]
        [TestCase("place again", "spot")]
        [TestCase("readjust", "spot")]
        [TestCase("reposition", "spot")]
        [TestCase("new spot", "spot")]
        [TestCase("yes", "stay")]
        [TestCase("this one is right", "stay")]
        [TestCase("i like it", "stay")]
        [TestCase("love it", "stay")]
        [TestCase("perfect", "stay")]
        [TestCase("keep it", "stay")]
        [TestCase("don't want", "leave")]
        [TestCase("do not want", "leave")]
        [TestCase("not this", "leave")]
        [TestCase("hate", "leave")]
        [TestCase("", "clarify")]
        [TestCase("not sure", "clarify")]
        [TestCase("unsure", "clarify")]
        [TestCase("maybe", "clarify")]
        [TestCase("don't know", "clarify")]
        [TestCase("what about the frame", "clarify")]
        [TestCase("who painted this", "artist")]
        [TestCase("what year", "date")]
        [TestCase("what is it made of", "medium")]
        [TestCase("how much is it", "money")]
        [TestCase("how big is it", "measure")]
        public void TheSentenceChoosesOneAnswer(string said, string kind)
        {
            DecisionTalk.Turn turn = DecisionTalk.Hear(said, Sample());

            switch (kind)
            {
                case "smaller":
                    ExpectSuggestion(turn, WallAdvice.Aspect.Size, WallAdvice.Lean.Smaller);
                    break;
                case "larger":
                    ExpectSuggestion(turn, WallAdvice.Aspect.Size, WallAdvice.Lean.Larger);
                    break;
                case "cheaper":
                    ExpectSuggestion(turn, WallAdvice.Aspect.Price, WallAdvice.Lean.Cheaper);
                    break;
                case "dearer":
                    ExpectSuggestion(turn, WallAdvice.Aspect.Price, WallAdvice.Lean.Dearer);
                    break;
                case "colour-different":
                    ExpectSuggestion(turn, WallAdvice.Aspect.Colour, WallAdvice.Lean.Different);
                    break;
                case "colour-keep":
                    ExpectSuggestion(turn, WallAdvice.Aspect.Colour, WallAdvice.Lean.Keep);
                    break;
                case "lighter":
                    ExpectSuggestion(turn, WallAdvice.Aspect.Light, WallAdvice.Lean.Lighter);
                    break;
                case "deeper":
                    ExpectSuggestion(turn, WallAdvice.Aspect.Light, WallAdvice.Lean.Deeper);
                    break;
                case "piece-different":
                    ExpectSuggestion(turn, WallAdvice.Aspect.Piece, WallAdvice.Lean.Different);
                    break;
                case "piece-keep":
                    ExpectSuggestion(turn, WallAdvice.Aspect.Piece, WallAdvice.Lean.Keep);
                    break;
                case "fit":
                    ExpectSuggestion(turn, WallAdvice.Aspect.Fit, WallAdvice.Lean.Keep);
                    break;
                case "spot":
                    Assert.IsTrue(turn.WantsNewSpot);
                    Assert.IsFalse(turn.WantsSuggestions);
                    break;
                case "stay":
                    Assert.IsTrue(turn.Satisfied);
                    Assert.IsFalse(turn.WantsSuggestions);
                    break;
                case "leave":
                    Assert.IsTrue(turn.Leave);
                    Assert.IsFalse(turn.WantsSuggestions);
                    break;
                case "clarify":
                    Assert.IsFalse(turn.WantsSuggestions);
                    Assert.IsFalse(turn.WantsNewSpot);
                    Assert.IsFalse(string.IsNullOrEmpty(turn.Reply));
                    break;
                case "artist":
                    StringAssert.Contains("Vincent van Gogh", turn.Reply);
                    Assert.IsFalse(turn.WantsSuggestions);
                    break;
                case "date":
                    StringAssert.Contains("1889", turn.Reply);
                    break;
                case "medium":
                    StringAssert.Contains("Oil on canvas", turn.Reply);
                    break;
                case "money":
                    StringAssert.Contains("RWF", turn.Reply);
                    Assert.IsFalse(turn.WantsSuggestions);
                    break;
                case "measure":
                    StringAssert.Contains("cm", turn.Reply);
                    Assert.IsFalse(turn.WantsSuggestions);
                    break;
            }
        }

        static void ExpectSuggestion(DecisionTalk.Turn turn, WallAdvice.Aspect aspect, WallAdvice.Lean lean)
        {
            Assert.IsTrue(turn.WantsSuggestions);
            Assert.AreEqual(aspect, turn.Aspect);
            Assert.AreEqual(lean, turn.Lean);
            Assert.IsFalse(turn.WantsNewSpot);
        }

        static ArtworkRecord Sample()
        {
            return new ArtworkRecord(
                "436535", "Wheat Field with Cypresses", "Vincent van Gogh", "1889", "The Met",
                "Oil on canvas", PaintingMedium.Canvas,
                new PaintingDimensions(93.4f, 73.2f, 2f),
                "https://example.com/print.jpg", "https://example.com/thumb.jpg",
                420m, "USD");
        }
    }

    public sealed class SuggestionCases
    {
        [Test]
        public void AnEmptyCatalogueReturnsNothing()
        {
            Assert.AreEqual(0, WallAdvice.Suggest(null, Shop("1", 10, 10, "1000"), WallAdvice.Aspect.Size, WallAdvice.Lean.Smaller).Count);
            Assert.AreEqual(0, WallAdvice.Suggest(new List<ShopArtworkData>(), null, WallAdvice.Aspect.Size, WallAdvice.Lean.Smaller).Count);
        }

        [Test]
        public void AtMostThreeSuggestionsAreReturned()
        {
            List<WallAdvice.Suggestion> suggestions = WallAdvice.Suggest(Catalogue(), Current(), WallAdvice.Aspect.Size, WallAdvice.Lean.Smaller);

            Assert.AreEqual(3, suggestions.Count);
        }

        [Test]
        public void SuggestionsNeverRepeatThePrintOnTheWall()
        {
            foreach (WallAdvice.Suggestion suggestion in WallAdvice.Suggest(Catalogue(), Current(), WallAdvice.Aspect.Size, WallAdvice.Lean.Smaller))
            {
                Assert.AreNotEqual("436535", suggestion.Artwork.sourceId);
            }
        }

        [Test]
        public void SmallerMeansASmallerArea()
        {
            List<WallAdvice.Suggestion> suggestions = WallAdvice.Suggest(Catalogue(), Current(), WallAdvice.Aspect.Size, WallAdvice.Lean.Smaller);

            Assert.IsTrue(suggestions[0].Direct);
            Assert.Less(Area(suggestions[0].Artwork), Area(Current()));
        }

        [Test]
        public void LargerMeansALargerArea()
        {
            List<WallAdvice.Suggestion> suggestions = WallAdvice.Suggest(Catalogue(), Current(), WallAdvice.Aspect.Size, WallAdvice.Lean.Larger);

            Assert.IsTrue(suggestions[0].Direct);
            Assert.Greater(Area(suggestions[0].Artwork), Area(Current()));
        }

        [Test]
        public void CheaperMeansALowerRecordedPrice()
        {
            WallAdvice.Suggestion suggestion = WallAdvice.Suggest(Catalogue(), Current(), WallAdvice.Aspect.Price, WallAdvice.Lean.Cheaper)[0];

            Assert.IsTrue(suggestion.Direct);
            Assert.Less(float.Parse(suggestion.Artwork.priceAmount), float.Parse(Current().priceAmount));
        }

        [Test]
        public void DearerMeansAHigherRecordedPrice()
        {
            WallAdvice.Suggestion suggestion = WallAdvice.Suggest(Catalogue(), Current(), WallAdvice.Aspect.Price, WallAdvice.Lean.Dearer)[0];

            Assert.IsTrue(suggestion.Direct);
            Assert.Greater(float.Parse(suggestion.Artwork.priceAmount), float.Parse(Current().priceAmount));
        }

        [Test]
        public void ADifferentColourIsNotTheSameGuide()
        {
            ShopArtworkData current = Current();
            current.colourGuide = "gold";
            WallAdvice.Suggestion suggestion = WallAdvice.Suggest(Catalogue(), current, WallAdvice.Aspect.Colour, WallAdvice.Lean.Different)[0];

            Assert.IsTrue(suggestion.Direct);
            Assert.AreNotEqual("gold", string.IsNullOrEmpty(suggestion.Artwork.colourGuide) ? "earth" : suggestion.Artwork.colourGuide);
        }

        static float Area(ShopArtworkData artwork) => artwork.widthCm * artwork.heightCm;

        static ShopArtworkData Current() => Shop("436535", 100f, 80f, "42000");

        static List<ShopArtworkData> Catalogue()
        {
            return new List<ShopArtworkData>
            {
                Current(),
                Shop("1", 40f, 50f, "20000"),
                Shop("2", 30f, 40f, "18000"),
                Shop("3", 200f, 150f, "90000"),
                Shop("4", 90f, 70f, "30000"),
                Shop("5", 120f, 100f, "80000"),
            };
        }

        static ShopArtworkData Shop(string sourceId, float widthCm, float heightCm, string cents)
        {
            return new ShopArtworkData
            {
                sourceId = sourceId,
                artist = sourceId == "4" ? "Same" : "Other",
                widthCm = widthCm,
                heightCm = heightCm,
                depthCm = 2f,
                priceAmount = cents,
                colourGuide = sourceId == "4" ? "gold" : "blue",
            };
        }
    }
}
