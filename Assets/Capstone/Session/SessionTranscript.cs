using System.Text;

namespace Capstone.Session
{
    /// <summary>
    /// Prints the session that is already in memory. It adds no event the session did not store.
    /// An unmatched question is absent because the session does not log one.
    /// </summary>
    public static class SessionTranscript
    {
        public static string Format(BuyerSession session)
        {
            if (session == null)
                return string.Empty;

            StringBuilder text = new StringBuilder();
            text.Append("participant ");
            text.Append(session.ParticipantCode);
            text.Append('\n');
            text.Append("consent ");
            text.Append(session.ConsentLogged ? "marked" : "unmarked");
            for (int i = 0; i < session.Count; i++)
            {
                SessionEvent item = session.EventAt(i);
                text.Append('\n');
                text.Append(item.Kind);
                text.Append(' ');
                text.Append(item.ArtworkId);
                text.Append(' ');
                text.Append(item.Name);
                text.Append(' ');
                text.Append(item.Concern);
            }

            return text.ToString();
        }
    }
}
