using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Ship;
using UnityEngine;
using GameEntity = EmpireAtWar.Entities.BaseEntity.IEntity;

namespace EmpireAtWar.Services.Enemy
{
    public sealed class EnemyTaskForceExecutor
    {
        private const float FORMATION_SPACING = 12f;

        public void Execute(EnemyStrategicDecision decision, EnemyStrategicContext context)
        {
            switch (decision.State)
            {
                case EnemyStrategicState.CaptureZone:
                    AssignFormationMove(
                        context.Ships,
                        decision.CommittedShipCount,
                        context.CaptureTarget);
                    return;
                case EnemyStrategicState.HuntFleet:
                    AssignAttack(
                        context.Ships,
                        decision.CommittedShipCount,
                        context.EnemyFleetTarget);
                    return;
                case EnemyStrategicState.AssaultBase:
                    AssignAttack(
                        context.Ships,
                        decision.CommittedShipCount,
                        context.EnemyBaseTarget);
                    return;
                case EnemyStrategicState.DefendBase:
                    if (context.OwnBase == null)
                    {
                        HoldAll(context.Ships);
                        return;
                    }

                    AssignFormationMove(
                        context.Ships,
                        decision.CommittedShipCount,
                        context.OwnBase.HealthModel.Transform.position);
                    return;
                case EnemyStrategicState.RebuildFleet:
                case EnemyStrategicState.Hold:
                    HoldAll(context.Ships);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(decision.State));
            }
        }

        private static void AssignFormationMove(
            IReadOnlyList<IShipEntity> ships,
            int committedShipCount,
            Vector3 target)
        {
            int count = Math.Min(committedShipCount, ships.Count);
            FormationPoint targetCenter = new FormationPoint(target.x, target.z);
            for (int i = 0; i < ships.Count; i++)
            {
                if (i >= count)
                {
                    ships[i].HoldPosition();
                    continue;
                }

                FormationPoint destination = FormationModel.CalculateGridDestination(
                    i,
                    count,
                    targetCenter,
                    FORMATION_SPACING);
                ships[i].AssignMoveTarget(new Vector3(destination.X, 0f, destination.Z));
            }
        }

        private static void AssignAttack(
            IReadOnlyList<IShipEntity> ships,
            int committedShipCount,
            GameEntity target)
        {
            if (target == null)
            {
                HoldAll(ships);
                return;
            }

            int count = Math.Min(committedShipCount, ships.Count);
            for (int i = 0; i < ships.Count; i++)
            {
                if (i < count)
                {
                    ships[i].AssignAttackTarget(target);
                }
                else
                {
                    ships[i].HoldPosition();
                }
            }
        }

        private static void HoldAll(IReadOnlyList<IShipEntity> ships)
        {
            foreach (IShipEntity ship in ships)
            {
                ship.HoldPosition();
            }
        }
    }
}
