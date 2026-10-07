using EmpireAtWar.Components.Ship.Health;

namespace EmpireAtWar.Entities.EnemyFaction.Models.Combat
{
    /// <summary>Designer knowledge of which ship classes are strong or weak against which.</summary>
    public interface IShipClassMatchups
    {
        /// <summary>
        /// Production preference for <paramref name="candidate"/> against <paramref name="hostile"/>: above 1 when
        /// the hostile force is made of classes it is strong against, below 1 for classes it is weak against.
        /// </summary>
        float GetPreference(ShipClass candidate, ForceComposition hostile);
    }
}
