using System;
using System.Collections.Generic;
using Capstone.App.Data;

namespace Capstone.App.Services
{
    public enum VisitDecision
    {
        Keep,
        Save,
        Dismiss
    }

    /// <summary>
    /// Chooses three other works after a decision. Weights last for one visit and clear when the visit ends.
    /// </summary>
    public interface IRecommendationEngine
    {
        void Apply(ArtworkData current, VisitDecision decision);
        IReadOnlyList<ArtworkData> Three(IReadOnlyList<ArtworkData> works, ArtworkData current, VisitDecision decision, IReadOnlyList<string> dismissed, UserTasteProfile taste);
        string Heading(VisitDecision decision);
        void Clear();
    }
}
