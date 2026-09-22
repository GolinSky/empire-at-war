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
        private readonly List<IShipEntity> _captureShips = new List<IShipEntity>();
        private readonly Dictionary<IShipEntity, FormationPoint> _battleOffsets =
            new Dictionary<IShipEntity, FormationPoint>();
        private readonly List<IShipEntity> _battleShips = new List<IShipEntity>();
        private GameEntity _battleTarget;

        public void Execute(EnemyStrategicDecision decision, EnemyStrategicContext context)
        {
            switch (decision.State)
            {
                case EnemyStrategicState.CaptureZone:
                    _captureShips.Clear();
                    _captureShips.AddRange(context.Ships);
                    _captureShips.Sort((first, second) =>
                        second.NavigationSpeed.CompareTo(first.NavigationSpeed));
                    AssignFormationMove(
                        _captureShips,
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
                        context.OwnBase.HealthModel.Transform.position,
                        context.OwnBase);
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
            Vector3 target,
            GameEntity battleTarget = null)
        {
            int count = Math.Min(committedShipCount, ships.Count);
            FormationPoint targetCenter = new FormationPoint(target.x, target.z);
            BuildFormationInputs(ships, count);
            if (battleTarget != null)
            {
                CalculateBattleDestinations(ships, count, battleTarget, targetCenter);
            }
            else
            {
                FormationModel.CalculateCompactDestinations(
                    _formationPositions, _formationRadii, targetCenter, _formationDestinations);
            }
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
            CalculateBattleDestinations(ships, count, target, targetCenter);
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

        private void CalculateBattleDestinations(
            IReadOnlyList<IShipEntity> ships,
            int count,
            GameEntity target,
            FormationPoint center)
        {
            HashSet<IShipEntity> committed = new HashSet<IShipEntity>();
            bool rebuild = _battleTarget != target;
            for (int i = 0; i < count; i++)
            {
                committed.Add(ships[i]);
                rebuild |= !_battleOffsets.ContainsKey(ships[i]);
            }

            if (rebuild)
            {
                _battleOffsets.Clear();
                BattleFormationModel.CalculateDestinations(
                    _formationPositions, _formationRadii, default, _formationDestinations);
                for (int i = 0; i < count; i++)
                {
                    _battleOffsets.Add(ships[i], _formationDestinations[i]);
                }
            }
            else
            {
                foreach (IShipEntity ship in _battleShips)
                {
                    if (!committed.Contains(ship))
                    {
                        _battleOffsets.Remove(ship);
                    }
                }
            }

            _battleTarget = target;
            _battleShips.Clear();
            _formationDestinations.Clear();
            for (int i = 0; i < count; i++)
            {
                _battleShips.Add(ships[i]);
                FormationPoint offset = _battleOffsets[ships[i]];
                _formationDestinations.Add(new FormationPoint(center.X + offset.X, center.Z + offset.Z));
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
