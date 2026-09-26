using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Ship;
using Utilities.ScriptUtils.Time;
using Zenject;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// Builds the site facility once the AI owns an empty capture site, can pay for it,
    /// and has enough of a fleet that the investment does not starve its defense.
    /// </summary>
    public sealed class EnemySiteConstructionController : ITickable
    {
        private const float DECISION_INTERVAL = 3f;
        private const int MINIMUM_FLEET_SIZE = 2;

        private readonly ICaptureSitesSystem _captureSites;
        private readonly IShipService _shipService;
        private readonly ITimer _decisionTimer = TimerFactory.ConstructTimer(DECISION_INTERVAL);

        public EnemySiteConstructionController(ICaptureSitesSystem captureSites, IShipService shipService)
        {
            _captureSites = captureSites;
            _shipService = shipService;
        }

        public void Tick()
        {
            if (!_decisionTimer.IsComplete)
            {
                return;
            }

            _decisionTimer.StartTimer();
            if (CountOwnShips() >= MINIMUM_FLEET_SIZE)
            {
                _captureSites.TryBuildOnOwnedSite(PlayerType.Opponent);
            }
        }

        private int CountOwnShips()
        {
            int count = 0;
            foreach (IShipEntity ship in _shipService.Ships)
            {
                if (ship.PlayerType == PlayerType.Opponent)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
