using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Controllers.Factions
{
    /// <summary>Upgrades the second station from <see cref="Level"/> to the next level.</summary>
    public sealed class SecondStationLevelUnitRequest : UnitRequest
    {
        public int Level { get; }

        public SecondStationLevelUnitRequest(FactionData factionData, int level) : base(factionData) => Level = level;

        public override string Id => $"SecondStationLevel{Level}";
    }
}
