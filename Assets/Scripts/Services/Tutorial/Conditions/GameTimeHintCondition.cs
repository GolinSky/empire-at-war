using EmpireAtWar.Controllers.Game;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Met when the player pauses, resumes after a pause, speeds up, or returns to normal speed after.</summary>
    public sealed class GameTimeHintCondition : IHintCondition, IObserver<BattleState>, IObserver<GameSpeed>
    {
        public enum Kind
        {
            Paused = 0,
            Resumed = 1,
            SpeedUp = 2,
            NormalSpeed = 3,
        }

        private readonly SkirmishStateModel _battle;
        private readonly Kind _kind;
        private bool _wasPaused;
        private bool _wasFast;

        public bool IsMet { get; private set; }

        public GameTimeHintCondition(SkirmishStateModel battle, Kind kind)
        {
            _battle = battle;
            _kind = kind;
            if (IsSpeedKind) _battle.AddObserver((IObserver<GameSpeed>)this);
            else _battle.AddObserver((IObserver<BattleState>)this);
        }

        public void UpdateState(BattleState value)
        {
            if (value == BattleState.Paused)
            {
                _wasPaused = true;
                if (_kind == Kind.Paused) IsMet = true;
            }
            else if (value == BattleState.Running && _wasPaused && _kind == Kind.Resumed) IsMet = true;
        }

        public void UpdateState(GameSpeed value)
        {
            if (value == GameSpeed.Fast)
            {
                _wasFast = true;
                if (_kind == Kind.SpeedUp) IsMet = true;
            }
            else if (_wasFast && _kind == Kind.NormalSpeed) IsMet = true;
        }

        private bool IsSpeedKind => _kind == Kind.SpeedUp || _kind == Kind.NormalSpeed;

        public void Dispose()
        {
            if (IsSpeedKind) _battle.RemoveObserver((IObserver<GameSpeed>)this);
            else _battle.RemoveObserver((IObserver<BattleState>)this);
        }
    }
}
