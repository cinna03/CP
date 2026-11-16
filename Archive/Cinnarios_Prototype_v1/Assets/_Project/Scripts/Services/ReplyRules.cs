using System;
using System.Collections.Generic;
using Capstone.App.Data;

namespace Capstone.App.Services
{
    /// <summary>
    /// One kind of question the written table can answer. Each subclass decides which words it
    /// listens for and how it builds its reply from the stored record, so a new kind of question
    /// is added as a new class instead of another branch in a long if-chain.
    /// </summary>
    public abstract class ReplyRule
    {
        /// <summary>Words, in English, French or Kinyarwanda, that select this rule.</summary>
        protected abstract string[] Keywords { get; }

        /// <summary>A short name for logs and tests.</summary>
        public abstract string Name { get; }

        public virtual bool Matches(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
                return false;
            string text = question.ToLowerInvariant();
            string[] words = Keywords;
            for (int i = 0; i < words.Length; i++)
            {
                if (text.Contains(words[i]))
                    return true;
            }

            return false;
        }

        public abstract string Answer(ArtworkData work, ArtworkQueryService replies);
    }

    /// <summary>Who made the work, plus the artist's own note when one exists.</summary>
    public sealed class ArtistRule : ReplyRule
    {
        static readonly string[] Words = { "artist", "painter", "who painted", "who made", "artiste", "umuhanzi" };
        protected override string[] Keywords => Words;
        public override string Name => "artist";
        public override string Answer(ArtworkData work, ArtworkQueryService replies) => replies.DescribeArtist(work);
    }

    /// <summary>What inspired the work. Only the artist can say; otherwise the reply says so.</summary>
    public sealed class InspirationRule : ReplyRule
    {
        static readonly string[] Words = { "inspiration", "inspired", "inspire", "why did" };
        protected override string[] Keywords => Words;
        public override string Name => "inspiration";
        public override string Answer(ArtworkData work, ArtworkQueryService replies) => replies.DescribeInspiration(work);
    }

    /// <summary>The story and meaning, from the curator's note or the stored note.</summary>
    public sealed class StoryRule : ReplyRule
    {
        static readonly string[] Words = { "story", "note", "meaning", "inkuru", "igisobanuro", "histoire", "signification" };
        protected override string[] Keywords => Words;
        public override string Name => "story";
        public override string Answer(ArtworkData work, ArtworkQueryService replies) => replies.DescribeStory(work);
    }

    /// <summary>The storytelling rules in the order they are tried: the first match answers.</summary>
    public static class StoryRules
    {
        public static readonly IReadOnlyList<ReplyRule> InOrder = new ReplyRule[]
        {
            new ArtistRule(),
            new InspirationRule(),
            new StoryRule(),
        };

        /// <summary>The first rule that matches, or null.</summary>
        public static ReplyRule Find(string question)
        {
            for (int i = 0; i < InOrder.Count; i++)
            {
                if (InOrder[i].Matches(question))
                    return InOrder[i];
            }

            return null;
        }
    }
}
