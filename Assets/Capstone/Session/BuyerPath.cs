namespace Capstone.Session
{
    /// <summary>
    /// The buyer order for one session. The camera opens only when a chosen work is placed.
    /// A question comes after that placement. Keep, save, or dismiss is the rank step.
    /// A chosen recommendation returns to the wall. The buyer can set tags again or end.
    /// The five uncertainty questions are not a step.
    /// </summary>
    public static class BuyerPath
    {
        public const string Tags = "tags";
        public const string Catalogue = "catalogue";
        public const string Wall = "wall";
        public const string Question = "question";
        public const string Rank = "rank";
        public const string Ended = "ended";

        public const string Continue = "continue";
        public const string Place = "place";
        public const string Placed = "placed";
        public const string Send = "send";
        public const string Skip = "skip";
        public const string ChangeTags = "change-tags";
        public const string End = "end";

        public static string Start()
        {
            return Tags;
        }

        public static string Next(string step, string action)
        {
            if (step == Tags && action == Continue)
                return Catalogue;
            if (step == Catalogue && action == Place)
                return Wall;
            if (step == Wall && action == Placed)
                return Question;
            if (step == Question && (action == Send || action == Skip))
                return Rank;
            if (step == Rank && action == Place)
                return Wall;
            if (step == Rank && action == ChangeTags)
                return Tags;
            if (step == Rank && action == End)
                return Ended;
            return step;
        }

        public static bool OpensCamera(string step)
        {
            return step == Wall;
        }
    }
}
