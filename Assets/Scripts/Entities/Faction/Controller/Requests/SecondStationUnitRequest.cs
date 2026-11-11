using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Controllers.Factions
{
    /// <summary>A second space station: placed like a structure, starts at level 1 and levels up on its own.</summary>
    public sealed class SecondStationUnitRequest : UnitRequest
    {
        public const string REQUEST_ID = "SecondStation";

        public SecondStationUnitRequest(FactionData factionData) : base(factionData)
        {
        }

        public override string Id => REQUEST_ID;
    }
}
