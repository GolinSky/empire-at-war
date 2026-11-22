using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities
{
    /// <summary>The moment and geometry of a fired shot: <see cref="Direction"/> is planar, <see cref="Rise"/> climbs per unit.</summary>
    public readonly struct ShipAbilityFire
    {
        public readonly Vector3 Origin;
        public readonly Vector3 Direction;
        public readonly float Rise;
        public readonly float Range;

        public ShipAbilityFire(Vector3 origin, Vector3 direction, float rise, float range)
        {
            Origin = origin;
            Direction = direction;
            Rise = rise;
            Range = range;
        }
    }
}
