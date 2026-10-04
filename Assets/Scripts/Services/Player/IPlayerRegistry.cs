using EmpireAtWar.Entities.EnemyFaction.Controllers;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.CaptureSites;

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

        void RegisterStationSpawner(PlayerId owner, IStationSpawner spawner);

        void UnregisterSiteBuilder(PlayerId owner);

        void UnregisterAiReinforcement(PlayerId owner);

        void UnregisterStationSpawner(PlayerId owner);

        ISiteFacilityBuilder GetSiteBuilder(PlayerId owner);

        IStationSpawner GetStationSpawner(PlayerId owner);

        /// <summary>Only AI players queue builds that still arrive after their station died; humans never do.</summary>
        bool HasPendingReinforcement(PlayerId owner);
    }
}
