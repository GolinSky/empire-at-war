using EmpireAtWar.Lifecycle;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Services.Networking.Online;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// Closes the battle's online connection on scene exit. Clients can recover their seat after exiting
    /// a live match, and a restart keeps the existing connection.
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
            if (_generation != _online.BattleGeneration || _online.IsReloading || _online.Role == OnlineRole.None) return;
            if (_online.Role == OnlineRole.Client && _online.IsInBattle && _online.HasRecovery) _online.SuspendBattle();
            else _online.Leave();
        }
    }
}
