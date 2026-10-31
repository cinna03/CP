namespace Capstone.Session
{
    /// <summary>
    /// Three questions that store the starting tags.
    /// An answer writes one style, one mood, or one colour.
    /// The blank tag leaves that category open, so the catalogue filter drops nothing on it.
    /// </summary>
    public sealed class PreferenceQuiz
    {
        public const int QuestionCount = 3;
        public const string Unknown = "";

        public int Index { get; private set; }

        public string Style { get; private set; } = Unknown;

        public string Mood { get; private set; } = Unknown;

        public string Colour { get; private set; } = Unknown;

        public bool Finished
        {
            get { return Index >= QuestionCount; }
        }

        public bool AnyAnswered()
        {
            return Style.Length > 0 || Mood.Length > 0 || Colour.Length > 0;
        }

        public string Prompt()
        {
            if (Index <= 0)
                return "What kind of piece are you after?";
            if (Index == 1)
                return "What feeling should it have?";
            if (Index == 2)
                return "Any colour in mind?";
            return string.Empty;
        }

        public Choice[] CurrentChoices()
        {
            if (Index <= 0)
                return StyleChoices();
            if (Index == 1)
                return MoodChoices();
            if (Index == 2)
                return ColourChoices();
            return new Choice[0];
        }

        public void Answer(string tag)
        {
            if (Finished)
                return;

            string stored = Normalize(tag);
            if (Index <= 0)
                Style = stored;
            else if (Index == 1)
                Mood = stored;
            else
                Colour = stored;

            Index++;
        }

        public void Restart()
        {
            Index = 0;
            Style = Unknown;
            Mood = Unknown;
            Colour = Unknown;
        }

        public static Choice[] StyleChoices()
        {
            return new Choice[]
            {
                new Choice("A simpler piece", "sample"),
                new Choice("Something bold", "bold"),
                new Choice("I don't know", Unknown),
            };
        }

        public static Choice[] MoodChoices()
        {
            return new Choice[]
            {
                new Choice("Calm", "calm"),
                new Choice("Quiet", "quiet"),
                new Choice("Bright", "bright"),
                new Choice("I don't know", Unknown),
            };
        }

        public static Choice[] ColourChoices()
        {
            return new Choice[]
            {
                new Choice("Blue", "blue"),
                new Choice("Orange", "orange"),
                new Choice("Red", "red"),
                new Choice("I don't know", Unknown),
            };
        }

        static string Normalize(string tag)
        {
            return string.IsNullOrWhiteSpace(tag) ? Unknown : tag.Trim().ToLowerInvariant();
        }

        public readonly struct Choice
        {
            public Choice(string label, string tag)
            {
                Label = label;
                Tag = tag ?? Unknown;
            }

            public string Label { get; }

            public string Tag { get; }
        }
    }
}
