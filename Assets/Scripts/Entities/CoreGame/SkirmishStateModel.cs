using EmpireAtWar.Utils;

namespace EmpireAtWar.Controllers.Game
{
    public sealed class SkirmishStateModel : INotifier<BattleState>, INotifier<GameSpeed>
    {
        private readonly ReplayNotifier<BattleState> _state = new ReplayNotifier<BattleState>(BattleState.NotInitialized);
        private readonly ReplayNotifier<GameSpeed> _speed = new ReplayNotifier<GameSpeed>(GameSpeed.Normal);

        public BattleState State => _state.Value;
        public GameSpeed Speed => _speed.Value;

        public void AddObserver(IObserver<BattleState> observer) => _state.AddObserver(observer);
        public void RemoveObserver(IObserver<BattleState> observer) => _state.RemoveObserver(observer);
        public void AddObserver(IObserver<GameSpeed> observer) => _speed.AddObserver(observer);
        public void RemoveObserver(IObserver<GameSpeed> observer) => _speed.RemoveObserver(observer);

        internal void SetState(BattleState state) => _state.Set(state);
        internal void SetSpeed(GameSpeed speed) => _speed.Set(speed);
    }
}
