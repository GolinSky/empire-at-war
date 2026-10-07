using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Entities.SpaceStation
{
    public sealed class StationFootprintFacade : INavigationFootprintFacade
    {
        // The same radius the map registers as the station's navigation obstacle.
        public float NavigationRadius { get; }

        public StationFootprintFacade(MapGenerationSettings settings, FactionType factionType)
        {
            NavigationRadius = settings.GetStationRadius(factionType);
        }
    }
}
