using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Saves;
using UnityEngine;

namespace EmpireAtWar.Services.Diagnostics
{
    public sealed class BattleCrashContextTracker : IInitializable, ILateDisposable, ITickable, IObserver<BattleState>
    {
        private const float UPDATE_INTERVAL_SECONDS = 5f;

        private readonly ICrashContext _context;
        private readonly BattleDefinition _battle;
        private readonly ILocalPlayer _localPlayer;
        private readonly BattleClock _clock;
        private readonly IEntityLocator _entities;
        private readonly INotifier<BattleState> _battleState;
        private BattleState _state;
        private float _secondsSinceUpdate;

        public BattleCrashContextTracker(ICrashContext context, BattleDefinition battle, ILocalPlayer localPlayer,
            BattleClock clock, IEntityLocator entities, INotifier<BattleState> battleState)
        {
            _context = context;
            _battle = battle;
            _localPlayer = localPlayer;
            _clock = clock;
            _entities = entities;
            _battleState = battleState;
        }

        public void Initialize()
        {
            _context.BeginBattle(_battle.Mode, _battle.Map.Type, _battle.Map.Planet, _localPlayer.Slot.Faction);
            RecordContext();
            _battleState.AddObserver(this);
        }

        public void UpdateState(BattleState state)
        {
            _state = state;
            RecordContext();
        }

        public void Tick()
        {
            if (_state != BattleState.Running) return;
            _secondsSinceUpdate += Time.unscaledDeltaTime;
            if (_secondsSinceUpdate >= UPDATE_INTERVAL_SECONDS) RecordContext();
        }

        private void RecordContext()
        {
            _context.UpdateBattle(_state, _clock.Elapsed, _entities.Entities.Count);
            _secondsSinceUpdate = 0f;
        }

        public void LateDispose()
        {
            _battleState.RemoveObserver(this);
            _context.EndBattle();
        }
    }
}
