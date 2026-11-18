using Cinnarios.Catalogue.Met;

namespace Cinnarios.Catalogue.Filtering
{
    /// <summary>True size is the whole point, so works without a usable width and height are left out.</summary>
    public sealed class HasDimensionsFilter : IArtworkFilter
    {
        public string Reason => "no usable width and height";

        public bool Accepts(MetObjectData artwork) => MetMeasurementSelector.TryGetPaintingSize(artwork, out _);
    }
}
