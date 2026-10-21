using System;
using System.Linq;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Enemy;

namespace EmpireAtWar.Services.Saves
{
    /// <summary>AI superweapon state, cooldown and independently running charge timers.</summary>
    public sealed class AiSuperWeaponSaveSource : PlayerSaveSource
    {
        private readonly SuperWeaponModel _superWeapons;
        private readonly EnemySuperWeaponController _controller;

        public AiSuperWeaponSaveSource(PlayerSaveRegistry registry, PlayerSlot owner,
            SuperWeaponModel superWeapons, EnemySuperWeaponController controller) : base(registry, owner)
        {
            _superWeapons = superWeapons;
            _controller = controller;
        }

        public override void Capture(PlayerSave save) => save.SuperWeapons = Enum.GetValues(typeof(SuperWeaponType))
            .Cast<SuperWeaponType>()
            .Select(type => SuperWeaponSave.Create(type, _superWeapons.GetState(type),
                _superWeapons.GetCooldownRemaining(type),
                _superWeapons.GetState(type) == SuperWeaponState.Charging
                    ? _controller.GetChargeTimeRemaining(type) : 0f))
            .ToArray();

        public override void RestoreBeforeUnits(PlayerSave save)
        {
            foreach (SuperWeaponSave weapon in save.SuperWeapons)
            {
                _superWeapons.Restore(weapon.Type, weapon.State, weapon.Cooldown);
                if (weapon.State == SuperWeaponState.Charging)
                    _controller.RestoreCharging(weapon.Type, weapon.HasChargeTime ? weapon.ChargeTimeLeft : (float?)null);
            }
        }
    }
}
