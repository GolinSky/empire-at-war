using System;

namespace EmpireAtWar.Editor.Balance
{
    // Mirrors WeaponHardPoint: the reload starts with the salvo, and the next salvo waits for both to finish.
    // Projectile travel time depends on distance and is not included.
    public static class BalanceWeaponMath
    {
        public static double CycleSeconds(int shotsPerSalvo, double shotInterval, double reload) =>
            Math.Max(reload, (shotsPerSalvo - 1) * shotInterval);

        public static double SalvoDps(double damage, int shotsPerSalvo, double shotInterval, double reload) =>
            damage * shotsPerSalvo / CycleSeconds(shotsPerSalvo, shotInterval, reload);
    }
}
