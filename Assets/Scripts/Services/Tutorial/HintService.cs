using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Lifecycle;
using UnityEngine;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>
    /// Runs one lesson at a time, one step at a time. Onboarding lessons run in track order once their trigger
    /// happened; event lessons wait for theirs. A step whose prerequisite is missing shows its small fix step first.
    /// Pacing runs on real time, so the pause lesson can go on while the battle is paused.
    /// </summary>
    public sealed class HintService : IHintService, ITickable, ILateDisposable
    {
        private const int MAX_FIX_DEPTH = 4;
        private const string ABILITY_TOKEN = "ability";
        private static readonly Regex TOKEN = new Regex(@"\[(\w+)\]");

        private readonly HintTrack _track;
        private readonly HintConditionFactory _conditions;
        private readonly HintStates _states;
        private readonly HintTargets _targets;
        private readonly IHintProgressRepository _repository;
        private readonly IHintVisibility _visibility;
        private readonly SkirmishStateModel _battle;
        private readonly LocalUnitDamageTracker _damage;
        private readonly HintProgress _progress;
        private readonly List<LessonRun> _onboarding = new List<LessonRun>();
        private readonly List<LessonRun> _events = new List<LessonRun>();
        private readonly List<LessonRun> _parked = new List<LessonRun>();
        private LessonRun _lesson;
        private HintDefinition _shown;
        private bool _shownIsFix;
        private float _now;
        private float _gapEnd;
        private float _lastDamage = float.NegativeInfinity;
        private float _combatHoldStart = -1f;
        private float _shownAt;

        public event Action<HintDefinition> Shown;
        public event Action<HintDefinition, bool> Closed;

        public HintDefinition Current => _shown;

        public string CurrentProgress =>
            _lesson == null || _shownIsFix || _lesson.Lesson.Steps.Count < 2
                ? string.Empty
                : $"{_lesson.StepNumber}/{_lesson.Lesson.Steps.Count}";

        public HintService(HintTrack track, HintConditionFactory conditions, HintStates states, HintTargets targets,
            IHintProgressRepository repository, IHintVisibility visibility, SkirmishStateModel battle,
            LocalUnitDamageTracker damage)
        {
            _track = track;
            _conditions = conditions;
            _states = states;
            _targets = targets;
            _repository = repository;
            _visibility = visibility;
            _battle = battle;
            _damage = damage;
            _progress = _repository.Load();
            BuildQueues();
            _damage.LocalUnitDamaged += HandleLocalUnitDamaged;
        }

        /// <summary>Forgets every seen lesson and starts onboarding again, in this match.</summary>
        public void Restart()
        {
            HideShown();
            if (_lesson != null) _lesson.Dispose();
            _lesson = null;
            DisposeQueues();
            _repository.Reset();
            BuildQueues();
            _gapEnd = _now;
            _combatHoldStart = -1f;
        }

        private void BuildQueues()
        {
            foreach (HintLesson lesson in _track.Onboarding)
                if (!_progress.IsDone(lesson.Id)) _onboarding.Add(CreateRun(lesson));
            foreach (HintLesson lesson in _track.Events)
                if (!_progress.IsDone(lesson.Id)) _events.Add(CreateRun(lesson));
        }

        private LessonRun CreateRun(HintLesson lesson)
        {
            LessonRun run = new LessonRun(lesson, _conditions.CreateTrigger(lesson.Trigger));
            foreach (HintDefinition step in lesson.Steps)
                if (!_progress.IsDone(step.Id))
                    run.Steps.Add(new StepRun(step,
                        step.ListenFromShown ? null : _conditions.CreateCompletion(step)));
            return run;
        }

        private void DisposeQueues()
        {
            foreach (LessonRun run in _onboarding) run.Dispose();
            foreach (LessonRun run in _events) run.Dispose();
            foreach (LessonRun run in _parked) run.Dispose();
            _onboarding.Clear();
            _events.Clear();
            _parked.Clear();
        }

        public void Tick()
        {
            if (_battle.State != BattleState.Running && _battle.State != BattleState.Paused) return;
            float deltaTime = Time.unscaledDeltaTime;
            _now += deltaTime;
            PollConditions(deltaTime);
            if (!_visibility.IsOn)
            {
                HideShown();
                return;
            }
            if (_lesson == null) StartNextLesson();
            if (_lesson != null) RunLesson();
        }

        private void StartNextLesson()
        {
            if (_now < _gapEnd) return;
            LessonRun next = PickLesson();
            if (next == null) return;
            if (!_parked.Remove(next) && !_onboarding.Remove(next)) _events.Remove(next);
            next.ParkedOn = null;
            _lesson = next;
        }

        // A parked lesson whose wait is over goes first; then onboarding; only urgent event lessons jump ahead of it.
        private LessonRun PickLesson()
        {
            foreach (LessonRun run in _parked)
                if (CanResume(run))
                    return run;
            LessonRun onboarding = null;
            foreach (LessonRun run in _onboarding)
                if (run.Trigger.IsMet)
                {
                    onboarding = run;
                    break;
                }
            LessonRun urgent = null;
            foreach (LessonRun run in _events)
                if (run.Trigger.IsMet && (onboarding == null || run.Lesson.InterruptsOnboarding) &&
                    (urgent == null || run.Lesson.Priority > urgent.Lesson.Priority))
                    urgent = run;
            return urgent ?? onboarding;
        }

        private void RunLesson()
        {
            StepRun step = _lesson.Head;
            if (_shown != null)
            {
                if (_shownIsFix)
                {
                    // A fix step closes once the state it fixes holds, or swaps to a deeper fix.
                    bool isStepDone = step.Condition != null && step.Condition.IsMet;
                    HintDefinition wanted = Resolve(step.Definition, 0);
                    if (wanted == _shown && !isStepDone) return;
                    CloseShown(isStepDone || wanted == step.Definition);
                }
                else
                {
                    float autoClose = _shown.AutoCloseSeconds;
                    if (step.Condition.IsMet || autoClose > 0f && _now - _shownAt >= autoClose)
                    {
                        CompleteHead();
                        CloseShown(true);
                    }
                    else if (Resolve(step.Definition, 0) != step.Definition)
                        HideShown(); // Prerequisite lost (deselected): show the fix step again.
                    else return;
                }
                _gapEnd = _now + _track.StepGapSeconds;
            }
            if (_now < _gapEnd) return;
            SkipFinishedSteps();
            step = _lesson.Head;
            if (step == null)
            {
                FinishLesson();
                return;
            }
            HintDefinition show = Resolve(step.Definition, 0);
            // A step that listens from shown starts listening only once it is the one showing: while its fix step
            // (or silent wait) runs, an earlier action — the upgrade already building — must not count.
            if (step.Condition == null && show == step.Definition)
                step.Condition = _conditions.CreateCompletion(step.Definition);
            if (show.WaitSilently)
            {
                Park(show);
                return;
            }
            if (!IsQuiet() && !show.ShowDuringCombat) return;
            Show(show, show != step.Definition);
        }

        // A capture or construction takes a while: other lessons run meanwhile and this one comes back when it is done.
        private void Park(HintDefinition waitingOn)
        {
            _lesson.ParkedOn = waitingOn;
            _parked.Add(_lesson);
            _lesson = null;
        }

        // The parked step is done, or the step to show changed (its prerequisite now holds, or was lost).
        private bool CanResume(LessonRun run)
        {
            StepRun head = run.Head;
            return head == null || head.Condition != null && head.Condition.IsMet ||
                   Resolve(head.Definition, 0) != run.ParkedOn;
        }

        // The player already did it, or the step does not apply to this unit: record it and move on.
        private void SkipFinishedSteps()
        {
            while (_lesson.Head != null)
            {
                StepRun head = _lesson.Head;
                bool isDone = head.Condition != null && head.Condition.IsMet;
                if (!isDone && (!CanJudgeSkip(head.Definition) || _states.IsSatisfied(head.Definition.SkipUnless)))
                    return;
                CompleteHead();
            }
        }

        // Skip rules read the selection, so they apply only once the step's prerequisites hold — apart from the
        // step's own first click (Click [ability]), which an instant ability must never be told to make.
        private bool CanJudgeSkip(HintDefinition step)
        {
            foreach (HintRequirement requirement in step.Requires)
                if (!HintStates.IsEnteredByStep(requirement.State) && !_states.IsSatisfied(requirement.State))
                    return false;
            return true;
        }

        /// <summary>The step to show for <paramref name="step"/>: itself, or the fix step of its first missing state.</summary>
        private HintDefinition Resolve(HintDefinition step, int depth)
        {
            if (depth >= MAX_FIX_DEPTH) throw new InvalidOperationException($"Hint fix steps loop at '{step.Id}'.");
            foreach (HintRequirement requirement in step.Requires)
                if (!_states.IsSatisfied(requirement.State)) return Resolve(requirement.Step, depth + 1);
            return step;
        }

        private void CompleteHead()
        {
            StepRun head = _lesson.Head;
            _lesson.Steps.RemoveAt(0);
            head.Condition?.Dispose();
            Record(head.Definition.Id);
        }

        private void FinishLesson()
        {
            Record(_lesson.Lesson.Id);
            // A lesson the player had already done shows nothing and costs no pause.
            if (_lesson.HasShown) _gapEnd = _now + _track.MinimumGapSeconds;
            _lesson.Dispose();
            _lesson = null;
        }

        // A long fight must not stall the tutorial: after the hold limit the waiting step shows anyway.
        private bool IsQuiet()
        {
            if (_now - _lastDamage >= _track.CombatQuietSeconds)
            {
                _combatHoldStart = -1f;
                return true;
            }
            if (_combatHoldStart < 0f) _combatHoldStart = _now;
            return _now - _combatHoldStart >= _track.MaxCombatHoldSeconds;
        }

        public bool TryGetWorldTarget(out Vector3 position)
        {
            position = default;
            return _shown != null && !_shown.HideTargetMarker &&
                   _targets.TryGet(_shown.Target, _lesson.Trigger, out position);
        }

        public string ResolveText(string text)
        {
            if (_lesson == null) return text;
            return TOKEN.Replace(text, match => TryGetToken(match.Groups[1].Value, out string value) ? value : match.Value);
        }

        private bool TryGetToken(string token, out string value)
        {
            if (token == ABILITY_TOKEN) return _states.TryGetSelectedAbilityName(out value);
            value = null;
            // The visible step's own condition (a [count]) first, then the lesson trigger (a [unit]).
            if (_lesson.Head?.Condition is IHintTextSource step && step.TryGetText(token, out value)) return true;
            return _lesson.Trigger is IHintTextSource source && source.TryGetText(token, out value);
        }

        /// <summary>Skips the whole lesson and never shows it again.</summary>
        public void Dismiss()
        {
            if (_shown == null) throw new InvalidOperationException("No hint is visible to dismiss.");
            HintDefinition dismissed = _shown;
            _shown = null;
            while (_lesson.Head != null) CompleteHead();
            FinishLesson();
            Closed?.Invoke(dismissed, false);
        }

        // Conditions keep listening while hints are hidden or turned off, so finished steps are never shown later.
        private void PollConditions(float deltaTime)
        {
            if (_lesson != null) _lesson.Poll(deltaTime);
            foreach (LessonRun run in _onboarding) run.Poll(deltaTime);
            foreach (LessonRun run in _events) run.Poll(deltaTime);
            foreach (LessonRun run in _parked) run.Poll(deltaTime);
        }

        private void Show(HintDefinition step, bool isFix)
        {
            _shown = step;
            _shownIsFix = isFix;
            _lesson.HasShown = true;
            _shownAt = _now;
            _combatHoldStart = -1f;
            Shown?.Invoke(step);
        }

        private void CloseShown(bool completed)
        {
            HintDefinition closed = _shown;
            _shown = null;
            Closed?.Invoke(closed, completed);
        }

        // Hints were turned off or the step must wait: the card closes, the lesson keeps its place.
        private void HideShown()
        {
            if (_shown != null) CloseShown(false);
        }

        private void Record(string id)
        {
            if (!_progress.MarkDone(id)) return;
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
            if (_lesson != null) _lesson.Dispose();
            DisposeQueues();
        }

        private sealed class LessonRun
        {
            public HintLesson Lesson { get; }

            /// <summary>Kept after it fires: it may name a unit (text) or mark a place (world target).</summary>
            public IHintCondition Trigger { get; }

            /// <summary>Steps not done yet, in order.</summary>
            public List<StepRun> Steps { get; } = new List<StepRun>();

            /// <summary>1-based number of the head step within the whole lesson, for the card's progress.</summary>
            public int StepNumber => Lesson.Steps.Count - Steps.Count + 1;

            public bool HasShown { get; set; }

            /// <summary>The waiting step the lesson stepped aside on; null while the lesson is not parked.</summary>
            public HintDefinition ParkedOn { get; set; }

            public StepRun Head => Steps.Count > 0 ? Steps[0] : null;

            public LessonRun(HintLesson lesson, IHintCondition trigger)
            {
                Lesson = lesson;
                Trigger = trigger;
            }

            public void Poll(float deltaTime)
            {
                PollCondition(Trigger, deltaTime);
                foreach (StepRun step in Steps) PollCondition(step.Condition, deltaTime);
            }

            private static void PollCondition(IHintCondition condition, float deltaTime)
            {
                if (condition is IPolledHintCondition polled) polled.Poll(deltaTime);
            }

            public void Dispose()
            {
                Trigger.Dispose();
                foreach (StepRun step in Steps) step.Condition?.Dispose();
                Steps.Clear();
            }
        }

        private sealed class StepRun
        {
            public HintDefinition Definition { get; }

            /// <summary>Null until the step shows, for steps that listen from shown.</summary>
            public IHintCondition Condition { get; set; }

            public StepRun(HintDefinition definition, IHintCondition condition)
            {
                Definition = definition;
                Condition = condition;
            }
        }
    }
}
