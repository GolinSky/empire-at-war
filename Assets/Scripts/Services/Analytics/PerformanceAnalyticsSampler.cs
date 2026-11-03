using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Lifecycle;
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
        private const long BYTES_PER_MB = 1024 * 1024;

        private readonly IAnalyticsService _analytics;
        private readonly MatchAnalyticsTracker _match;
        private readonly IEntityLocator _entities;
        private readonly INotifier<BattleState> _battleState;
        private readonly FrameTimeHistogram _frames = new FrameTimeHistogram();

        private BattleState _state;

        public PerformanceAnalyticsSampler(IAnalyticsService analytics, MatchAnalyticsTracker match,
            IEntityLocator entities, INotifier<BattleState> battleState)
        {
            _analytics = analytics;
            _match = match;
            _entities = entities;
            _battleState = battleState;
        }

        public void Initialize()
        {
            _battleState.AddObserver(this);
        }

        public void LateDispose()
        {
            _battleState.RemoveObserver(this);
            RecordSample();
        }

        public void UpdateState(BattleState state)
        {
            _state = state;
            if (state == BattleState.Ended) RecordSample();
        }

        public void Tick()
        {
            if (_state != BattleState.Running) return;

            _frames.Add(Time.unscaledDeltaTime);
            if (_frames.TotalSeconds >= SAMPLE_WINDOW_SECONDS) RecordSample();
        }

        private void RecordSample()
        {
            if (_frames.Count == 0) return;

            _analytics.RecordPerformanceSample(new PerformanceSampleData(_match.MatchId, _frames.AverageFps,
                _frames.GetLowFps(), (int)(Profiler.GetTotalAllocatedMemoryLong() / BYTES_PER_MB),
                _entities.Entities.Count, QualitySettings.GetQualityLevel()));
            _frames.Reset();
        }
    }
}
