using System;

namespace Capstone.Dialogue
{
    /// <summary>
    /// One author-written row. Keywords and replies are stored for English, French, and Kinyarwanda.
    /// A higher priority wins when two rows match the same number of words.
    /// </summary>
    public sealed class DialogueRule
    {
        public DialogueRule(
            string id,
            string concern,
            int priority,
            string[] englishKeywords,
            string englishReply,
            string[] frenchKeywords,
            string frenchReply,
            string[] kinyarwandaKeywords,
            string kinyarwandaReply)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A rule needs an id.", nameof(id));

            Id = id;
            Concern = concern ?? string.Empty;
            Priority = priority;
            EnglishKeywords = Normalize(englishKeywords);
            EnglishReply = englishReply ?? string.Empty;
            FrenchKeywords = Normalize(frenchKeywords);
            FrenchReply = frenchReply ?? string.Empty;
            KinyarwandaKeywords = Normalize(kinyarwandaKeywords);
            KinyarwandaReply = kinyarwandaReply ?? string.Empty;
        }

        public string Id { get; }

        public string Concern { get; }

        public int Priority { get; }

        public string[] EnglishKeywords { get; }

        public string EnglishReply { get; }

        public string[] FrenchKeywords { get; }

        public string FrenchReply { get; }

        public string[] KinyarwandaKeywords { get; }

        public string KinyarwandaReply { get; }

        public string Reply(string language)
        {
            if (language == "fr")
                return FrenchReply;
            if (language == "rw")
                return KinyarwandaReply;
            return EnglishReply;
        }

        static string[] Normalize(string[] keywords)
        {
            if (keywords == null || keywords.Length == 0)
                return new string[0];

            string[] normalized = new string[keywords.Length];
            for (int i = 0; i < keywords.Length; i++)
                normalized[i] = (keywords[i] ?? string.Empty).Trim().ToLowerInvariant();
            return normalized;
        }
    }
}
