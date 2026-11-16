using System.Collections.Generic;
using NUnit.Framework;

namespace Capstone.Placement.Tests
{
    public class ARSessionStateMachineTests
    {
        [Test]
        public void ASessionStartsIdle()
        {
            Assert.AreEqual(WallSessionState.Idle, new ARSessionStateMachine().Current);
        }

        [Test]
        public void TheUsualPathScansSettlesAndPlaces()
        {
            var machine = new ARSessionStateMachine();
            Assert.IsTrue(machine.MoveTo(WallSessionState.Scanning));
            Assert.IsTrue(machine.MoveTo(WallSessionState.Settling));
            Assert.IsTrue(machine.MoveTo(WallSessionState.Placed));
            Assert.AreEqual(WallSessionState.Placed, machine.Current);
            Assert.AreEqual(0, machine.RefusedMoves);
        }

        [Test]
        public void IdleCannotJumpToSettling()
        {
            var machine = new ARSessionStateMachine();
            Assert.IsFalse(machine.MoveTo(WallSessionState.Settling));
            Assert.AreEqual(WallSessionState.Idle, machine.Current);
            Assert.AreEqual(1, machine.RefusedMoves);
        }

        [Test]
        public void PlacedCannotGoBackToSettlingWithoutScanning()
        {
            var machine = new ARSessionStateMachine(WallSessionState.Placed);
            Assert.IsFalse(machine.MoveTo(WallSessionState.Settling));
            Assert.IsTrue(machine.MoveTo(WallSessionState.Scanning));
        }

        [Test]
        public void ChangesAreReportedInOrderAndRepeatsAreSilent()
        {
            var machine = new ARSessionStateMachine();
            var seen = new List<string>();
            machine.Changed += (from, to) => seen.Add(from + ">" + to);
            machine.MoveTo(WallSessionState.Scanning);
            machine.MoveTo(WallSessionState.Scanning);
            machine.MoveTo(WallSessionState.Placed);
            CollectionAssert.AreEqual(new[] { "Idle>Scanning", "Scanning>Placed" }, seen);
        }

        [Test]
        public void ResetReturnsToIdleFromAnyState()
        {
            var machine = new ARSessionStateMachine(WallSessionState.Settling);
            machine.Reset();
            Assert.AreEqual(WallSessionState.Idle, machine.Current);
        }

        [Test]
        public void TheEditorCanPlaceWithoutScanning()
        {
            Assert.IsTrue(ARSessionStateMachine.CanMove(WallSessionState.Idle, WallSessionState.Placed));
        }
    }
}
