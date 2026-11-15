using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Services.Stations
{
    public interface IStationRegistry : IService
    {
        /// <summary>Checks for a living station or, when allowed, a Golan III or Empress with production capacity.</summary>
        bool HasProductionBase(PlayerId owner, bool stationsOnly = false);

        bool TryGetProductionBase(PlayerId owner, out IEntity productionBase, bool stationsOnly = false);

        bool TryGetLivingStation(PlayerId owner, out IEntity station);
    }
}
