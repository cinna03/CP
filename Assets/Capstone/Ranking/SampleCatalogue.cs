using System.Collections.Generic;
using Capstone.Catalogue;

namespace Capstone.Ranking
{
    /// <summary>
    /// Fifteen local stand-in works. Each record already has the catalogue fields:
    /// stored centimetres, style, mood, colour, and story notes for background, inspiration, and meaning.
    /// A consented work replaces the image and the notes. The id, the size, and the tags stay the shape the ranker and the wall already read.
    /// sample-wall and sample-wide use the same ids, sizes, and tags as the two works the placement screen can swap.
    /// The first five stay in this order. Later works give keep, save, and dismiss another piece to rank.
    /// </summary>
    public static class SampleCatalogue
    {
        public static IReadOnlyList<Artwork> All { get; } = new Artwork[]
        {
            Work("sample-wall", "Sample wall work", 50f, 40f, "sample", "calm", "blue", "Sample artist prepared this print for the first wall preview.", "A calm blue field, made to be read at arm's length.", "A first blue sample for the wall."),
            Work("sample-wide", "Wide sample", 30f, 80f, "sample", "calm", "orange", "Sample artist works in a small studio for this prototype.", "A wide orange band, made to test a long wall.", "A wider orange sample."),
            Work("sample-calm-blue", "Calm blue study", 60f, 45f, "sample", "calm", "blue", "Sample artist keeps a second blue study beside the first.", "Still water, used here only as a stand-in mood.", "Another calm blue study."),
            Work("sample-quiet", "Quiet blue", 40f, 40f, "sample", "quiet", "blue", "Sample artist set this square aside as a quieter companion.", "A low blue, meant to sit without calling across the room.", "A quieter blue study."),
            Work("sample-bold", "Bold red study", 70f, 50f, "bold", "bright", "red", "Sample artist made this red study with none of the blue tags.", "A bright mark, used to test a recommendation that shares nothing.", "A bright red study with no shared tags."),
            Work("sample-bright-blue", "Bright blue sample", 55f, 40f, "sample", "bright", "blue", "Sample artist printed this as a brighter blue than the calm pair.", "Daylight on a blue cloth.", "A brighter blue sample."),
            Work("sample-bright-orange", "Bright orange sample", 45f, 60f, "sample", "bright", "orange", "Sample artist uses orange when the brief asks for something bright.", "A market cloth seen in strong sun.", "A bright orange sample."),
            Work("sample-bright-red", "Bright red sample", 65f, 50f, "sample", "bright", "red", "Sample artist keeps a red study for buyers who want a strong colour.", "A single red shape, large enough to hold a wall.", "A bright red sample."),
            Work("sample-quiet-orange", "Quiet orange", 35f, 35f, "sample", "quiet", "orange", "Sample artist made this small square for a quiet corner.", "Warm dust, kept soft on purpose.", "A small quiet orange study."),
            Work("sample-quiet-red", "Quiet red study", 48f, 36f, "sample", "quiet", "red", "Sample artist mixed a quiet red so it would not shout.", "Embers rather than flame.", "A quiet red study."),
            Work("bold-calm-blue", "Bold calm blue", 80f, 60f, "bold", "calm", "blue", "Sample artist scaled this blue up for a long wall.", "A calm colour carried at a bold size.", "A large calm blue study."),
            Work("bold-calm-orange", "Bold calm orange", 42f, 70f, "bold", "calm", "orange", "Sample artist stretched this orange wide and kept the mood calm.", "Late light across a plain wall.", "A wide calm orange study."),
            Work("bold-quiet-blue", "Bold quiet blue", 55f, 55f, "bold", "quiet", "blue", "Sample artist made this square both large and quiet.", "A deep blue that stays inside its own edges.", "A square quiet blue study."),
            Work("bold-bright-orange", "Bold orange", 38f, 90f, "bold", "bright", "orange", "Sample artist made this the widest bright orange in the set.", "A banner of orange for a long hall.", "A wide bright orange study."),
            Work("bold-bright-blue", "Bold bright blue", 72f, 48f, "bold", "bright", "blue", "Sample artist made this the tall bright blue in the set.", "A vertical blue, meant to be seen from across a room.", "A tall bright blue study."),
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
            string background,
            string inspiration,
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
                new StoryNotes(background, inspiration, meaning));
        }
    }
}
