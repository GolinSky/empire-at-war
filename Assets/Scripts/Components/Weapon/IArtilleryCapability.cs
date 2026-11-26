using EmpireAtWar.Entities.Units;

namespace EmpireAtWar.Components.Weapon
{
    public interface IArtilleryCapability
    {
        int ArtilleryRevision { get; }
        float GetArtilleryRange(ShipClass targetClass);
    }
}
