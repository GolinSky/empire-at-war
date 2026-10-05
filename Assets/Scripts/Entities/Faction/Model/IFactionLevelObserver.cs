using System;

namespace EmpireAtWar.Models.Factions
{
    /// <summary>Station level of one player's faction, shared by the human and AI faction models.</summary>
    public interface IFactionLevelObserver
    {
        event Action<int> OnLevelUpgraded;

        int CurrentLevel { get; }
    }
}
