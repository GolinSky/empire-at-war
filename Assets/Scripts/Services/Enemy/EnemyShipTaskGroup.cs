using System.Collections.Generic;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Ship;
using EmpireAtWar.Services.TacticalMarkers;

namespace EmpireAtWar.Services.Enemy
{
    public sealed class EnemyShipTaskGroup
    {
        private static int _nextGroupId;
        private readonly int _groupId = ++_nextGroupId;
        private bool _hasDecision;
        private readonly bool _unbuiltMinesOnly;
        public List<IShipEntity> Ships { get; } = new List<IShipEntity>();
        public EnemyStrategicContextBuilder ContextBuilder { get; }
        public EnemyStrategicDecisionModel DecisionModel { get; }
        public EnemyTaskForceExecutor Executor { get; }
        public bool IsCaptureDetachment { get; }
        public bool IsMainForce { get; set; }
        public EnemyStrategicContext Context { get; private set; }
        public EnemyStrategicDecision Decision { get; private set; }
        public bool HasMarkerRequest { get; private set; }
        public float RecoveryStartedAt { get; private set; } = -1f;
        public float NoViableCaptureSince { get; internal set; } = -1f;

        public EnemyShipTaskGroup(EnemyStrategicContextBuilder contextBuilder,
            EnemyStrategicDecisionModel decisionModel, EnemyTaskForceExecutor executor,
            bool isCaptureDetachment = false, bool unbuiltMinesOnly = false)
        {
            ContextBuilder = contextBuilder;
            DecisionModel = decisionModel;
            Executor = executor;
            IsCaptureDetachment = isCaptureDetachment;
            _unbuiltMinesOnly = unbuiltMinesOnly;
        }

        public void Evaluate(ISet<string> reservedObjectives, float now)
        {
            Context = ContextBuilder.Build(Ships, reservedObjectives, preferStrategicObjectives: IsMainForce,
                unbuiltMinesOnly: _unbuiltMinesOnly);
            if (Context.Snapshot.HasCaptureTarget)
                NoViableCaptureSince = -1f;
            else if (NoViableCaptureSince < 0f) NoViableCaptureSince = now;
            EnemyStrategicDecision next = DecisionModel.Evaluate(Context.Snapshot, IsCaptureDetachment, now: now, commit: false);
            if (IsMainForce && next.State == EnemyStrategicState.CaptureZone)
                next = new EnemyStrategicDecision(next.Reason, next.State, Ships.Count);
            bool outnumbered = Context.Snapshot.EnemyShipCount >= 3 &&
                               Context.Snapshot.EnemyShipCount >= 2 * Ships.Count;
            bool canAssemble = !outnumbered && Executor.CanAssemble(next, Context);
            next = DecisionModel.Evaluate(Context.Snapshot, IsCaptureDetachment, allowOffense: canAssemble, now: now);
            if (IsMainForce && next.State == EnemyStrategicState.CaptureZone)
                next = new EnemyStrategicDecision(next.Reason, next.State, Ships.Count);
            TacticalMarker request = ContextBuilder.AlliedMarkers == null ? null :
                ContextBuilder.AlliedMarkers.SelectRequest(Context, Context.Snapshot.RecoveryMode);
            if (request != null && request.Type != TacticalMarkerType.Retreat &&
                !ContextBuilder.IsMarkerApproachSafe(request.Point)) request = null;
            if (request == null && IsMainForce)
                request = ContextBuilder.SelectAllyDefenseRequest(Context.Snapshot.RecoveryMode);
            if (request != null)
            {
                next = new EnemyStrategicDecision($"Accepted allied {request.Type} request.",
                    request.Type == TacticalMarkerType.Retreat ? EnemyStrategicState.RetreatValue :
                    EnemyStrategicState.Hold, Ships.Count);
            }
            bool execute = HasMarkerRequest || !_hasDecision || Decision.State != next.State ||
                next.State != EnemyStrategicState.Hold && next.State != EnemyStrategicState.RebuildFleet;
            HasMarkerRequest = request != null;
            Context.StateHeldSeconds = DecisionModel.StateHeldSeconds;
            string group = _groupId.ToString();
            if (!_hasDecision || Decision.State != next.State)
                ContextBuilder.RecordStabilityEvent("stateSwitch", System.FormattableString.Invariant(
                    $"group={group}; from={(_hasDecision ? Decision.State.ToString() : "none")}; to={next.State}; localRaw={Context.Snapshot.LocalRaw:F3}; localSmoothed={Context.Snapshot.LocalAdvantage:F3}; heldSeconds={DecisionModel.StateHeldSeconds:F1}; bypass={DecisionModel.Bypass}; bestScore={DecisionModel.BestScore:F3}; runnerUp={DecisionModel.RunnerUpScore:F3}"));
            else if (DecisionModel.SwitchHeld)
                ContextBuilder.RecordStabilityEvent("stateSwitchHeld", System.FormattableString.Invariant(
                    $"group={group}; current={Decision.State}; blocked={DecisionModel.BlockedState}; scoreGap={DecisionModel.BestScore - DecisionModel.RunnerUpScore:F3}; heldSeconds={DecisionModel.StateHeldSeconds:F1}"));
            Decision = next;
            _hasDecision = true;
            if (Context.Snapshot.RecoveryMode && RecoveryStartedAt < 0f) RecoveryStartedAt = now;
            if (!Context.Snapshot.RecoveryMode) RecoveryStartedAt = -1f;
            if (request != null)
                Executor.ExecuteMarker(request,
                    ContextBuilder.AlliedMarkers != null &&
                    ContextBuilder.AlliedMarkers.TryGetAttackTarget(request, out var attackTarget) ? attackTarget : null,
                    Context);
            else if (IsMainForce && Decision.State == EnemyStrategicState.Hold && !Context.Snapshot.IsRegrouped)
                Executor.Rally(Context);
            else if (execute) Executor.Execute(Decision, Context, now);
            EnemyCaptureObjective executedObjective = Decision.State == EnemyStrategicState.CaptureZone &&
                Executor.BodyCount > 0 ? Context.CaptureObjective : null;
            ContextBuilder.ReportCaptureExecution(executedObjective, now);
            if (executedObjective != null) reservedObjectives.Add(executedObjective.Id);
        }
    }
}
