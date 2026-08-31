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
        private readonly List<FormationPoint> _formationPositions =
            new List<FormationPoint>();
        private readonly List<float> _formationRadii = new List<float>();
        private readonly List<FormationPoint> _formationDestinations =
            new List<FormationPoint>();

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

        private void AssignFormationMove(
            IReadOnlyList<IShipEntity> ships,
            int committedShipCount,
            Vector3 target)
        {
            int count = Math.Min(committedShipCount, ships.Count);
            FormationPoint targetCenter = new FormationPoint(target.x, target.z);
            BuildFormationInputs(ships, count);
            FormationModel.CalculateCompactDestinations(
                _formationPositions,
                _formationRadii,
                targetCenter,
                _formationDestinations);
            for (int i = 0; i < ships.Count; i++)
            {
                if (i >= count)
                {
                    ships[i].HoldPosition();
                    continue;
                }

                FormationPoint destination = _formationDestinations[i];
                ships[i].AssignMoveTarget(new Vector3(destination.X, 0f, destination.Z));
            }
        }

        private void AssignAttack(
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
            BuildFormationInputs(ships, count);

            Vector3 targetPosition = target.HealthModel.Transform.position;
            FormationPoint targetCenter = new FormationPoint(
                targetPosition.x,
                targetPosition.z);
            FormationModel.CalculateCompactDestinations(
                _formationPositions,
                _formationRadii,
                targetCenter,
                _formationDestinations);
            for (int i = 0; i < ships.Count; i++)
            {
                if (i < count)
                {
                    FormationPoint destination = _formationDestinations[i];
                    ships[i].AssignAttackTarget(
                        target,
                        new Vector3(
                            destination.X - targetCenter.X,
                            0f,
                            destination.Z - targetCenter.Z));
                }
                else
                {
                    ships[i].HoldPosition();
                }
            }
        }

        private void BuildFormationInputs(
            IReadOnlyList<IShipEntity> ships,
            int count)
        {
            _formationPositions.Clear();
            _formationRadii.Clear();
            for (int i = 0; i < count; i++)
            {
                _formationPositions.Add(new FormationPoint(
                    ships[i].WorldPosition.x,
                    ships[i].WorldPosition.z));
                _formationRadii.Add(ships[i].NavigationRadius);
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
