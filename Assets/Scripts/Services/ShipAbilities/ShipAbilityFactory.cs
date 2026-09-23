using System;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using Zenject;

namespace EmpireAtWar.Services.ShipAbilities
{
    public sealed class ShipAbilityFactory : IShipAbilityFactory
    {
        private readonly DiContainer _container;

        public ShipAbilityFactory(DiContainer container) { _container = container; }

        public IShipAbility Create(ShipAbilityId id) => id switch
        {
            ShipAbilityId.ProtonBeam => _container.Instantiate<ProtonBeamAbility>(),
            ShipAbilityId.Invulnerability => _container.Instantiate<InvulnerabilityAbility>(),
            ShipAbilityId.BoostShieldPower => _container.Instantiate<BoostShieldPowerAbility>(),
            ShipAbilityId.BoostEnginePower => _container.Instantiate<BoostEnginePowerAbility>(),
            ShipAbilityId.BoostWeaponPower => _container.Instantiate<BoostWeaponPowerAbility>(),
            ShipAbilityId.Assault => _container.Instantiate<AssaultAbility>(),
            ShipAbilityId.ConcentrateFire => _container.Instantiate<ConcentrateFireAbility>(),
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, null)
        };
    }
}
