using System;

namespace EmpireAtWar.Entities.Game
{
    [Flags]
    public enum BattleFeatures
    {
        None = 0,
        SuperWeapons = 1,
        CaptureSites = 2,
        MiningSites = 4,
        Economy = 8,
        Fog = 16,
        All = SuperWeapons | CaptureSites | MiningSites | Economy | Fog
    }
}
