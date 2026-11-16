using System.Globalization;

namespace Capstone.App.Data
{
    /// <summary>Why a buyer's own piece cannot be placed yet.</summary>
    public enum OwnPieceProblem
    {
        None,
        MissingPhoto,
        MissingSize,
        TooSmall,
        TooLarge
    }

    /// <summary>
    /// Checks for "try your own piece": a photo of the work plus its painted width and height.
    /// Pure C#, so every rule is covered by EditMode tests.
    /// </summary>
    public static class OwnPieceRules
    {
        public const float MinCentimetres = 5f;
        public const float MaxCentimetres = 400f;

        /// <summary>How far, as a fraction, the photo's shape may differ from the entered size before we warn.</summary>
        public const float ShapeTolerance = 0.12f;

        /// <summary>Reads a centimetre value typed with a point or a comma ("60", "60.5", "60,5").</summary>
        public static bool TryParseCentimetres(string text, out float centimetres)
        {
            centimetres = 0f;
            if (string.IsNullOrWhiteSpace(text))
                return false;
            string clean = text.Trim().Replace(',', '.').Replace("cm", "").Trim();
            return float.TryParse(clean, NumberStyles.Float, CultureInfo.InvariantCulture, out centimetres);
        }

        public static OwnPieceProblem Validate(bool hasPhoto, string widthText, string heightText, out float widthCm, out float heightCm)
        {
            widthCm = 0f;
            heightCm = 0f;
            if (!hasPhoto)
                return OwnPieceProblem.MissingPhoto;
            if (!TryParseCentimetres(widthText, out widthCm) || !TryParseCentimetres(heightText, out heightCm))
                return OwnPieceProblem.MissingSize;
            if (widthCm < MinCentimetres || heightCm < MinCentimetres)
                return OwnPieceProblem.TooSmall;
            if (widthCm > MaxCentimetres || heightCm > MaxCentimetres)
                return OwnPieceProblem.TooLarge;
            return OwnPieceProblem.None;
        }

        /// <summary>
        /// Relative difference between the photo's aspect ratio and the entered size's aspect ratio.
        /// 0 means the same shape; 0.2 means one is 20% wider for its height than the other.
        /// A large value usually means the width and height were swapped or the photo includes a wall.
        /// </summary>
        public static float ShapeDifference(int photoWidth, int photoHeight, float widthCm, float heightCm)
        {
            if (photoWidth <= 0 || photoHeight <= 0 || widthCm <= 0f || heightCm <= 0f)
                return 0f;
            float photo = photoWidth / (float)photoHeight;
            float entered = widthCm / heightCm;
            float larger = photo > entered ? photo : entered;
            float smaller = photo > entered ? entered : photo;
            return larger / smaller - 1f;
        }

        public static bool ShapeMismatch(int photoWidth, int photoHeight, float widthCm, float heightCm)
        {
            return ShapeDifference(photoWidth, photoHeight, widthCm, heightCm) > ShapeTolerance;
        }

        /// <summary>True when swapping width and height would match the photo much better.</summary>
        public static bool LooksSwapped(int photoWidth, int photoHeight, float widthCm, float heightCm)
        {
            return ShapeMismatch(photoWidth, photoHeight, widthCm, heightCm)
                && !ShapeMismatch(photoWidth, photoHeight, heightCm, widthCm);
        }

        public static string Message(OwnPieceProblem problem)
        {
            switch (problem)
            {
                case OwnPieceProblem.MissingPhoto:
                    return "Choose a clear photo of the piece first.";
                case OwnPieceProblem.MissingSize:
                    return "Enter the width and height in centimetres.";
                case OwnPieceProblem.TooSmall:
                    return "Each side must be at least " + MinCentimetres.ToString(CultureInfo.InvariantCulture) + " cm.";
                case OwnPieceProblem.TooLarge:
                    return "Each side must be at most " + MaxCentimetres.ToString(CultureInfo.InvariantCulture) + " cm.";
                default:
                    return "";
            }
        }
    }
}
