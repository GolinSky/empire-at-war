using System;
using UnityEngine;

namespace EmpireAtWar.Entities.Units.Matchups
{
    /// <summary>The units one side of a matchup rule covers: any of these classes, or any unit with one of these roles.</summary>
    [Serializable]
    public struct MatchupSide
    {
        [SerializeField] private ShipClass[] classes;
        [SerializeField] private UnitRole roles;

        public ShipClass[] Classes => classes ?? Array.Empty<ShipClass>();
        public UnitRole Roles => roles;

        public MatchupSide(ShipClass[] classes, UnitRole roles = UnitRole.None)
        {
            this.classes = classes;
            this.roles = roles;
        }

        public bool HasClass(ShipClass shipClass) => Array.IndexOf(Classes, shipClass) >= 0;

        public bool HasRole(UnitRole unitRoles) => (roles & unitRoles) != UnitRole.None;

        public bool Matches(ShipClass shipClass, UnitRole unitRoles) => HasClass(shipClass) || HasRole(unitRoles);
    }
}
