using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Services.Stations
{
    public interface IStationRegistry : IService
    {
        /// <summary>True while the owner keeps a living station, Golan III or Empress to buy units from.</summary>
        bool HasProductionBase(PlayerId owner);

        bool TryGetProductionBase(PlayerId owner, out IEntity productionBase);

        bool TryGetLivingStation(PlayerId owner, out IEntity station);
    }
}
