using System;

namespace Cinnarios.Catalogue.Met
{
    /// <summary>
    /// Picks the measurement that describes the painting itself. The Met can list several
    /// (the painting, the frame, the sheet...), so preferred names are tried in order and
    /// anything describing a frame is never used.
    /// </summary>
    public static class MetMeasurementSelector
    {
        private static readonly string[] PreferredElementNames = { "Overall", "Canvas", "Panel", "Sheet", "Image" };

        public static bool TryGetPaintingSize(MetObjectData artwork, out MetElementMeasurements size)
        {
            size = null;
            if (artwork?.measurements == null)
            {
                return false;
            }

            foreach (string preferredName in PreferredElementNames)
            {
                foreach (MetMeasurement measurement in artwork.measurements)
                {
                    if (string.Equals(measurement?.elementName, preferredName, StringComparison.OrdinalIgnoreCase) && HasSize(measurement))
                    {
                        size = measurement.elementMeasurements;
                        return true;
                    }
                }
            }

            foreach (MetMeasurement measurement in artwork.measurements)
            {
                bool describesFrame = measurement?.elementName != null &&
                                      measurement.elementName.IndexOf("frame", StringComparison.OrdinalIgnoreCase) >= 0;
                if (!describesFrame && HasSize(measurement))
                {
                    size = measurement.elementMeasurements;
                    return true;
                }
            }

            return false;
        }

        private static bool HasSize(MetMeasurement measurement) =>
            measurement?.elementMeasurements != null &&
            measurement.elementMeasurements.Height > 0f &&
            measurement.elementMeasurements.Width > 0f;
    }
}
