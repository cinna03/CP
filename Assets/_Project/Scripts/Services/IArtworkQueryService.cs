using System.Collections.Generic;
using Capstone.App.Data;

namespace Capstone.App.Services
{
    /// <summary>
    /// Answers size, colour, and the story note from the record. An unknown question adds no fact.
    /// </summary>
    public interface IArtworkQueryService
    {
        string DescribeSize(ArtworkData work, int sizeIndex);
        string DescribeColour(ArtworkData work);
        string DescribeStory(ArtworkData work);
        string DescribePlacement(ArtworkData work, int sizeIndex);
        string DescribeTaste(ArtworkData work);
        ConsultReply Consult(ArtworkData work, int sizeIndex, FrameOption frame, IReadOnlyList<ArtworkData> catalogue, ArtworkSessionState visit, string question);
        string Answer(ArtworkData work, int sizeIndex, string question);
    }

    /// <summary>The written reply, the next question's replies, and any one-tap size or artwork actions.</summary>
    public sealed class ConsultReply
    {
        public string Text = "";
        public string[] Options = new string[0];
        public ConsultAction[] Actions = new ConsultAction[0];
    }

    /// <summary>A pill that resizes the current work or places another work on the wall.</summary>
    public sealed class ConsultAction
    {
        public string Label = "";
        public ArtworkData Work;
        public int SizeIndex = -1;
    }
}
