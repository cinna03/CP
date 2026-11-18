using System;

namespace Cinnarios.Catalogue.Met
{
    // These classes mirror the parts of the Met Collection API's JSON that we use, so Unity's
    // JsonUtility can read it. Field names must match the JSON keys exactly, which is why
    // they don't follow the usual C# naming style.

    /// <summary>One object from GET /public/collection/v1/objects/{objectID}.</summary>
    [Serializable]
    public sealed class MetObjectData
    {
        public int objectID;
        public bool isPublicDomain;
        public string primaryImage;
        public string primaryImageSmall;
        public string title;
        public string artistDisplayName;
        public string objectDate;
        public string medium;
        public string classification;
        public string objectName;
        public string creditLine;
        public MetMeasurement[] measurements;
    }

    /// <summary>One measured part of an object, for example "Overall" or "Framed".</summary>
    [Serializable]
    public sealed class MetMeasurement
    {
        public string elementName;
        public MetElementMeasurements elementMeasurements;
    }

    /// <summary>Sizes in centimetres. Depth is 0 when the Met did not record it.</summary>
    [Serializable]
    public sealed class MetElementMeasurements
    {
        public float Height;
        public float Width;
        public float Depth;
    }
}
