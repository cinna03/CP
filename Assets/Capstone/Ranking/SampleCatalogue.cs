using System.Collections.Generic;
using Capstone.Catalogue;

namespace Capstone.Ranking
{
    /// <summary>
    /// Fifteen local stand-in works. Each record already has the catalogue fields:
    /// stored centimetres, style, mood, colour, and a story note.
    /// A consented work replaces the image and the notes. The id, the size, and the tags stay the shape the ranker and the wall already read.
    /// sample-wall and sample-wide use the same ids, sizes, and tags as the two works the placement screen can swap.
    /// The first five stay in this order. Later works give keep, save, and dismiss another piece to rank.
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
            Work("sample-bright-blue", "Bright blue sample", 55f, 40f, "sample", "bright", "blue", "A brighter blue sample."),
            Work("sample-bright-orange", "Bright orange sample", 45f, 60f, "sample", "bright", "orange", "A bright orange sample."),
            Work("sample-bright-red", "Bright red sample", 65f, 50f, "sample", "bright", "red", "A bright red sample."),
            Work("sample-quiet-orange", "Quiet orange", 35f, 35f, "sample", "quiet", "orange", "A small quiet orange study."),
            Work("sample-quiet-red", "Quiet red study", 48f, 36f, "sample", "quiet", "red", "A quiet red study."),
            Work("bold-calm-blue", "Bold calm blue", 80f, 60f, "bold", "calm", "blue", "A large calm blue study."),
            Work("bold-calm-orange", "Bold calm orange", 42f, 70f, "bold", "calm", "orange", "A wide calm orange study."),
            Work("bold-quiet-blue", "Bold quiet blue", 55f, 55f, "bold", "quiet", "blue", "A square quiet blue study."),
            Work("bold-bright-orange", "Bold orange", 38f, 90f, "bold", "bright", "orange", "A wide bright orange study."),
            Work("bold-bright-blue", "Bold bright blue", 72f, 48f, "bold", "bright", "blue", "A tall bright blue study."),
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
