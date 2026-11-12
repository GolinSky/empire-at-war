using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>A matchup table with no opinions, so production tests see only the damage-matrix gain.</summary>
    public sealed class NeutralShipClassMatchups : IShipClassMatchups
    {
        public float GetPreference(ShipClass candidate, ForceComposition hostile) => 1f;
    }
}
