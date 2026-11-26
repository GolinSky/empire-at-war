using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.Units;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>A matchup table with no opinions, so production tests see only the damage-matrix gain.</summary>
    public sealed class NeutralShipClassMatchups : IShipClassMatchups
    {
        public float GetPreference(ShipClass candidate, ForceComposition hostile) => 1f;
    }
}
