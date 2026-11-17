namespace Cinnarios.Painting.Validation
{
    /// <summary>How well a photo's proportions match the real painting's proportions.</summary>
    public enum ImageFitStatus
    {
        /// <summary>Close enough to use as it is.</summary>
        Match,

        /// <summary>Slightly off: show a centred part of the image so it fits exactly.</summary>
        CropNeeded,

        /// <summary>The photo fits once width and height are swapped, so the recorded size was probably the wrong way round.</summary>
        DimensionsSwapped,

        /// <summary>Too far off to fix here: the painting should not be shown.</summary>
        Mismatch
    }
}
