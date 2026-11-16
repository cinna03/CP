using System.Collections.Generic;

namespace Capstone.App.Data
{
    /// <summary>
    /// One study question. Answers are stored as a position (1..N); Key() turns that into the value the
    /// export uses (the number for a scale, a short code such as "room_fit" for a choice).
    /// </summary>
    public abstract class StudyQuestion
    {
        protected StudyQuestion(string id, string prompt)
        {
            Id = id;
            Prompt = prompt;
        }

        /// <summary>Field name in the export, e.g. pre_confidence.</summary>
        public string Id { get; }
        public string Prompt { get; }
        /// <summary>What the buyer sees for each answer.</summary>
        public abstract IReadOnlyList<string> Labels { get; }
        public virtual bool IsScale => false;
        /// <summary>Where a slider starts.</summary>
        public virtual int Default => 0;

        public bool Accepts(int answer) => answer >= 1 && answer <= Labels.Count;
        public string Describe(int answer) => Accepts(answer) ? Labels[answer - 1] : "";
        /// <summary>The stored value: the number on a scale, the code of a choice.</summary>
        public abstract string Key(int answer);
        /// <summary>The position of a stored value, or 0.</summary>
        public abstract int Find(string key);
    }

    /// <summary>A 1-7 slider. Each point has its own words, which update as the buyer drags.</summary>
    public sealed class ScaleQuestion : StudyQuestion
    {
        readonly string[] words;

        public ScaleQuestion(string id, string prompt, params string[] sevenWords) : base(id, prompt)
        {
            words = sevenWords;
        }

        public override IReadOnlyList<string> Labels => words;
        public override bool IsScale => true;
        public override int Default => 4;
        public override string Key(int answer) => Accepts(answer) ? answer.ToString() : "";
        public override int Find(string key) => int.TryParse(key, out int value) && Accepts(value) ? value : 0;
    }

    /// <summary>A single tap on one of a few cards.</summary>
    public sealed class ChoiceQuestion : StudyQuestion
    {
        readonly string[] keys;
        readonly string[] labels;

        /// <param name="pairs">key, label, key, label, ...</param>
        public ChoiceQuestion(string id, string prompt, params string[] pairs) : base(id, prompt)
        {
            keys = new string[pairs.Length / 2];
            labels = new string[pairs.Length / 2];
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i] = pairs[i * 2];
                labels[i] = pairs[i * 2 + 1];
            }
        }

        public IReadOnlyList<string> Keys => keys;
        public override IReadOnlyList<string> Labels => labels;
        public override string Key(int answer) => Accepts(answer) ? keys[answer - 1] : "";
        public override int Find(string key) => System.Array.IndexOf(keys, key) + 1;
    }

    /// <summary>
    /// The paired before/after assessment. Confidence and size clarity use the same 1-7 scale both times,
    /// so each buyer's change can be tested with a paired t-test; the buyer's own blocker is named before
    /// and checked after, and the after set asks which feature helped.
    /// </summary>
    public static class StudyQuestions
    {
        static readonly string[] Confidence =
            { "Very hesitant", "Hesitant", "A little hesitant", "On the fence", "Leaning ready", "Ready", "Completely ready" };
        static readonly string[] Clarity =
            { "Pure guesswork", "Mostly guesswork", "A little hazy", "Somewhat clear", "Fairly clear", "Clear", "Crystal clear" };

        public const string None = "none";
        public const string Unsure = "unsure";

        public static readonly ScaleQuestion PreConfidence = new ScaleQuestion("pre_confidence",
            "Imagine finding a painting you love online today. How confident would you feel hitting 'Buy'?", Confidence);

        public static readonly ChoiceQuestion PreBlocker = new ChoiceQuestion("pre_blocker",
            "When shopping for art online, what usually makes you pause?",
            "size", "Getting the size right",
            "room_fit", "Knowing how it fits my room",
            "story", "Connecting with the artist's story",
            "price", "Price & value",
            None, "Nothing holds me back");

        public static readonly ScaleQuestion PreSizeClarity = new ScaleQuestion("pre_size_clarity",
            "Looking at standard online art photos, how easy is it to picture a painting's true size?", Clarity);

        public static readonly ChoiceQuestion PreIntent = new ChoiceQuestion("pre_intent",
            "Where are you in your art search right now?",
            "specific_wall", "I have a specific wall ready",
            "general_room", "Shopping for a general room",
            "browsing", "Just browsing for inspiration");

        public static readonly ScaleQuestion PostConfidence = new ScaleQuestion("post_confidence",
            "Now that you've explored the pieces here, how confident would you feel buying one?", Confidence);

        public static readonly ScaleQuestion PostSizeClarity = new ScaleQuestion("post_size_clarity",
            "How easy was it to picture the paintings' true size during this visit?", Clarity);

        /// <summary>Asked when the buyer named a blocker before. The prompt quotes it (see ResolutionPrompt).</summary>
        public static readonly ChoiceQuestion BlockerResolution = new ChoiceQuestion("post_blocker_resolution",
            "How do you feel about that now?",
            "resolved", "Completely resolved",
            "somewhat", "Somewhat better",
            Unsure, "Still unsure");

        /// <summary>Asked instead when the buyer said nothing held them back. Same codes, so the column stays comparable.</summary>
        public static readonly ChoiceQuestion HesitationCheck = new ChoiceQuestion("post_blocker_resolution",
            "Did anything during this visit make you hesitate about buying?",
            "resolved", "No, felt great",
            "somewhat", "A little bit",
            Unsure, "Yes, had doubts");

        public static readonly ChoiceQuestion PostHelpfulFeature = new ChoiceQuestion("post_helpful_feature",
            "What helped you the most during this visit?",
            "true_scale", "Seeing the true scale",
            "room_preview", "Viewing it in a room",
            "story", "Reading the artwork's story",
            "suggestions", "Personalized suggestions",
            None, "None of these");

        /// <summary>Optional follow-up tags, shown only to buyers who are still unsure.</summary>
        public static readonly string[] OutlierTags =
            { "Colors/lighting felt off", "Still hard to judge scale", "Price point", "Didn't find my style" };

        public const string Tailoring = "Got it—tailoring your gallery experience...";

        /// <summary>The four onboarding screens, in order.</summary>
        public static readonly StudyQuestion[] Before = { PreConfidence, PreBlocker, PreSizeClarity, PreIntent };

        /// <summary>The wrap-up screens, in order. Screen 3 is chosen by Resolution().</summary>
        public static StudyQuestion[] After(string preBlocker) =>
            new StudyQuestion[] { PostConfidence, PostSizeClarity, Resolution(preBlocker), PostHelpfulFeature };

        public static ChoiceQuestion Resolution(string preBlocker) =>
            string.IsNullOrEmpty(preBlocker) || preBlocker == None ? HesitationCheck : BlockerResolution;

        public static string ResolutionPrompt(string preBlocker)
        {
            int index = PreBlocker.Find(preBlocker);
            if (index < 1 || preBlocker == None)
                return HesitationCheck.Prompt;
            return "Earlier you mentioned <b>" + PreBlocker.Describe(index).ToLowerInvariant()
                + "</b> usually makes you pause. How do you feel about that now?";
        }

        /// <summary>The optional follow-up appears only for low confidence or an unresolved doubt.</summary>
        public static bool NeedsFollowUp(int postConfidence, string resolution) =>
            (postConfidence >= 1 && postConfidence <= 3) || resolution == Unsure;
    }
}
