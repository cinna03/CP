namespace Capstone.Ranking
{
    /// <summary>
    /// What the buyer did after looking at a work. Keep and save strengthen its tags.
    /// Dismiss and alternatives weaken them.
    /// </summary>
    public enum FeedbackAction
    {
        Keep,
        Save,
        Dismiss,
        Alternatives
    }
}
