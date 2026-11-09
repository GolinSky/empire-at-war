using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>
    /// Met when the local player owns a unit of one kind. Remembers that unit, so the card names it ([unit]) and
    /// the marker follows it while it lives.
    /// </summary>
    public sealed class LocalUnitHintCondition : IHintCondition, IHintWorldTarget, IHintTextSource
    {
        private readonly IEntityLocator _entities;
        private readonly ILocalPlayer _player;
        private readonly FactionCatalog _factions;
        private readonly LocalUnitKind _kind;
        private IEntity _unit;
        private string _unitName;

        public bool IsMet => _unit != null;

        public LocalUnitHintCondition(IEntityLocator entities, ILocalPlayer player, FactionCatalog factions,
            LocalUnitKind kind)
        {
            _entities = entities;
            _player = player;
            _factions = factions;
            _kind = kind;
            _entities.EntityAdded += HandleAdded;
            foreach (IEntity entity in _entities.Entities) HandleAdded(entity);
        }

        private void HandleAdded(IEntity entity)
        {
            if (_unit != null || !_player.IsLocal(entity.Owner) || entity.HealthModel.IsDestroyed ||
                !entity.TryGetFacade(out IUnitTypeFacade unit)) return;
            FactionData data = unit.UnitTypeId.IsShip
                ? _factions.GetShipFactionData(unit.UnitTypeId.ShipType)
                : _factions.GetSquadronFactionData(unit.UnitTypeId.SquadronType);
            if (!Matches(entity, unit.UnitTypeId, data)) return;
            _unit = entity;
            _unitName = data.Name;
        }

        private bool Matches(IEntity entity, UnitTypeId type, FactionData data)
        {
            switch (_kind)
            {
                case LocalUnitKind.Hero: return data.IsHero;
                case LocalUnitKind.Carrier:
                    return type.IsShip && entity.TryGetFacade(out IHangarCommand _) && entity.TryGetFacade(out IMoveFacade _);
                default:
                    return type.IsShip && entity.TryGetFacade(out IShipAbilityFacade abilities) && abilities.Slots.Count > 0;
            }
        }

        public bool TryGetWorldTarget(out Vector3 position)
        {
            position = default;
            if (_unit == null || _unit.HealthModel.IsDestroyed) return false;
            position = _unit.GetFacade<IEntityTransformFacade>().Transform.position;
            return true;
        }

        public bool TryGetText(string token, out string value)
        {
            value = _unitName;
            return token == "unit" && _unit != null;
        }

        public void Dispose() => _entities.EntityAdded -= HandleAdded;
    }
}
