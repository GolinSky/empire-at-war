using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Entities.Map
{
    public readonly struct ZoneSpot
    {
        public ZoneSpot(Vector3 center, PlayerId owner, bool isCapturable)
        {
            Center = center;
            Owner = owner;
            IsCapturable = isCapturable;
        }

        public Vector3 Center { get; }
        public PlayerId Owner { get; }
        public bool IsCapturable { get; }
    }
}
