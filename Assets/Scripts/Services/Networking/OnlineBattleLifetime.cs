using EmpireAtWar.Lifecycle;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Services.Networking.Online;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// The online session ends with its battle scene, so the next battle starts offline — unless the match is
    /// restarting, when every PC reloads the battle and stays connected.
    /// </summary>
    public sealed class OnlineBattleLifetime : IInitializable, ILateDisposable, IObserver<BattleState>
    {
        private readonly OnlineMatchService _online;
        private readonly int _generation;
        private readonly INotifier<BattleState> _battleState;

        public OnlineBattleLifetime(OnlineMatchService online, INotifier<BattleState> battleState)
        {
            _online = online;
            _generation = online.BattleGeneration;
            _battleState = battleState;
        }

        public void Initialize() => _battleState.AddObserver(this);

        public void UpdateState(BattleState state)
        {
            if (state == BattleState.Ended && _generation == _online.BattleGeneration) _online.MarkMatchEnded();
        }

        public void LateDispose()
        {
            _battleState.RemoveObserver(this);
            if (_generation == _online.BattleGeneration && !_online.IsReloading && _online.Role != OnlineRole.None) _online.Leave();
        }
    }
}
