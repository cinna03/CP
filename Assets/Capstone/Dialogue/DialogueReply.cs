namespace Capstone.Dialogue
{
    /// <summary>
    /// The result of one match. RuleId is empty when nothing matched.
    /// </summary>
    public sealed class DialogueReply
    {
        public DialogueReply(string ruleId, string concern, string language, string text)
        {
            RuleId = ruleId ?? string.Empty;
            Concern = concern ?? string.Empty;
            Language = language ?? string.Empty;
            Text = text ?? string.Empty;
        }

        public string RuleId { get; }

        public string Concern { get; }

        public string Language { get; }

        public string Text { get; }

        public bool Matched => RuleId.Length > 0;

        public static DialogueReply Unmatched(string prompt)
        {
            return new DialogueReply(string.Empty, string.Empty, string.Empty, prompt);
        }
    }
}
