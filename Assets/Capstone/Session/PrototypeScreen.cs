namespace Capstone.Session
{
    /// <summary>
    /// The three steps a buyer can open in the prototype.
    /// The catalogue condition stays on Works. Wall and Recommend open only in the prototype.
    /// </summary>
    public static class PrototypeScreen
    {
        public const string Works = "works";
        public const string Wall = "wall";
        public const string Recommend = "recommend";

        public static string Open(string condition, string step)
        {
            if (step != Wall && step != Recommend)
                return Works;
            if (condition == StudyCondition.Baseline)
                return Works;
            return step;
        }

        public static bool ShowsWorks(string step)
        {
            return step == Works;
        }

        public static bool ShowsWall(string step)
        {
            return step == Wall;
        }

        public static bool ShowsRecommend(string step)
        {
            return step == Recommend;
        }
    }
}
