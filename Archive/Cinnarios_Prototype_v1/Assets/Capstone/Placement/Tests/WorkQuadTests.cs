using Capstone.Catalogue;
using NUnit.Framework;
using UnityEngine;

namespace Capstone.Placement.Tests
{
    public class WorkQuadTests
    {
        [Test]
        public void QuadUsesTheStoredMetres()
        {
            Artwork work = new Artwork("blue", "Blue note", "A. Maker", 2019, "Oil", 50f, 40f, "Quiet", "Calm", "Blue", "Mid", "", "Painted for a small room.");
            var face = GameObject.CreatePrimitive(PrimitiveType.Quad);

            WorkQuad.Fit(face.transform, work);

            Assert.AreEqual(0.40f, face.transform.localScale.x, 0.0001f);
            Assert.AreEqual(0.50f, face.transform.localScale.y, 0.0001f);
            Object.DestroyImmediate(face);
        }
    }
}
