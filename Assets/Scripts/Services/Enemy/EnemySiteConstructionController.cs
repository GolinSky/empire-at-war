using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.CaptureSites;
using Utilities.ScriptUtils.Time;
using Zenject;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>Builds the site facility as soon as the AI owns an empty capture site and can pay for it.</summary>
    public sealed class EnemySiteConstructionController : ITickable
    {
        private const float DECISION_INTERVAL = 3f;

        private readonly ICaptureSitesSystem _captureSites;
        private readonly ITimer _decisionTimer = TimerFactory.ConstructTimer(DECISION_INTERVAL);

        public EnemySiteConstructionController(ICaptureSitesSystem captureSites)
        {
            _captureSites = captureSites;
        }

        public void Tick()
        {
            if (!_decisionTimer.IsComplete)
            {
                return;
            }

            _decisionTimer.StartTimer();
            _captureSites.TryBuildOnOwnedSite(PlayerType.Opponent);
        }
    }
}
