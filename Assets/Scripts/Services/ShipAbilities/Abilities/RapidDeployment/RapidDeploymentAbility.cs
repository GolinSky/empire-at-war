using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class RapidDeploymentAbility : IPhasedShipAbility
    {
        private IHangarCommand _hangar;
        private int _remaining;

        public bool IsComplete => _remaining == 0;
        public bool SurvivesCasterDeath => false;

        public bool CanStart(IShipAbilityFacade caster, IEntity target) =>
            caster.Entity.GetFacade<IHangarCommand>().CanLaunchImmediately;

        public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target)
        {
            _hangar = caster.Entity.GetFacade<IHangarCommand>();
            _remaining = 2;
            Advance(0);
        }

        public void Advance(float deltaTime)
        {
            if (_remaining == 0) return;
            if (!_hangar.CanLaunchImmediately) _remaining = 0;
            else if (_hangar.TryLaunchImmediately()) _remaining--;
        }

        public void Stop() => _remaining = 0;
    }
}
