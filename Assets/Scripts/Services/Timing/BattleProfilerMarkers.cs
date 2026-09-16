using Unity.Profiling;

namespace EmpireAtWar.Services.Timing
{
    internal static class BattleProfilerMarkers
    {
        public const string SHIP_TICK = "Battle.Ship.Tick";
        public const string WEAPON_TICK = "Battle.Weapon.Tick";
        public const string WEAPON_TRY_FIRE = "Battle.Weapon.TryFire";
        public const string PROJECTILE_GET_OR_CREATE = "Battle.Projectile.GetOrCreate";
        public const string PROJECTILE_INSTANTIATE = "Battle.Projectile.Instantiate";
        public const string PROJECTILE_TURRET_UPDATE = "Battle.Projectile.TurretUpdate";
        public const string PROJECTILE_LASER_UPDATE = "Battle.Projectile.LaserUpdate";
        public const string RADAR_SCAN = "Battle.Radar.Scan";

        public static readonly ProfilerMarker ShipTick = new ProfilerMarker(SHIP_TICK);
        public static readonly ProfilerMarker WeaponTick = new ProfilerMarker(WEAPON_TICK);
        public static readonly ProfilerMarker WeaponTryFire = new ProfilerMarker(WEAPON_TRY_FIRE);
        public static readonly ProfilerMarker ProjectileGetOrCreate = new ProfilerMarker(PROJECTILE_GET_OR_CREATE);
        public static readonly ProfilerMarker ProjectileInstantiate = new ProfilerMarker(PROJECTILE_INSTANTIATE);
        public static readonly ProfilerMarker ProjectileTurretUpdate = new ProfilerMarker(PROJECTILE_TURRET_UPDATE);
        public static readonly ProfilerMarker ProjectileLaserUpdate = new ProfilerMarker(PROJECTILE_LASER_UPDATE);
        public static readonly ProfilerMarker RadarScan = new ProfilerMarker(RADAR_SCAN);
    }
}
