using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Editor
{
    /// <summary>One ship of one faction's roster: its data, the faction that builds it and its price there.</summary>
    public readonly struct ShipRoleEntry
    {
        public ShipData Ship { get; }
        public FactionType Faction { get; }
        public float Price { get; }
        /// <summary>The ship data lives in this faction's folder; a borrowed ship is rated with its owner.</summary>
        public bool IsOwner { get; }

        public ShipRoleEntry(ShipData ship, FactionType faction, float price, bool isOwner)
        {
            Ship = ship;
            Faction = faction;
            Price = price;
            IsOwner = isOwner;
        }
    }
}
