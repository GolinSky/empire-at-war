using System.Collections.Generic;

namespace EmpireAtWar.Components.Hangar
{
    public interface IHangarData
    {
        IReadOnlyList<HangarBay> HangarBays { get; }
        float HangarInitialDelay { get; }
        float HangarLaunchInterval { get; }
    }
}
