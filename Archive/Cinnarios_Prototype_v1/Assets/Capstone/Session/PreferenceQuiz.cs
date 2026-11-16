namespace Capstone.Session
{
    /// <summary>
    /// The first question asks whether the buyer already has a piece.
    /// Exploring then stores a style, a mood, and a colour.
    /// A blank tag leaves that category open, so the catalogue filter drops nothing on it.
    /// A buyer who already has a piece does not store those tags.
    /// </summary>
    public sealed class PreferenceQuiz
    {
        public const string Unknown = "";
        public const string OwnPiece = "own";
        public const string Exploring = "explore";

        public int Index { get; private set; }

        public bool BringingOwnPiece { get; private set; }

        public string Style { get; private set; } = Unknown;

        public string Mood { get; private set; } = Unknown;

        public string Colour { get; private set; } = Unknown;

        public bool Finished
        {
            get { return BringingOwnPiece || Index >= 4; }
        }

        public bool AnyAnswered()
        {
            return Style.Length > 0 || Mood.Length > 0 || Colour.Length > 0;
        }

        public string Prompt()
        {
            if (Index <= 0)
                return "Do you already have a piece to try?";
            if (Index == 1)
                return "What kind of piece are you after?";
            if (Index == 2)
                return "What feeling should it have?";
            if (Index == 3)
                return "Any colour in mind?";
            return string.Empty;
        }

        public Choice[] CurrentChoices()
        {
            if (Index <= 0)
                return IntentChoices();
            if (Index == 1)
                return StyleChoices();
            if (Index == 2)
                return MoodChoices();
            if (Index == 3)
                return ColourChoices();
            return new Choice[0];
        }

        public void Answer(string tag)
        {
            if (Finished)
                return;

            string stored = Normalize(tag);
            if (Index <= 0)
            {
                BringingOwnPiece = stored == OwnPiece;
                Index = 1;
                return;
            }

            if (Index == 1)
                Style = stored;
            else if (Index == 2)
                Mood = stored;
            else
                Colour = stored;

            Index++;
        }

        public void Restart()
        {
            Index = 0;
            BringingOwnPiece = false;
            Style = Unknown;
            Mood = Unknown;
            Colour = Unknown;
        }

        public static Choice[] IntentChoices()
        {
            return new Choice[]
            {
                new Choice("I have a piece", OwnPiece),
                new Choice("I'm still exploring", Exploring),
            };
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
