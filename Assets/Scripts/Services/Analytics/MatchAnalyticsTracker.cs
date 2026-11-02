using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Menu;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Services.Ships;
using EmpireAtWar.Ship;

namespace EmpireAtWar.Services.Analytics
{
    /// <summary>
    /// Reports one battle scene as one match: started when the battle first runs, then completed by the
    /// battle result or abandoned when the scene closes without one.
    /// </summary>
    public sealed class MatchAnalyticsTracker : IInitializable, ILateDisposable,
        IObserver<BattleState>, IObserver<BattleResult>, IObserver<UserNotifierState>
    {
        private readonly IAnalyticsService _analytics;
        private readonly BattleDefinition _battle;
        private readonly GameData _game;
        private readonly BattleClock _clock;
        private readonly ILocalPlayer _localPlayer;
        private readonly IShipService _ships;
        private readonly INotifier<BattleState> _battleState;
        private readonly INotifier<BattleResult> _battleResult;
        private readonly IUserStateNotifier _userState;

        private bool _isLoadedSave;
        private bool _isFinished;
        // A scene closed without a pause-menu choice means the application is quitting.
        private MatchAbandonReason _exitReason = MatchAbandonReason.QuitApplication;
        private int _shipsDestroyed;
        private int _shipsLost;

        /// <summary>Null until the battle first runs.</summary>
        public string MatchId { get; private set; }

        private bool IsStarted => MatchId != null;

        public MatchAnalyticsTracker(IAnalyticsService analytics, BattleDefinition battle, GameData game,
            BattleClock clock, ILocalPlayer localPlayer, IShipService ships, INotifier<BattleState> battleState,
            INotifier<BattleResult> battleResult, IUserStateNotifier userState)
        {
            _analytics = analytics;
            _battle = battle;
            _game = game;
            _clock = clock;
            _localPlayer = localPlayer;
            _ships = ships;
            _battleState = battleState;
            _battleResult = battleResult;
            _userState = userState;
        }

        public void Initialize()
        {
            // The restore step consumes the pending save later in the startup sequence.
            _isLoadedSave = _game.PendingBattleSave != null;
            _ships.ShipRemoved += HandleShipRemoved;
            _userState.AddObserver(this);
            _battleResult.AddObserver(this);
            _battleState.AddObserver(this);
        }

        public void LateDispose()
        {
            _battleState.RemoveObserver(this);
            _battleResult.RemoveObserver(this);
            _userState.RemoveObserver(this);
            _ships.ShipRemoved -= HandleShipRemoved;
            if (!IsStarted || _isFinished) return;

            _analytics.RecordMatchAbandoned(new MatchAbandonedData(MatchId, _battle.Mode, _clock.Elapsed, _exitReason));
            _analytics.Flush();
        }

        public void UpdateState(BattleState state)
        {
            if (state != BattleState.Running || IsStarted) return;

            MatchId = Guid.NewGuid().ToString("N");
            MapSpec map = _battle.Map;
            _analytics.RecordMatchStarted(new MatchStartedData(MatchId, _battle.Mode, map.Type, map.Planet, map.Size,
                _battle.VictoryCondition, _localPlayer.Slot.Faction, GetOpponentFactions(),
                _battle.Slots.Count(slot => slot.IsAi), _battle.StartingMoney, _battle.InitialStationLevel,
                _isLoadedSave));
        }

        public void UpdateState(BattleResult result)
        {
            if (!IsStarted || _isFinished) return;

            _isFinished = true;
            _analytics.RecordMatchCompleted(new MatchCompletedData(MatchId, result.Mode, result.Outcome,
                result.VictoryCondition, result.PlayerFaction, result.EnemyFactions, _clock.Elapsed,
                result.PlayerShipCount, result.EnemyShipCount, result.IsPlayerBaseAlive, result.IsEnemyBaseAlive,
                _shipsDestroyed, _shipsLost));
            _analytics.Flush();
        }

        public void UpdateState(UserNotifierState state)
        {
            if (state == UserNotifierState.RestartBattle) _exitReason = MatchAbandonReason.Restart;
            else if (state == UserNotifierState.ExitGame) _exitReason = MatchAbandonReason.QuitToMenu;
        }

        // Ships leave the service only when destroyed until the scene tears down, which happens after the match is decided.
        private void HandleShipRemoved(IShipEntity ship)
        {
            if (!IsStarted || _isFinished) return;
            if (_localPlayer.IsFriendly(ship.Owner)) _shipsLost++;
            else _shipsDestroyed++;
        }

        private IReadOnlyList<FactionType> GetOpponentFactions() => _battle.Slots
            .Where(slot => !_localPlayer.IsFriendly(slot.Id))
            .Select(slot => slot.Faction)
            .Distinct()
            .ToArray();
    }
}
