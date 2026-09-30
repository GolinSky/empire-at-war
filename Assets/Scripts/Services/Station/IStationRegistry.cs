using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Services.Stations
{
    public interface IStationRegistry : IService
    {
        bool IsStationOperational(PlayerId owner);
        bool TryGetLivingStation(PlayerId owner, out IEntity station);
    }
}
