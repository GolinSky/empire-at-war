using System;
using System.Collections.Generic;
using System.Diagnostics;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using Unity.Profiling;
using UnityEngine;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// Battle-scope AI scheduler that runs due AI work units (one ship task group, one squadron pass,
    /// or one production tick) oldest-first within a per-frame millisecond budget.
    /// </summary>
    public sealed class EnemyAiScheduler : IInitializable, ITickable, ILateDisposable, IObserver<BattleState>
    {
        public const float DEFAULT_FRAME_BUDGET_MS = 2f;
        public const float STARVATION_MULTIPLIER = 2f;

        private static readonly ProfilerMarker SCHEDULER_TICK_MARKER = new ProfilerMarker("EnemyAI.Scheduler.Tick");
        private static readonly double TICKS_TO_MS = 1000.0 / Stopwatch.Frequency;

        public sealed class WorkHandle
        {
            internal readonly PlayerId Owner;
            internal readonly float Interval;
            internal readonly Func<bool> IsCritical;
            internal readonly Func<bool> HasPendingSubWork;
            internal readonly Action RunUnit;

            internal float DueTime;
            internal float LastRunTime;
            internal bool Removed;

            internal WorkHandle(
                PlayerId owner,
                float interval,
                float initialDelay,
                Func<bool> isCritical,
                Func<bool> hasPendingSubWork,
                Action runUnit)
            {
                Owner = owner;
                Interval = interval;
                DueTime = initialDelay;
                LastRunTime = float.NegativeInfinity;
                IsCritical = isCritical;
                HasPendingSubWork = hasPendingSubWork;
                RunUnit = runUnit;
            }
        }

        private readonly INotifier<BattleState> _battleState;
        private readonly List<WorkHandle> _items = new List<WorkHandle>();
        private readonly List<WorkHandle> _due = new List<WorkHandle>();
        private readonly Stopwatch _stopwatch = new Stopwatch();

        private Func<double> _elapsedMs;
        private bool _isRunning;
        private float _battleClock;

        public float FrameBudgetMs { get; set; } = DEFAULT_FRAME_BUDGET_MS;
        public int StarvationRunCount { get; private set; }
        public int ExecutedUnitCount { get; private set; }
        public float BattleClock => _battleClock;
        public bool IsRunning => _isRunning;

        public EnemyAiScheduler(INotifier<BattleState> battleState)
        {
            _battleState = battleState;
        }

        internal void SetElapsedMsProviderForTests(Func<double> elapsedMs)
        {
            _elapsedMs = elapsedMs;
        }

        public void Initialize()
        {
            _battleState.AddObserver(this);
        }

        public void LateDispose()
        {
            _battleState.RemoveObserver(this);
            _isRunning = false;
            _items.Clear();
            _due.Clear();
        }

        public void UpdateState(BattleState state)
        {
            _isRunning = state == BattleState.Running;
        }

        public WorkHandle Register(
            PlayerId owner,
            float interval,
            float initialDelay,
            Action runUnit,
            Func<bool> isCritical = null,
            Func<bool> hasPendingSubWork = null)
        {
            WorkHandle handle = new WorkHandle(
                owner,
                Mathf.Max(0.01f, interval),
                _battleClock + Mathf.Max(0f, initialDelay),
                isCritical,
                hasPendingSubWork,
                runUnit);
            _items.Add(handle);
            return handle;
        }

        public void Unregister(WorkHandle handle)
        {
            if (handle == null)
            {
                throw new ArgumentNullException(nameof(handle));
            }

            handle.Removed = true;
            _items.Remove(handle);
        }

        public void MarkDirty(WorkHandle handle, float minimumGap)
        {
            if (handle == null)
            {
                throw new ArgumentNullException(nameof(handle));
            }

            if (handle.Removed)
            {
                return;
            }

            float earliestAllowed = float.IsNegativeInfinity(handle.LastRunTime)
                ? _battleClock
                : handle.LastRunTime + minimumGap;
            float targetDue = Mathf.Max(_battleClock, earliestAllowed);
            if (targetDue < handle.DueTime)
            {
                handle.DueTime = targetDue;
            }
        }

        public void MarkDue(WorkHandle handle)
        {
            if (handle == null)
            {
                throw new ArgumentNullException(nameof(handle));
            }

            if (handle.Removed)
            {
                return;
            }

            if (_battleClock < handle.DueTime)
            {
                handle.DueTime = _battleClock;
            }
        }

        public float GetRemainingDelay(WorkHandle handle)
        {
            if (handle == null)
            {
                throw new ArgumentNullException(nameof(handle));
            }

            return Mathf.Max(0f, handle.DueTime - _battleClock);
        }

        public void Tick()
        {
            Step(Time.deltaTime);
        }

        public void Step(float deltaTime)
        {
            if (!_isRunning || _items.Count == 0)
            {
                return;
            }

            using var sample = SCHEDULER_TICK_MARKER.Auto();
            _battleClock += deltaTime;

            _due.Clear();
            for (int i = 0; i < _items.Count; i++)
            {
                WorkHandle item = _items[i];
                if (item.Removed)
                {
                    continue;
                }

                if (item.DueTime <= _battleClock || (item.HasPendingSubWork != null && item.HasPendingSubWork()))
                {
                    _due.Add(item);
                }
            }

            if (_due.Count == 0)
            {
                return;
            }

            _due.Sort(ComparePriority);

            double startMs = StartClock();
            for (int i = 0; i < _due.Count; i++)
            {
                WorkHandle item = _due[i];
                if (item.Removed)
                {
                    continue;
                }

                bool starved = IsStarved(item);
                double elapsedMs = GetElapsedMs(startMs);
                if (i > 0 && !starved && elapsedMs >= FrameBudgetMs)
                {
                    break;
                }

                if (starved && i > 0 && elapsedMs >= FrameBudgetMs)
                {
                    StarvationRunCount++;
                }

                item.RunUnit();
                ExecutedUnitCount++;

                bool moreSubWork = item.HasPendingSubWork != null && item.HasPendingSubWork();
                if (!moreSubWork)
                {
                    item.LastRunTime = _battleClock;
                    item.DueTime = _battleClock + item.Interval;
                }
            }
        }

        private bool IsStarved(WorkHandle item)
        {
            return _battleClock - item.DueTime >= item.Interval * STARVATION_MULTIPLIER;
        }

        private int ComparePriority(WorkHandle left, WorkHandle right)
        {
            bool leftStarved = IsStarved(left);
            bool rightStarved = IsStarved(right);
            if (leftStarved != rightStarved)
            {
                return rightStarved.CompareTo(leftStarved);
            }

            bool leftCritical = left.IsCritical != null && left.IsCritical();
            bool rightCritical = right.IsCritical != null && right.IsCritical();
            if (leftCritical != rightCritical)
            {
                return rightCritical.CompareTo(leftCritical);
            }

            return left.DueTime.CompareTo(right.DueTime);
        }

        private double StartClock()
        {
            if (_elapsedMs != null)
            {
                return _elapsedMs();
            }

            _stopwatch.Restart();
            return 0.0;
        }

        private double GetElapsedMs(double startMs)
        {
            if (_elapsedMs != null)
            {
                return _elapsedMs() - startMs;
            }

            return _stopwatch.ElapsedTicks * TICKS_TO_MS;
        }
    }
}
