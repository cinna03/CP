namespace Capstone.Session
{
    /// <summary>
    /// The 25 consent codes for the Kigali sample, one for each adult buyer.
    /// A blank value and any other text do not match. Matching ignores case and surrounding spaces.
    /// </summary>
    public static class ConsentCodes
    {
        public static readonly string[] All =
        {
            "AZURE", "INDIGO", "COBALT", "LINEN", "OCHRE",
            "SIENNA", "IVORY", "SAFFRON", "MARINE", "CEDAR",
            "MAPLE", "TERRA", "PEARL", "SLATE", "MOSS",
            "CORAL", "AMBER", "FLAX", "CLAY", "DUSK",
            "DAWN", "HARBOR", "ATLAS", "QUILL", "MURAL"
        };

        public static string Canonical(string typed)
        {
            if (string.IsNullOrWhiteSpace(typed))
                return null;

            string key = typed.Trim();
            for (int i = 0; i < All.Length; i++)
            {
                if (string.Equals(All[i], key, System.StringComparison.OrdinalIgnoreCase))
                    return All[i];
            }

            return null;
        }
    }
}
