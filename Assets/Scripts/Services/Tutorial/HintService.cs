using System;
using System.Collections.Generic;
using System.IO;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Settings;
using UnityEngine;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>
    /// Decides which hint is visible. Basics run in order; event hints wait for their trigger. One hint at a time,
    /// a minimum gap between hints, and only Show During Combat hints while a local unit was hit recently.
    /// </summary>
    public sealed class HintService : IHintService, ITickable, ILateDisposable
    {
        private readonly HintTrack _track;
        private readonly HintConditionFactory _conditions;
        private readonly IHintProgressRepository _repository;
        private readonly ISettingsService _settings;
        private readonly SkirmishStateModel _battle;
        private readonly LocalUnitDamageTracker _damage;
        private readonly HintProgress _progress;
        private readonly List<PendingHint> _waiting = new List<PendingHint>();
        private readonly List<PendingHint> _ready = new List<PendingHint>();
        private readonly List<PendingHint> _basics = new List<PendingHint>();
        private PendingHint _current;
        private bool _currentIsBasic;
        private float _now;
        private float _gapEnd;
        private float _lastDamage = float.NegativeInfinity;

        public event Action<HintDefinition> Shown;
        public event Action<HintDefinition, bool> Closed;

        public HintDefinition Current => _current?.Definition;
        public int CurrentBasicNumber => _current != null && _currentIsBasic ? _current.BasicNumber : 0;
        public int BasicCount => _track.Basics.Count;

        public HintService(HintTrack track, HintConditionFactory conditions, IHintProgressRepository repository,
            ISettingsService settings, SkirmishStateModel battle, LocalUnitDamageTracker damage)
        {
            _track = track;
            _conditions = conditions;
            _repository = repository;
            _settings = settings;
            _battle = battle;
            _damage = damage;
            _progress = _repository.Load();
            foreach (HintDefinition hint in _track.Events)
                if (!_progress.IsDone(hint.Id))
                    _waiting.Add(new PendingHint(hint, _conditions.CreateTrigger(hint.Trigger), 0));
            for (int i = 0; i < _track.Basics.Count; i++)
                if (!_progress.IsDone(_track.Basics[i].Id))
                    _basics.Add(new PendingHint(_track.Basics[i], _conditions.CreateCompletion(_track.Basics[i].Completion), i + 1));
            _damage.LocalUnitDamaged += HandleLocalUnitDamaged;
        }

        public void Tick()
        {
            if (_battle.State != BattleState.Running) return;
            float deltaTime = Time.deltaTime;
            _now += deltaTime;
            PollConditions(deltaTime);
            if (!_settings.Saved.Gameplay.ShowHints)
            {
                if (_current != null) Hide();
                return;
            }
            if (_current != null)
            {
                if (_current.Condition.IsMet) Close(true);
                return;
            }
            PromoteTriggered();
            DropCompletedBeforeShown();
            if (_now < _gapEnd) return;
            bool isQuiet = _now - _lastDamage >= _track.CombatQuietSeconds;
            PendingHint next = PickReady(isQuiet);
            if (next != null) Show(next, false);
            else if (isQuiet && _basics.Count > 0) Show(_basics[0], true);
        }

        public bool TryGetWorldTarget(out Vector3 position)
        {
            position = _current != null ? _current.WorldTarget : default;
            return _current != null && _current.HasWorldTarget;
        }

        public void Dismiss()
        {
            if (_current == null) throw new InvalidOperationException("No hint is visible to dismiss.");
            Close(false);
        }

        // Conditions keep listening while hints are hidden or turned off, so finished lessons are never shown later.
        private void PollConditions(float deltaTime)
        {
            Poll(_current, deltaTime);
            foreach (PendingHint hint in _basics) Poll(hint, deltaTime);
            foreach (PendingHint hint in _waiting) Poll(hint, deltaTime);
            foreach (PendingHint hint in _ready) Poll(hint, deltaTime);
        }

        private static void Poll(PendingHint hint, float deltaTime)
        {
            if (hint != null && hint.Condition is IPolledHintCondition polled) polled.Poll(deltaTime);
        }

        private void PromoteTriggered()
        {
            for (int i = _waiting.Count - 1; i >= 0; i--)
            {
                PendingHint waiting = _waiting[i];
                if (!waiting.Condition.IsMet) continue;
                PendingHint ready = new PendingHint(waiting.Definition,
                    _conditions.CreateCompletion(waiting.Definition.Completion), 0);
                if (waiting.Condition is IHintWorldTarget target && target.TryGetWorldTarget(out Vector3 position))
                    ready.SetWorldTarget(position);
                waiting.Condition.Dispose();
                _waiting.RemoveAt(i);
                _ready.Add(ready);
            }
        }

        // The player already did what the hint teaches: record it and never show it.
        private void DropCompletedBeforeShown()
        {
            for (int i = _basics.Count - 1; i >= 0; i--)
            {
                if (!_basics[i].Condition.IsMet) continue;
                Retire(_basics[i]);
                _basics.RemoveAt(i);
            }
            for (int i = _ready.Count - 1; i >= 0; i--)
            {
                if (!_ready[i].Condition.IsMet) continue;
                Retire(_ready[i]);
                _ready.RemoveAt(i);
            }
        }

        private PendingHint PickReady(bool isQuiet)
        {
            PendingHint best = null;
            foreach (PendingHint hint in _ready)
                if ((isQuiet || hint.Definition.ShowDuringCombat) &&
                    (best == null || hint.Definition.Priority > best.Definition.Priority))
                    best = hint;
            return best;
        }

        private void Show(PendingHint hint, bool isBasic)
        {
            if (isBasic) _basics.Remove(hint);
            else _ready.Remove(hint);
            _current = hint;
            _currentIsBasic = isBasic;
            Shown?.Invoke(hint.Definition);
        }

        private void Close(bool completed)
        {
            PendingHint closed = _current;
            _current = null;
            Retire(closed);
            _gapEnd = _now + _track.MinimumGapSeconds;
            Closed?.Invoke(closed.Definition, completed);
        }

        // Hints were turned off: the visible hint goes back to its queue unrecorded.
        private void Hide()
        {
            PendingHint hidden = _current;
            _current = null;
            if (_currentIsBasic) _basics.Insert(0, hidden);
            else _ready.Add(hidden);
            Closed?.Invoke(hidden.Definition, false);
        }


        private void Retire(PendingHint hint)
        {
            hint.Condition.Dispose();
            if (!_progress.MarkDone(hint.Definition.Id)) return;
            try
            {
                _repository.Save(_progress);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning($"Could not save hint progress: {exception.Message}");
            }
        }

        private void HandleLocalUnitDamaged() => _lastDamage = _now;

        public void LateDispose()
        {
            _damage.LocalUnitDamaged -= HandleLocalUnitDamaged;
            _current?.Condition.Dispose();
            foreach (PendingHint hint in _basics) hint.Condition.Dispose();
            foreach (PendingHint hint in _waiting) hint.Condition.Dispose();
            foreach (PendingHint hint in _ready) hint.Condition.Dispose();
            _basics.Clear();
            _waiting.Clear();
            _ready.Clear();
        }

        private sealed class PendingHint
        {
            public HintDefinition Definition { get; }
            public IHintCondition Condition { get; }

            /// <summary>1-based position in the basics track; 0 for event hints.</summary>
            public int BasicNumber { get; }

            public bool HasWorldTarget { get; private set; }
            public Vector3 WorldTarget { get; private set; }

            public PendingHint(HintDefinition definition, IHintCondition condition, int basicNumber)
            {
                Definition = definition;
                Condition = condition;
                BasicNumber = basicNumber;
            }

            public void SetWorldTarget(Vector3 position)
            {
                WorldTarget = position;
                HasWorldTarget = true;
            }
        }
    }
}
