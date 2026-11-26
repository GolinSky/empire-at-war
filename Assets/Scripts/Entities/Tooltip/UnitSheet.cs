using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Entities.Tooltip
{
    /// <summary>Everything a unit tooltip shows about a ship or squadron type, read from its data assets.</summary>
    public sealed class UnitSheet
    {
        public FactionData FactionData { get; }
        public FactionType Faction { get; }
        public ShipClass ShipClass { get; }
        public UnitRole Roles { get; }
        public float Hull { get; }
        public float Shields { get; }
        public float ShieldRegenPerSecond { get; }

        /// <summary>Ships take faction hull and shield research; squadron health ignores it.</summary>
        public bool TakesResearch { get; }

        public UnitSheet(FactionData factionData, FactionType faction, ShipClass shipClass, UnitRole roles, float hull,
            float shields, float shieldRegenPerSecond, bool takesResearch)
        {
            FactionData = factionData;
            Faction = faction;
            ShipClass = shipClass;
            Roles = roles;
            Hull = hull;
            Shields = shields;
            ShieldRegenPerSecond = shieldRegenPerSecond;
            TakesResearch = takesResearch;
        }
    }
}
