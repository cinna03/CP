using System;
using System.Collections.Generic;

namespace Capstone.Session
{
    /// <summary>
    /// The 25 entrance codes. The same list admits a buyer on the entrance screen.
    /// </summary>
    public static class EntranceCodes
    {
        static readonly string[] Codes =
        {
            "AZURE", "LINEN", "OCHRE", "IVORY", "CEDAR",
            "MAPLE", "TERRA", "PEARL", "SLATE", "CORAL",
            "AMBER", "ATLAS", "QUILL", "MURAL", "BLUSH",
            "CREAM", "EBONY", "FRAME", "GLAZE", "HONEY",
            "LILAC", "OLIVE", "ROUGE", "SEPIA", "UMBER"
        };

        public static IReadOnlyList<string> All => Codes;

        public static bool Accepts(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            string asked = code.Trim();
            for (int i = 0; i < Codes.Length; i++)
            {
                if (string.Equals(Codes[i], asked, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
