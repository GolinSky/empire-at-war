using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;

namespace EmpireAtWar.Services.ShipAbilities
{
    public static class AutoPowerEligibility
    {
        public static bool IsEligible(ShipAbilitySlot slot) =>
            (int)slot.Id != 24 && (int)slot.Id != 35 &&
            (slot.Definition.Settings is BoostShieldPowerSettings || slot.Definition.Settings is BoostWeaponPowerSettings);
    }
}
