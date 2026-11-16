using System;

namespace Capstone.Placement
{
    /// <summary>
    /// One editor room. The wall size is metres. The artwork placed on it keeps its own stored centimetres.
    /// </summary>
    public sealed class DemoRoom
    {
        public const string WallObjectName = "Demo wall";

        public DemoRoom(
            string name,
            float widthMetres,
            float heightMetres,
            float depthMetres,
            float wallR,
            float wallG,
            float wallB,
            float floorR,
            float floorG,
            float floorB)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A room needs a name.", "name");
            if (widthMetres <= 0f || heightMetres <= 0f || depthMetres <= 0f)
                throw new ArgumentOutOfRangeException("size", "A room wall needs a positive size.");

            Name = name;
            WidthMetres = widthMetres;
            HeightMetres = heightMetres;
            DepthMetres = depthMetres;
            WallR = wallR;
            WallG = wallG;
            WallB = wallB;
            FloorR = floorR;
            FloorG = floorG;
            FloorB = floorB;
        }

        public string Name { get; }

        public float WidthMetres { get; }

        public float HeightMetres { get; }

        public float DepthMetres { get; }

        public float WallR { get; }

        public float WallG { get; }

        public float WallB { get; }

        public float FloorR { get; }

        public float FloorG { get; }

        public float FloorB { get; }

        public static DemoRoom[] Three()
        {
            return new DemoRoom[]
            {
                new DemoRoom("Warm 2.4m", 2.4f, 2.4f, 3.2f, 0.72f, 0.42f, 0.24f, 0.40f, 0.24f, 0.14f),
                new DemoRoom("Gallery 4.2m", 4.2f, 3.2f, 5f, 0.94f, 0.93f, 0.90f, 0.72f, 0.72f, 0.70f),
                new DemoRoom("Hall 7m", 7f, 4f, 8f, 0.13f, 0.16f, 0.24f, 0.08f, 0.08f, 0.10f),
            };
        }

        public float ViewDistanceMetres()
        {
            float span = WidthMetres > HeightMetres ? WidthMetres : HeightMetres;
            return span * 0.95f + 0.4f;
        }

        public string Describe()
        {
            return Name
                + ". The wall is "
                + WidthMetres.ToString("0.0")
                + " m wide and "
                + HeightMetres.ToString("0.0")
                + " m tall.";
        }
    }
}
