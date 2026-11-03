using System.Linq;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Services.Ships;

namespace EmpireAtWar.Services.Analytics
{
    /// <summary>Reports each cleared Survival wave of the match tracked by <see cref="MatchAnalyticsTracker"/>.</summary>
    public sealed class SurvivalWaveAnalytics : IInitializable, ILateDisposable, IObserver<BattleState>
    {
        private readonly IAnalyticsService _analytics;
        private readonly MatchAnalyticsTracker _match;
        private readonly SurvivalModel _survival;
        private readonly BattleClock _clock;
        private readonly ILocalPlayer _localPlayer;
        private readonly IShipService _ships;
        private readonly INotifier<BattleState> _battleState;

        private bool _isTracking;
        private int _reportedWaves;

        public SurvivalWaveAnalytics(IAnalyticsService analytics, MatchAnalyticsTracker match, SurvivalModel survival,
            BattleClock clock, ILocalPlayer localPlayer, IShipService ships, INotifier<BattleState> battleState)
        {
            _analytics = analytics;
            _match = match;
            _survival = survival;
            _clock = clock;
            _localPlayer = localPlayer;
            _ships = ships;
            _battleState = battleState;
        }

        public void Initialize()
        {
            _battleState.AddObserver(this);
        }

        public void LateDispose()
        {
            _battleState.RemoveObserver(this);
            if (_isTracking) _survival.Changed -= HandleSurvivalChanged;
        }

        // Starts once the battle runs, so waves already cleared in a restored save are not reported again.
        public void UpdateState(BattleState state)
        {
            if (state != BattleState.Running || _isTracking) return;

            _isTracking = true;
            _reportedWaves = CountClearedWaves();
            _survival.Changed += HandleSurvivalChanged;
        }

        private void HandleSurvivalChanged()
        {
            int clearedWaves = CountClearedWaves();
            if (clearedWaves <= _reportedWaves) return;

            int playerShips = _ships.Ships.Count(ship => _localPlayer.IsFriendly(ship.Owner));
            for (int wave = _reportedWaves + 1; wave <= clearedWaves; wave++)
            {
                _analytics.RecordSurvivalWaveCompleted(
                    new SurvivalWaveCompletedData(_match.MatchId, wave, _clock.Elapsed, playerShips));
            }

            _reportedWaves = clearedWaves;
        }

        private int CountClearedWaves() =>
            _survival.Phase == SurvivalPhase.Won ? _survival.WaveIndex : _survival.WaveIndex - 1;
    }
}
