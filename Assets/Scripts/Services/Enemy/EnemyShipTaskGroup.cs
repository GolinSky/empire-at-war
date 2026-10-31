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
        public EnemyStrategicContext Context { get; private set; }
        public EnemyStrategicDecision Decision { get; private set; }
        public float RecoveryStartedAt { get; private set; } = -1f;

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
            Context = ContextBuilder.Build(Ships, reservedObjectives);
            EnemyStrategicDecision next = DecisionModel.Evaluate(Context.Snapshot, IsCaptureDetachment);
            bool execute = !_hasDecision || Decision.State != next.State ||
                next.State != EnemyStrategicState.Hold && next.State != EnemyStrategicState.RebuildFleet;
            Decision = next;
            _hasDecision = true;
            if (DecisionModel.IsRecovering && RecoveryStartedAt < 0f) RecoveryStartedAt = now;
            if (!DecisionModel.IsRecovering) RecoveryStartedAt = -1f;
            if (execute) Executor.Execute(Decision, Context);
            EnemyCaptureObjective executedObjective = Decision.State == EnemyStrategicState.CaptureZone &&
                Executor.BodyCount > 0 ? Context.CaptureObjective : null;
            ContextBuilder.ReportCaptureExecution(executedObjective, now);
            if (executedObjective != null) reservedObjectives.Add(executedObjective.Id);
        }
    }
}
