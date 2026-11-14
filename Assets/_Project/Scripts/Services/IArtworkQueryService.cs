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
        string Answer(ArtworkData work, int sizeIndex, string question);
    }
}
