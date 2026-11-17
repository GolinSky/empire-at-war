using EmpireAtWar.Components.Ship.Health;

namespace EmpireAtWar.Components.Weapon
{
    public interface IArtilleryCapability
    {
        int ArtilleryRevision { get; }
        float GetArtilleryRange(ShipClass targetClass);

        /// <summary>True when a surviving main battery reaches this class, so the ship keeps facing it without backing off.</summary>
        bool HasMainBatteryCover(ShipClass targetClass);
    }
}
