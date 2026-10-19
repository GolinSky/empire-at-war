using System;
using System.Linq;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.Saves
{
    /// <summary>Superweapon states and cooldowns; a charging weapon's build time is in its owner's production.</summary>
    public sealed class SuperWeaponSaveSource : PlayerSaveSource
    {
        private readonly SuperWeaponModel _superWeapons;

        public SuperWeaponSaveSource(PlayerSaveRegistry registry, PlayerSlot owner, SuperWeaponModel superWeapons)
            : base(registry, owner)
        {
            _superWeapons = superWeapons;
        }

        public override void Capture(PlayerSave save) => save.SuperWeapons = Enum.GetValues(typeof(SuperWeaponType))
            .Cast<SuperWeaponType>()
            .Select(type => SuperWeaponSave.Create(type, _superWeapons.GetState(type), _superWeapons.GetCooldownRemaining(type)))
            .ToArray();

        public override void RestoreBeforeUnits(PlayerSave save)
        {
            foreach (SuperWeaponSave weapon in save.SuperWeapons)
                _superWeapons.Restore(weapon.Type, weapon.State, weapon.Cooldown);
        }
    }
}
