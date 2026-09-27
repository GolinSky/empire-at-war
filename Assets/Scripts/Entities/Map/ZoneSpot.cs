using EmpireAtWar.Models.Factions;
using UnityEngine;

namespace EmpireAtWar.Entities.Map
{
    public readonly struct ZoneSpot
    {
        public ZoneSpot(Vector3 center, PlayerType owner, bool isCapturable)
        {
            Center = center;
            Owner = owner;
            IsCapturable = isCapturable;
        }

        public Vector3 Center { get; }
        public PlayerType Owner { get; }
        public bool IsCapturable { get; }
    }
}
