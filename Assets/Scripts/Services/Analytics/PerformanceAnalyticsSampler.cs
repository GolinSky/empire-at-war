using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Timing;
using EmpireAtWar.Services.Squadrons;
using UnityEngine;
using UnityEngine.Profiling;

namespace EmpireAtWar.Services.Analytics
{
    /// <summary>
    /// Sends one performance sample per minute of running battle plus a final partial one when the match ends.
    /// Paused and loading frames are excluded.
    /// </summary>
    public sealed class PerformanceAnalyticsSampler : IInitializable, ILateDisposable, ITickable,
        IObserver<BattleState>
    {
        private const float SAMPLE_WINDOW_SECONDS = 60f;
        private const float SQUADRON_LOG_INTERVAL_SECONDS = 10f;
        private const long BYTES_PER_MB = 1024 * 1024;

        private readonly IAnalyticsService _analytics;
        private readonly MatchAnalyticsTracker _match;
        private readonly IEntityLocator _entities;
        private readonly INotifier<BattleState> _battleState;
        private readonly FrameTimeHistogram _frames = new FrameTimeHistogram();
        private readonly SquadronRegistry _squadrons;
        private readonly BattleDefinition _battle;
        private float _squadronLogSeconds;

        private BattleState _state;
        private bool _skipTransitionFrame;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private readonly BattleFpsHistory _history;
#endif

        public PerformanceAnalyticsSampler(IAnalyticsService analytics, MatchAnalyticsTracker match,
            IEntityLocator entities, INotifier<BattleState> battleState, BattleDefinition battle,
            SquadronRegistry squadrons)
        {
            _analytics = analytics;
            _match = match;
            _entities = entities;
            _battleState = battleState;
            _battle = battle;
            _squadrons = squadrons;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _history = new BattleFpsHistory(battle);
#endif
        }

        public void Initialize()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _history.Initialize();
#endif
            _battleState.AddObserver(this);
        }

        public void LateDispose()
        {
            _battleState.RemoveObserver(this);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            BattlePerformanceCapture.StopHitchMonitoring();
#endif
            RecordSample();
        }

        public void UpdateState(BattleState state)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_state == BattleState.Running && state != BattleState.Running)
                BattlePerformanceCapture.StopHitchMonitoring();
#endif
            if (_state != BattleState.Running && state == BattleState.Running) _skipTransitionFrame = true;
            _state = state;
            if (state == BattleState.Ended) RecordSample();
        }

        public void Tick()
        {
            if (_state != BattleState.Running) return;
            if (_skipTransitionFrame)
            {
                _skipTransitionFrame = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                _history.ResetCollectionBaseline();
                BattlePerformanceCapture.StartHitchMonitoring(_match.MatchId);
#endif
                return;
            }

            float seconds = Time.unscaledDeltaTime;
            if (seconds <= 0f) return;
            _squadronLogSeconds += seconds;
            if (_squadronLogSeconds >= SQUADRON_LOG_INTERVAL_SECONDS)
            {
                _squadronLogSeconds %= SQUADRON_LOG_INTERVAL_SECONDS;
                foreach (var player in _battle.Players)
                    Debug.Log($"[Squadrons] match={_match.MatchId} time={Time.time:F1} player={player.Slot.Id} live={_squadrons.GetLivingCount(player.Slot.Id)} craft={_squadrons.GetLivingCraftCount(player.Slot.Id)}");
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!BattlePerformanceCapture.TryObserveHitchFrame()) return;
            _history.ObserveFrame(seconds > _frames.WorstSeconds);
#endif
            _frames.Add(seconds);
            if (_frames.TotalSeconds >= SAMPLE_WINDOW_SECONDS) RecordSample();
        }

        private void RecordSample()
        {
            if (_frames.Count == 0) return;

            var sample = new PerformanceSampleData(_match.MatchId, _frames.AverageFps,
                _frames.GetLowFps(), (int)(Profiler.GetTotalAllocatedMemoryLong() / BYTES_PER_MB),
                _entities.Entities.Count, QualitySettings.GetQualityLevel());
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _history.WriteWindow(sample, _frames);
#endif
            _analytics.RecordPerformanceSample(sample);
            _frames.Reset();
        }
    }
}
