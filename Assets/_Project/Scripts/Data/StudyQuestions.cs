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
        /// <summary>True when the buyer may pick several answers.</summary>
        public virtual bool AllowsMany => false;
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
    public class ChoiceQuestion : StudyQuestion
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
    /// A choice where several answers can be true at once ("pick all that apply"). One code, usually
    /// "none", is exclusive: picking it clears the others. Several picks export as codes joined by ";".
    /// </summary>
    public sealed class MultiChoiceQuestion : ChoiceQuestion
    {
        public const char Separator = ';';

        public MultiChoiceQuestion(string id, string prompt, string exclusiveKey, params string[] pairs) : base(id, prompt, pairs)
        {
            Exclusive = Find(exclusiveKey);
        }

        public override bool AllowsMany => true;
        /// <summary>Position of the answer that cannot be combined with others, or 0.</summary>
        public int Exclusive { get; }

        /// <summary>The picks in order, joined as one export value, e.g. "size;room_fit".</summary>
        public string Codes(IEnumerable<int> picks)
        {
            var codes = new List<string>();
            foreach (int pick in picks)
            {
                string code = Key(pick);
                if (code.Length > 0 && !codes.Contains(code))
                    codes.Add(code);
            }

            return string.Join(Separator.ToString(), codes);
        }
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

        public static readonly MultiChoiceQuestion PreBlocker = new MultiChoiceQuestion("pre_blocker",
            "When shopping for art online, what usually makes you pause? Pick all that apply.", None,
            "size", "Getting the size right",
            "room_fit", "Knowing how it fits my room",
            "story", "Connecting with the artist's story",
            "price", "Price & value",
            None, "Nothing holds me back");

        public static readonly ScaleQuestion PreSizeClarity = new ScaleQuestion("pre_size_clarity",
            "Looking at standard online art photos, how easy is it to picture a painting's true size?", Clarity);

        public static readonly ChoiceQuestion PreIntent = new ChoiceQuestion("pre_intent",
            "What brings you here today?",
            "specific_wall", "I have a wall that needs a piece",
            "general_room", "I'm decorating a room",
            "browsing", "Just exploring art");

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

        public static readonly MultiChoiceQuestion PostHelpfulFeature = new MultiChoiceQuestion("post_helpful_feature",
            "What helped you during this visit? Pick all that apply.", None,
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
            Blockers(preBlocker).Count == 0 ? HesitationCheck : BlockerResolution;

        /// <summary>The buyer's named blockers in their words, without "nothing holds me back".</summary>
        public static List<string> Blockers(string preBlocker)
        {
            var words = new List<string>();
            if (string.IsNullOrEmpty(preBlocker))
                return words;
            foreach (string code in preBlocker.Split(MultiChoiceQuestion.Separator))
            {
                int index = PreBlocker.Find(code.Trim());
                if (index > 0 && code.Trim() != None)
                    words.Add(PreBlocker.Describe(index).ToLowerInvariant());
            }

            return words;
        }

        public static string ResolutionPrompt(string preBlocker)
        {
            List<string> words = Blockers(preBlocker);
            if (words.Count == 0)
                return HesitationCheck.Prompt;
            string named = words.Count == 1 ? words[0]
                : string.Join(", ", words.GetRange(0, words.Count - 1)) + " and " + words[words.Count - 1];
            return "Earlier you mentioned <b>" + named + "</b> usually " + (words.Count == 1 ? "makes" : "make")
                + " you pause. How do you feel about that now?";
        }

        /// <summary>The optional follow-up appears only for low confidence or an unresolved doubt.</summary>
        public static bool NeedsFollowUp(int postConfidence, string resolution) =>
            (postConfidence >= 1 && postConfidence <= 3) || resolution == Unsure;
    }
}
