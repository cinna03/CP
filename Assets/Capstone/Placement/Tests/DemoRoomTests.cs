using System;
using NUnit.Framework;

namespace Capstone.Placement.Tests
{
    public class DemoRoomTests
    {
        [Test]
        public void ThreeRoomsGrowFromWarmToHall()
        {
            DemoRoom[] rooms = DemoRoom.Three();

            Assert.AreEqual(3, rooms.Length);
            Assert.AreEqual("Warm 2.4m", rooms[0].Name);
            Assert.AreEqual(2.4f, rooms[0].WidthMetres);
            Assert.Less(rooms[0].WidthMetres, rooms[1].WidthMetres);
            Assert.Less(rooms[1].WidthMetres, rooms[2].WidthMetres);
            Assert.Less(rooms[0].HeightMetres, rooms[2].HeightMetres);
            Assert.Less(rooms[0].ViewDistanceMetres(), rooms[2].ViewDistanceMetres());
        }

        [Test]
        public void ARoomDescribesItsWallInMetres()
        {
            string text = DemoRoom.Three()[0].Describe();

            StringAssert.Contains("2.4 m wide", text);
            StringAssert.Contains("2.4 m tall", text);
        }

        [Test]
        public void ARoomNeedsAPositiveWall()
        {
            ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(
                () => new DemoRoom("x", 0f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f));

            Assert.AreEqual("size", error.ParamName);
        }
    }
}
