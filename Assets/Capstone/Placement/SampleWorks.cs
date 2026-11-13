using Capstone.Catalogue;

namespace Capstone.Placement
{
    public static class SampleWorks
    {
        public static Artwork[] All()
        {
            return new[]
            {
                new Artwork("blue", "Blue note", "A. Maker", 2019, "Oil", 50f, 40f, "Quiet", "Calm", "Blue", "Mid", "", "Painted for a small room."),
                new Artwork("red", "Red field", "B. Maker", 2021, "Acrylic", 80f, 60f, "Bold", "Bright", "Red", "High", "", "A wide field of red."),
                new Artwork("green", "Green study", "C. Maker", 2018, "Ink", 30f, 30f, "Soft", "Still", "Green", "Low", "", ""),
                new Artwork("linen", "Linen room", "A. Maker", 2020, "Oil", 45f, 35f, "Quiet", "Calm", "Blue", "Mid", "", "The same quiet blue, in a smaller size.")
            };
        }
    }
}
