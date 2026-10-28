using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Utils;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class FighterCoordinationAbility : IPhasedShipAbility
    {
        private readonly IEntityLocator _entities;
        private readonly FighterCoordinationSettings _settings;
        private readonly IPlayerRelations _relations;
        private readonly List<CombatModifiers> _affected = new List<CombatModifiers>();
        private IHardPointModel _commandArray;
        private CombatModifiers _casterModifiers;

        public bool IsComplete => _commandArray.IsDestroyed || _casterModifiers.AbilitiesSuppressed;
        public bool SurvivesCasterDeath => false;

        public FighterCoordinationAbility(IEntityLocator entities, FighterCoordinationSettings settings,
            IPlayerRelations relations)
        {
            _entities = entities;
            _settings = settings;
            _relations = relations;
        }

        public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target)
        {
            _commandArray = caster.Health.GetShipUnits(HardPointType.CommandArray)
                .First(point => point.HardPointType == HardPointType.CommandArray && !point.IsDestroyed);
            _casterModifiers = caster.Modifiers;
            foreach (IEntity entity in _entities.Entities)
            {
                if (!_relations.IsAllied(caster.Entity.Owner, entity.Owner) || entity.HealthModel.IsDestroyed ||
                    !entity.TryGetFacade(out IUnitTypeFacade unit) || !unit.UnitTypeId.IsSquadron)
                    continue;

                var transform = entity.GetFacade<IEntityTransformFacade>();
                if (PlanarGeometry.Distance(caster.WorldPosition, transform.Transform.position) > _settings.CommandRadius)
                    continue;

                var modifiers = entity.GetFacade<ICombatModifiersFacade>().Modifiers;
                modifiers.AddFighterCoordination();
                _affected.Add(modifiers);
            }
        }

        public bool CanStart(IShipAbilityFacade caster, IEntity target) =>
            caster.Health.GetShipUnits(HardPointType.CommandArray)
                .Any(point => point.HardPointType == HardPointType.CommandArray && !point.IsDestroyed);

        public void Advance(float deltaTime) { }

        public void Stop()
        {
            foreach (CombatModifiers modifiers in _affected)
            {
                modifiers.RemoveFighterCoordination();
            }
            _affected.Clear();
        }
    }
}
