using System.Linq;
using EmpireAtWar.Extensions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Saves;
using UnityEngine;
using VContainer;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public sealed class StructureSaveFacade : IStructureSaveFacade
    {
        private readonly long _entityId;
        private readonly PlayerId _owner;
        private readonly StructureKind _kind;
        private readonly int _type;
        private readonly Transform _viewTransform;
        private readonly HealthModel _health;

        /// <param name="type">The station's faction, or the facility or platform type, as its enum value.</param>
        public StructureSaveFacade(long entityId, PlayerId owner, StructureKind kind, int type,
            [Key(EntityBindType.ViewTransform)] Transform viewTransform, HealthModel health)
        {
            _entityId = entityId;
            _owner = owner;
            _kind = kind;
            _type = type;
            _viewTransform = viewTransform;
            _health = health;
        }

        public StructureSave Capture() => StructureSave.Create(_entityId, _owner, _kind, _type, _viewTransform.position,
            UnitHealthSave.ForShip(_health.HullPercentage, _health.ShieldPercentage,
                _health.HardPointModels.Select(point => point.HealthPercentage).ToArray()));

        public void RestoreHealth(UnitHealthSave save) => _health.Restore(save.Hull, save.Shields, save.HardPoints);
    }
}
