using System.Collections.Generic;
using Capstone.App.Data;
using UnityEditor;
using UnityEngine;

namespace Capstone.App.EditorTools
{
    /// <summary>
    /// Fills medium, year, and a short story note on fifteen of the shelf records.
    /// Five more receive a medium and a year with an empty story. Five stay blank.
    /// Titles, artists, sizes, tags, and pictures are left as they are.
    /// </summary>
    public static class ArtworkMetadataPopulator
    {
        const string ShelfPath = "Assets/Resources/CatalogueShelf.asset";

        struct DemoNote
        {
            public string Identifier;
            public string Medium;
            public int Year;
            public string Story;
        }

        static readonly DemoNote[] Notes =
        {
            new DemoNote { Identifier = "reading", Medium = "Oil", Year = 2019, Story = "A quiet figure reading, suited to a warm small room." },
            new DemoNote { Identifier = "garden", Medium = "Watercolour", Year = 2021, Story = "Botanical pink light in the garden, for a calm room." },
            new DemoNote { Identifier = "orange", Medium = "Acrylic", Year = 2022, Story = "A graphic orange band for a minimalist living room." },
            new DemoNote { Identifier = "reaching", Medium = "Oil", Year = 2020, Story = "A still figure reaching, large on a tall wall." },
            new DemoNote { Identifier = "fish", Medium = "Ink", Year = 2019, Story = "Still blue water with the fish, for a quiet room." },
            new DemoNote { Identifier = "seated", Medium = "Acrylic", Year = 2023, Story = "A seated figure in calm blue, for a medium wall." },
            new DemoNote { Identifier = "rocks", Medium = "Oil", Year = 2024, Story = "A coastal view on the rocks, bright and blue." },
            new DemoNote { Identifier = "ducks", Medium = "Watercolour", Year = 2021, Story = "Botanical green under the roses, for a small room." },
            new DemoNote { Identifier = "caravan", Medium = "Oil", Year = 2020, Story = "An evening caravan in a warm sunset." },
            new DemoNote { Identifier = "climb", Medium = "Acrylic", Year = 2022, Story = "A bold red climb, square and large enough for a gallery wall." },
            new DemoNote { Identifier = "letter", Medium = "Oil", Year = 2019, Story = "A quiet pink letter, calm enough for a small room." },
            new DemoNote { Identifier = "dress", Medium = "Acrylic", Year = 2025, Story = "A tall white dress for a narrow wall." },
            new DemoNote { Identifier = "bouquet", Medium = "Watercolour", Year = 2022, Story = "A botanical white bouquet for a living room." },
            new DemoNote { Identifier = "roses", Medium = "Oil", Year = 2024, Story = "Among the roses, botanical and calm, for a soft room." },
            new DemoNote { Identifier = "water", Medium = "Ink", Year = 2019, Story = "A coastal shore in pink light, small enough for a side wall." },
            new DemoNote { Identifier = "swallows", Medium = "Ink", Year = 2021, Story = "" },
            new DemoNote { Identifier = "study", Medium = "Acrylic", Year = 2020, Story = "" },
            new DemoNote { Identifier = "rider", Medium = "Oil", Year = 2023, Story = "" },
            new DemoNote { Identifier = "reclining", Medium = "Watercolour", Year = 2024, Story = "" },
            new DemoNote { Identifier = "open", Medium = "Acrylic", Year = 2025, Story = "" }
        };

        [MenuItem("Tools/Capstone/Populate Rich Demo Metadata")]
        public static void Populate()
        {
            CatalogueShelf shelf = AssetDatabase.LoadAssetAtPath<CatalogueShelf>(ShelfPath);
            if (shelf == null || shelf.Works == null)
            {
                Debug.LogError("The catalogue shelf was not found at " + ShelfPath);
                return;
            }

            int written = 0;
            for (int i = 0; i < Notes.Length; i++)
            {
                ArtworkData work = Find(shelf.Works, Notes[i].Identifier);
                if (work == null)
                {
                    Debug.LogWarning("No shelf record uses the identifier " + Notes[i].Identifier);
                    continue;
                }

                SerializedObject serialized = new SerializedObject(work);
                serialized.FindProperty("medium").stringValue = Notes[i].Medium;
                serialized.FindProperty("year").intValue = Notes[i].Year;
                serialized.FindProperty("storyNote").stringValue = Notes[i].Story;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(work);
                written++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Wrote demo medium, year, and story notes on " + written + " records. Profile, Standing, Wrap, Folded, and Hands stay blank.");
        }

        static ArtworkData Find(IReadOnlyList<ArtworkData> works, string identifier)
        {
            for (int i = 0; i < works.Count; i++)
            {
                ArtworkData work = works[i];
                if (work != null && work.Identifier == identifier)
                    return work;
            }

            return null;
        }
    }
}
