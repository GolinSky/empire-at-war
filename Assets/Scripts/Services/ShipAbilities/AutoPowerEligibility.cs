using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;

namespace EmpireAtWar.Services.ShipAbilities
{
    public static class AutoPowerEligibility
    {
        public static bool IsEligible(ShipAbilitySlot slot) =>
            slot.Id != ShipAbilityId.ImperialVenatorIntensifyFirepower &&
            (slot.Definition.Settings is BoostShieldPowerSettings ||
             slot.Definition.Settings is BoostWeaponPowerSettings ||
             slot.Definition.Settings is PowerToMainBatteriesSettings) ||
             slot.Definition.Settings is BoostWeaponPowerSettings ||
             IsLaserBeam(slot.Id));

        public static bool IsLaserBeam(ShipAbilityId id) =>
            id == ShipAbilityId.LaserBeam || id == ShipAbilityId.ExecutorLaserBeam;
    }
}
