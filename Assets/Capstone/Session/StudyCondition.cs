namespace Capstone.Session
{
    /// <summary>
    /// The two study conditions share one catalogue. Baseline keeps the stored facts and the story.
    /// It turns placement, questions, and ranking off. Any other condition leaves them on.
    /// </summary>
    public static class StudyCondition
    {
        public const string Baseline = "baseline";
        public const string Prototype = "prototype";
        public const string Catalogue = "catalogue";
        public const string Placement = "placement";
        public const string Dialogue = "dialogue";
        public const string Ranking = "ranking";

        public const string BaselineMessage = "This is the catalogue page. Placement, questions, and ranking stay off.";
        public const string PrototypeMessage = "Prototype. Placement, questions, and ranking are on.";

        public static bool Allows(string condition, string feature)
        {
            if (condition != Baseline)
                return true;
            return feature == Catalogue;
        }
    }
}
