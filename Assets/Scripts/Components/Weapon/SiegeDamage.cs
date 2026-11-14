using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Services.Saves;

namespace EmpireAtWar.Components.Weapon
{
    public static class SiegeDamage
    {
        private const float HEAVY_CAPITAL_MULTIPLIER = 1.5f;

        public static float GetMultiplier(ShipClass attackerClass, StructureKind? targetKind) =>
            attackerClass == ShipClass.HeavyCapital && IsEligibleTarget(targetKind) ? HEAVY_CAPITAL_MULTIPLIER : 1f;

        public static bool IsEligibleTarget(StructureKind? kind) =>
            kind is StructureKind.SpaceStation or StructureKind.SecondStation or StructureKind.DefendPlatform;
    }
}
