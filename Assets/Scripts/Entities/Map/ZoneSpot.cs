using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Entities.Map
{
    public readonly struct ZoneSpot
    {
        public Vector3 Center { get; }
        public PlayerId Owner { get; }
        public bool IsCapturable { get; }
        public bool IsMain { get; }

        public ZoneSpot(Vector3 center, PlayerId owner, bool isCapturable, bool isMain = false)
        {
            Center = center;
            Owner = owner;
            IsCapturable = isCapturable;
            IsMain = isMain;
        }
    }
}
