using System.Collections.Generic;

namespace EmpireAtWar.Models.Players
{
    public interface IPlayerRoster : IPlayerRelations
    {
        IReadOnlyList<PlayerSlot> Players { get; }
        PlayerSlot Get(PlayerId id);
    }
}
