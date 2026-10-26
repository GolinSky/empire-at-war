using System;
using EmpireAtWar.Entities.BaseEntity;

namespace EmpireAtWar.Entities.SpaceStation
{
    public interface IStationLevelFacade : IEntityFacade
    {
        event Action<int> LevelUpgraded;
        int CurrentLevel { get; }
    }
}
