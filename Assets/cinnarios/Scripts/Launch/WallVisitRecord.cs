namespace Cinnarios.Launch
{
    /// <summary>
    /// What the wall session decided, so the shop can keep it when the camera closes.
    /// Satisfied and unsure are both a finished try.
    /// </summary>
    public static class WallVisitRecord
    {
        public static string Outcome { get; private set; }
        public static string ImageFile { get; private set; }

        public static void Remember(string outcome, string imageFile)
        {
            Outcome = outcome;
            if (!string.IsNullOrEmpty(imageFile))
            {
                ImageFile = imageFile;
            }
        }

        public static void Clear()
        {
            Outcome = null;
            ImageFile = null;
        }
    }
}
