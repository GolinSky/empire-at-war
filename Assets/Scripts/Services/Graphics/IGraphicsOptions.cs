using System.Collections.Generic;

namespace EmpireAtWar.Services.Graphics
{
    public interface IGraphicsOptions
    {
        IReadOnlyList<string> QualityPresets { get; }

        /// <summary>Maps a saved preset name to its quality level; unknown or empty names map to the project default.</summary>
        int ResolveQualityLevel(string preset);
    }
}
