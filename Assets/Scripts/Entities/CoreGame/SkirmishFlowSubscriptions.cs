using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Menu;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Controllers.Game
{
    public sealed class SkirmishFlowSubscriptions : IInitializable, ILateDisposable
    {
        private readonly SkirmishOrchestrator _orchestrator;
        private readonly INotifier<BattleResult> _battleVictoryNotifier;
        private readonly IUserStateNotifier _userStateNotifier;

        public SkirmishFlowSubscriptions(
            SkirmishOrchestrator orchestrator,
            INotifier<BattleResult> battleVictoryNotifier,
            IUserStateNotifier userStateNotifier)
        {
            _orchestrator = orchestrator;
            _battleVictoryNotifier = battleVictoryNotifier;
            _userStateNotifier = userStateNotifier;
        }

        public void Initialize()
        {
            _userStateNotifier.AddObserver(_orchestrator);
            _battleVictoryNotifier.AddObserver(_orchestrator);
        }

        public void LateDispose()
        {
            _userStateNotifier.RemoveObserver(_orchestrator);
            _battleVictoryNotifier.RemoveObserver(_orchestrator);
        }
    }
}
