using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Entities.EnemyFaction.Controllers;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.Squadrons;

namespace EmpireAtWar.Services.Player
{
    /// <summary>
    /// Scene-wide lookup for services that live inside one player's sub-container.
    /// Each player context registers its own services while it initializes.
    /// </summary>
    public interface IPlayerRegistry
    {
        void RegisterSiteBuilder(PlayerId owner, ISiteFacilityBuilder builder);

        void RegisterAiReinforcement(PlayerId owner, IEnemyReinforcementObserver reinforcement);

        void RegisterReinforcementReserve(PlayerId owner, ReinforcementModel reserve);

        void UnregisterReinforcementReserve(PlayerId owner);

        void RegisterAiStationCheat(PlayerId owner, IEnemyStationCheat station);

        void RegisterStationSpawner(PlayerId owner, IStationSpawner spawner);

        void RegisterSquadronPrewarmer(PlayerId owner, ISquadronPrewarmer prewarmer);

        void RegisterEconomy(PlayerId owner, IEconomyProvider economy);

        void UnregisterSiteBuilder(PlayerId owner);

        void UnregisterAiReinforcement(PlayerId owner);

        void UnregisterAiStationCheat(PlayerId owner);

        void UnregisterStationSpawner(PlayerId owner);

        void UnregisterSquadronPrewarmer(PlayerId owner);

        void UnregisterEconomy(PlayerId owner);

        ISiteFacilityBuilder GetSiteBuilder(PlayerId owner);

        IStationSpawner GetStationSpawner(PlayerId owner);

        ISquadronPrewarmer GetSquadronPrewarmer(PlayerId owner);

        IEconomyProvider GetEconomy(PlayerId owner);

        IEnemyStationCheat GetAiStationCheat(PlayerId owner);

        /// <summary>Builds still arriving or purchased units waiting for deployment.</summary>
        bool HasPendingReinforcement(PlayerId owner);
    }
}
