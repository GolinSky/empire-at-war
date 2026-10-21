using System.Linq;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Extensions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Utils;
using UnityEngine;
using VContainer;

namespace EmpireAtWar.Entities.Ship.EntityFacades
{
    public sealed class ShipSaveFacade : IUnitSaveFacade
    {
        private readonly long _entityId;
        private readonly PlayerId _owner;
        private readonly ShipType _shipType;
        private readonly Transform _viewTransform;
        private readonly ShipMoveModel _move;
        private readonly HealthModel _health;
        private readonly UnitOrderModel _orders;

        public ShipSaveFacade(long entityId, PlayerId owner, ShipType shipType,
            [Key(EntityBindType.ViewTransform)] Transform viewTransform, ShipMoveModel move,
            HealthModel health, UnitOrderModel orders)
        {
            _entityId = entityId;
            _owner = owner;
            _shipType = shipType;
            _viewTransform = viewTransform;
            _move = move;
            _health = health;
            _orders = orders;
        }

        public UnitSave Capture() => UnitSave.Create(_entityId, _owner, UnitTypeId.Ship(_shipType),
            _move.Phase == MovementPhase.Arriving ? _move.HyperSpacePosition.ToUnity() : _viewTransform.position,
            _viewTransform.rotation,
            UnitHealthSave.ForShip(_health.HullPercentage, _health.ShieldPercentage,
                _health.HardPointModels.Select(point => point.HealthPercentage).ToArray()),
            UnitOrderSave.From(_orders));

        public void RestoreHealth(UnitHealthSave save) => _health.Restore(save.Hull, save.Shields, save.HardPoints);
    }
}
