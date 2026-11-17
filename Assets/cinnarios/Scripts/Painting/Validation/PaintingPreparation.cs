using Cinnarios.Painting.Data;
using UnityEngine;

namespace Cinnarios.Painting.Validation
{
    /// <summary>
    /// The single gate every painting passes through before it is shown: runs the image fit
    /// check and hands back a painting that is safe to build, with the crop to apply.
    /// </summary>
    public static class PaintingPreparation
    {
        private static readonly ImageFitCheck FitCheck = new ImageFitCheck();

        public static bool TryPrepare(PaintingSpec recorded, out PaintingSpec ready, out ImageFitResult fit)
        {
            fit = FitCheck.Evaluate(recorded.Image.width, recorded.Image.height, recorded.Dimensions);
            if (!fit.IsUsable)
            {
                ready = null;
                return false;
            }

            ready = fit.Status == ImageFitStatus.DimensionsSwapped ? recorded.WithDimensions(fit.Dimensions) : recorded;
            return true;
        }
    }
}
