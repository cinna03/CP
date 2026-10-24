using System.Collections.Generic;
using Capstone.Catalogue;

namespace Capstone.Ranking
{
    /// <summary>
    /// Five local sample works for the ranker. sample-wall and sample-wide use the same ids,
    /// sizes, and tags as the two works the placement screen can swap.
    /// </summary>
    public static class SampleCatalogue
    {
        public static IReadOnlyList<Artwork> All { get; } = new Artwork[]
        {
            Work("sample-wall", "Sample wall work", 50f, 40f, "sample", "calm", "blue", "A first blue sample for the wall."),
            Work("sample-wide", "Wide sample", 30f, 80f, "sample", "calm", "orange", "A wider orange sample."),
            Work("sample-calm-blue", "Calm blue study", 60f, 45f, "sample", "calm", "blue", "Another calm blue study."),
            Work("sample-quiet", "Quiet blue", 40f, 40f, "sample", "quiet", "blue", "A quieter blue study."),
            Work("sample-bold", "Bold red study", 70f, 50f, "bold", "bright", "red", "A bright red study with no shared tags."),
        };

        public static Artwork Find(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            for (int i = 0; i < All.Count; i++)
            {
                if (All[i].Id == id)
                    return All[i];
            }

            return null;
        }

        static Artwork Work(
            string id,
            string title,
            float heightCm,
            float widthCm,
            string style,
            string mood,
            string colour,
            string meaning)
        {
            return new Artwork(
                id,
                title,
                "Sample artist",
                2024,
                "Test print",
                heightCm,
                widthCm,
                style,
                mood,
                colour,
                "none",
                "generated-" + id,
                new StoryNotes(string.Empty, string.Empty, meaning));
        }
    }
}
