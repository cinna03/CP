using Capstone.Catalogue;

namespace Capstone.Ranking
{
    /// <summary>
    /// Applies one buyer reaction to the preference profile. The next rank reads that same profile.
    /// </summary>
    public sealed class FeedbackLoop
    {
        public FeedbackLoop(PreferenceProfile profile)
        {
            Profile = profile ?? PreferenceProfile.FromInitialTags(string.Empty, string.Empty, string.Empty);
        }

        public PreferenceProfile Profile { get; }

        public void Accept(Artwork artwork)
        {
            Profile.UpdateFromFeedback(artwork, FeedbackAction.Keep);
        }

        public void Save(Artwork artwork)
        {
            Profile.UpdateFromFeedback(artwork, FeedbackAction.Save);
        }

        public void Dismiss(Artwork artwork)
        {
            Profile.UpdateFromFeedback(artwork, FeedbackAction.Dismiss);
        }

        public void RequestAlternatives(Artwork artwork)
        {
            Profile.UpdateFromFeedback(artwork, FeedbackAction.Alternatives);
        }
    }
}
