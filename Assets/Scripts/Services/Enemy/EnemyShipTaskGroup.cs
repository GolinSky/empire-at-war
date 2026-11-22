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
        /// <summary>
        /// The main force answers an allied station's defense request or a hostile armada: the allocator leaves its
        /// ships alone until the answer ends.
        /// </summary>
        public bool IsAnsweringTeam { get; private set; }
        public float RecoveryStartedAt { get; private set; } = -1f;
        public float NoViableCaptureSince { get; internal set; } = -1f;
        /// <summary>A station was lost: only defend or gather until the turtle time ends (no raid, capture or move out).</summary>
        public bool Turtle { get; set; }

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
            ContextBuilder.CommittedBody = Executor.Body;
            ContextBuilder.AssaultUnderWay = _hasDecision && Decision.State == EnemyStrategicState.AssaultBase;
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
            // A raid is rated against the defenders at its target, so the enemy's total ship count does not block it.
            bool canAssemble = (!outnumbered || next.State == EnemyStrategicState.RaidStructures) &&
                Executor.CanAssemble(next, Context);
            next = DecisionModel.Evaluate(Context.Snapshot, IsCaptureDetachment, allowOffense: canAssemble, now: now);
            if (IsMainForce && next.State == EnemyStrategicState.CaptureZone)
                next = new EnemyStrategicDecision(next.Reason, next.State, Ships.Count);
            TacticalMarker request = ContextBuilder.AlliedMarkers == null ? null :
                ContextBuilder.AlliedMarkers.SelectRequest(Context, Context.Snapshot.RecoveryMode);
            if (request != null && request.Type != TacticalMarkerType.Retreat &&
                !ContextBuilder.IsMarkerApproachSafe(request.Point)) request = null;
            string requestReason = null;
            if (request == null && IsMainForce)
            {
                request = ContextBuilder.SelectAllyDefenseRequest();
                if (request != null) requestReason = "Answering an allied station under pressure.";
            }
            if (request == null && IsMainForce)
                request = ContextBuilder.SelectArmadaResponse(next.State, out requestReason);
            if (Turtle && request != null && request.Type != TacticalMarkerType.Retreat) request = null;
            IsAnsweringTeam = request != null && requestReason != null;
            if (Turtle && next.State is not (EnemyStrategicState.DefendBase or EnemyStrategicState.Hold or
                    EnemyStrategicState.RebuildFleet or EnemyStrategicState.RetreatValue))
                next = new EnemyStrategicDecision("Turtle: a station was lost; the fleet gathers at home.",
                    EnemyStrategicState.Hold, Ships.Count);
            if (request != null)
            {
                next = new EnemyStrategicDecision(requestReason ?? $"Accepted allied {request.Type} request.",
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
                    Context, requestReason);
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
