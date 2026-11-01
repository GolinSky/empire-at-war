using System.Collections.Generic;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Ship;

namespace EmpireAtWar.Services.Enemy
{
    public sealed class EnemyShipTaskGroup
    {
        private bool _hasDecision;
        public List<IShipEntity> Ships { get; } = new List<IShipEntity>();
        public EnemyStrategicContextBuilder ContextBuilder { get; }
        public EnemyStrategicDecisionModel DecisionModel { get; }
        public EnemyTaskForceExecutor Executor { get; }
        public bool IsCaptureDetachment { get; }
        public bool IsMainForce { get; set; }
        public EnemyStrategicContext Context { get; private set; }
        public EnemyStrategicDecision Decision { get; private set; }
        public float RecoveryStartedAt { get; private set; } = -1f;
        public float NoViableCaptureSince { get; internal set; } = -1f;

        public EnemyShipTaskGroup(EnemyStrategicContextBuilder contextBuilder,
            EnemyStrategicDecisionModel decisionModel, EnemyTaskForceExecutor executor,
            bool isCaptureDetachment = false)
        {
            ContextBuilder = contextBuilder;
            DecisionModel = decisionModel;
            Executor = executor;
            IsCaptureDetachment = isCaptureDetachment;
        }

        public void Evaluate(ISet<string> reservedObjectives, float now)
        {
            Context = ContextBuilder.Build(Ships, reservedObjectives, preferStrategicObjectives: IsMainForce);
            if (Context.Snapshot.HasCaptureTarget || ContextBuilder.CaptureReservedByOtherGroup)
                NoViableCaptureSince = -1f;
            else if (NoViableCaptureSince < 0f) NoViableCaptureSince = now;
            EnemyStrategicDecision next = DecisionModel.Evaluate(Context.Snapshot, IsCaptureDetachment);
            if (IsMainForce || next.State == EnemyStrategicState.DefendBase)
                next = new EnemyStrategicDecision(next.Reason, next.State, Ships.Count);
            if (!Executor.CanAssemble(next, Context))
                next = DecisionModel.Evaluate(Context.Snapshot, IsCaptureDetachment, allowOffense: false);
            if (IsMainForce || next.State == EnemyStrategicState.DefendBase)
                next = new EnemyStrategicDecision(next.Reason, next.State, Ships.Count);
            bool execute = !_hasDecision || Decision.State != next.State ||
                next.State != EnemyStrategicState.Hold && next.State != EnemyStrategicState.RebuildFleet;
            Decision = next;
            _hasDecision = true;
            if (DecisionModel.IsRecovering && RecoveryStartedAt < 0f) RecoveryStartedAt = now;
            if (!DecisionModel.IsRecovering) RecoveryStartedAt = -1f;
            if (IsMainForce && Decision.State == EnemyStrategicState.Hold && !Context.Snapshot.IsRegrouped)
                Executor.Rally(Context);
            else if (execute) Executor.Execute(Decision, Context);
            EnemyCaptureObjective executedObjective = Decision.State == EnemyStrategicState.CaptureZone &&
                Executor.BodyCount > 0 ? Context.CaptureObjective : null;
            ContextBuilder.ReportCaptureExecution(executedObjective, now);
            if (executedObjective != null) reservedObjectives.Add(executedObjective.Id);
        }
    }
}
