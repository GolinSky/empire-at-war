using System.Collections.Generic;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Utils;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.FleetCommand
{
    public sealed class FleetCommandService : ITickable, ILateDisposable
    {
        // Matches the existing Concentrate Fire command radius.
        private const float COMMAND_RADIUS = 400f;
        private const float UPDATE_INTERVAL = 0.25f;
        private readonly IEntityLocator _entities;
        private readonly IPlayerRoster _players;
        private readonly List<IEntity> _commanders = new List<IEntity>();
        private readonly HashSet<CombatModifiers> _affected = new HashSet<CombatModifiers>();
        private readonly HashSet<CombatModifiers> _next = new HashSet<CombatModifiers>();
        private float _remainingTime;

        public FleetCommandService(IEntityLocator entities, IPlayerRoster players)
        {
            _entities = entities;
            _players = players;
        }

        public void Tick()
        {
            _remainingTime -= Time.deltaTime;
            if (_remainingTime > 0f) return;
            _remainingTime = UPDATE_INTERVAL;
            _commanders.Clear();
            foreach (IEntity entity in _entities.Entities)
            {
                if (!entity.HealthModel.IsDestroyed &&
                    entity.TryGetFacade(out IUnitTypeFacade type) &&
                    (type.UnitTypeId == UnitTypeId.Ship(ShipType.Resolute) ||
                     type.UnitTypeId == UnitTypeId.Ship(ShipType.HomeOne)) &&
                    !entity.GetFacade<ICombatModifiersFacade>().Modifiers.IsIonDisabled)
                    _commanders.Add(entity);
            }

            _next.Clear();
            foreach (IEntity entity in _entities.Entities)
            {
                if (entity.HealthModel.IsDestroyed ||
                    !entity.TryGetFacade(out ICombatModifiersFacade combat) ||
                    !entity.TryGetFacade(out IUnitTypeFacade type) ||
                    !(type.UnitTypeId.IsShip || type.UnitTypeId.IsSquadron) ||
                    (_players.Get(entity.Owner).Faction != FactionType.Republic &&
                     _players.Get(entity.Owner).Faction != FactionType.Rebellion)) continue;
                foreach (IEntity commander in _commanders)
                {
                    // Command ships retain their own configured base stats.
                    if (ReferenceEquals(entity, commander) ||
                        _players.Get(commander.Owner).Faction != _players.Get(entity.Owner).Faction ||
                        !_players.IsAllied(commander.Owner, entity.Owner)) continue;
                    if (commander.GetFacade<IUnitTypeFacade>().UnitTypeId != UnitTypeId.Ship(ShipType.HomeOne) &&
                        PlanarGeometry.DistanceSquared(
                        commander.GetFacade<IEntityTransformFacade>().Transform.position,
                        entity.GetFacade<IEntityTransformFacade>().Transform.position) >
                        COMMAND_RADIUS * COMMAND_RADIUS) continue;
                    _next.Add(combat.Modifiers);
                    break;
                }
            }

            foreach (CombatModifiers modifiers in _affected)
                if (!_next.Contains(modifiers)) modifiers.SetFleetCommand(false);
            foreach (CombatModifiers modifiers in _next)
                if (!_affected.Contains(modifiers)) modifiers.SetFleetCommand(true);
            _affected.Clear();
            _affected.UnionWith(_next);
        }

        public void LateDispose()
        {
            foreach (CombatModifiers modifiers in _affected) modifiers.SetFleetCommand(false);
            _affected.Clear();
            _next.Clear();
        }
    }
}
