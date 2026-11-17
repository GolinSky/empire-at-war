using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Services.Saves;

namespace EmpireAtWar.Components.Weapon
{
    public static class SiegeDamage
    {
        private const float DREADNOUGHT_MULTIPLIER = 1.5f;
        private const float BATTLE_CRUISER_MULTIPLIER = 1.2f;

        public static float GetMultiplier(ShipClass attackerClass, StructureKind? targetKind)
        {
            if (!IsEligibleTarget(targetKind)) return 1f;
            return attackerClass switch
            {
                ShipClass.Dreadnought => DREADNOUGHT_MULTIPLIER,
                ShipClass.BattleCruiser => BATTLE_CRUISER_MULTIPLIER,
                _ => 1f
            };
        }

        public static bool IsEligibleTarget(StructureKind? kind) =>
            kind is StructureKind.SpaceStation or StructureKind.SecondStation or StructureKind.DefendPlatform;
    }
}
