using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class HarmonicBombAbility : IShipAbility
    {
        private readonly HarmonicBombService _bombs;
        private readonly HarmonicBombSettings _settings;

        public HarmonicBombAbility(HarmonicBombService bombs, HarmonicBombSettings settings)
        {
            _bombs = bombs;
            _settings = settings;
        }

        public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target) =>
            _bombs.Drop(caster.Entity.Owner, caster.WorldPosition, definition.Duration,
                _settings.Radius, _settings.ViewPrefab);
    }
}
