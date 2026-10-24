using System.Collections.Generic;
using System.Text;

namespace Capstone.Dialogue
{
    /// <summary>
    /// Matches free text against an author-written keyword table. The match does not call a network
    /// and does not invent a reply. The row with the most matching words wins. A tie uses priority,
    /// then the rule id. The reply language is the language of the matched words. If two languages
    /// tie, the language of the earliest matched word in the sentence wins.
    /// </summary>
    public static class RuleEngine
    {
        public const string FixedPrompt = "I can only answer from the written rules about size, colour, or price.";

        public static DialogueReply Match(string input, IReadOnlyList<DialogueRule> rules)
        {
            List<string> tokens = Tokenize(input);
            if (tokens.Count == 0 || rules == null || rules.Count == 0)
                return DialogueReply.Unmatched(FixedPrompt);

            DialogueRule winner = null;
            KeywordScore winningScore = new KeywordScore(0, 0, 0);
            for (int i = 0; i < rules.Count; i++)
            {
                DialogueRule rule = rules[i];
                KeywordScore score = Score(tokens, rule);
                if (score.Total == 0)
                    continue;
                if (winner == null || Beats(score, rule, winningScore, winner))
                {
                    winner = rule;
                    winningScore = score;
                }
            }

            if (winner == null)
                return DialogueReply.Unmatched(FixedPrompt);

            string language = ChooseLanguage(tokens, winner, winningScore);
            return new DialogueReply(winner.Id, winner.Concern, language, winner.Reply(language));
        }

        public static List<string> Tokenize(string input)
        {
            List<string> tokens = new List<string>();
            if (string.IsNullOrWhiteSpace(input))
                return tokens;

            StringBuilder current = new StringBuilder();
            foreach (char character in input)
            {
                if (char.IsLetter(character))
                {
                    current.Append(char.ToLowerInvariant(character));
                }
                else if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
            }

            if (current.Length > 0)
                tokens.Add(current.ToString());
            return tokens;
        }

        static KeywordScore Score(List<string> tokens, DialogueRule rule)
        {
            int english = 0;
            int french = 0;
            int kinyarwanda = 0;
            for (int i = 0; i < tokens.Count; i++)
            {
                string token = tokens[i];
                if (Contains(rule.EnglishKeywords, token))
                    english++;
                if (Contains(rule.FrenchKeywords, token))
                    french++;
                if (Contains(rule.KinyarwandaKeywords, token))
                    kinyarwanda++;
            }

            return new KeywordScore(english, french, kinyarwanda);
        }

        static bool Beats(KeywordScore score, DialogueRule rule, KeywordScore winningScore, DialogueRule winner)
        {
            if (score.Total != winningScore.Total)
                return score.Total > winningScore.Total;
            if (rule.Priority != winner.Priority)
                return rule.Priority > winner.Priority;
            return string.CompareOrdinal(rule.Id, winner.Id) < 0;
        }

        static string ChooseLanguage(List<string> tokens, DialogueRule rule, KeywordScore score)
        {
            int best = score.English;
            if (score.French > best)
                best = score.French;
            if (score.Kinyarwanda > best)
                best = score.Kinyarwanda;

            int leaders = 0;
            if (score.English == best)
                leaders++;
            if (score.French == best)
                leaders++;
            if (score.Kinyarwanda == best)
                leaders++;
            if (leaders == 1)
            {
                if (score.Kinyarwanda == best)
                    return "rw";
                if (score.French == best)
                    return "fr";
                return "en";
            }

            for (int i = 0; i < tokens.Count; i++)
            {
                string token = tokens[i];
                if (score.English == best && Contains(rule.EnglishKeywords, token))
                    return "en";
                if (score.French == best && Contains(rule.FrenchKeywords, token))
                    return "fr";
                if (score.Kinyarwanda == best && Contains(rule.KinyarwandaKeywords, token))
                    return "rw";
            }

            return "en";
        }

        static bool Contains(string[] keywords, string token)
        {
            for (int i = 0; i < keywords.Length; i++)
            {
                if (keywords[i] == token)
                    return true;
            }

            return false;
        }

        struct KeywordScore
        {
            public KeywordScore(int english, int french, int kinyarwanda)
            {
                English = english;
                French = french;
                Kinyarwanda = kinyarwanda;
            }

            public int English { get; }

            public int French { get; }

            public int Kinyarwanda { get; }

            public int Total => English + French + Kinyarwanda;
        }
    }
}
