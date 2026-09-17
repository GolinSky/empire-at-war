using Unity.Profiling;

namespace EmpireAtWar.Services.Timing
{
    internal static class BattleProfilerMarkers
    {
        public const string SHIP_TICK = "Battle.Ship.Tick";
        public const string WEAPON_TICK = "Battle.Weapon.Tick";
        public const string WEAPON_TRY_FIRE = "Battle.Weapon.TryFire";
        public const string TARGET_BATCH = "Battle.Attack.TargetBatch";
        public const string TARGET_BATCH_JOB = "Battle.Attack.TargetBatch.Job";
        public const string TARGET_BATCH_CAPTURE = "Battle.Attack.TargetBatch.Capture";
        public const string TARGET_BATCH_PREPARE = "Battle.Attack.TargetBatch.Prepare";
        public const string TARGET_BATCH_SERIAL = "Battle.Attack.TargetBatch.Serial";
        public const string TARGET_BATCH_SCHEDULE = "Battle.Attack.TargetBatch.Schedule";
        public const string TARGET_BATCH_COMPLETE = "Battle.Attack.TargetBatch.Complete";
        public const string TARGET_BATCH_APPLY = "Battle.Attack.TargetBatch.Apply";
        public const string DUE_BATCH = "Battle.Attack.DueBatch";
        public const string DUE_BATCH_JOB = "Battle.Attack.DueBatch.Job";
        public const string DUE_BATCH_PREPARE = "Battle.Attack.DueBatch.Prepare";
        public const string DUE_BATCH_SERIAL = "Battle.Attack.DueBatch.Serial";
        public const string DUE_BATCH_SCHEDULE = "Battle.Attack.DueBatch.Schedule";
        public const string DUE_BATCH_COMPLETE = "Battle.Attack.DueBatch.Complete";
        public const string DUE_BATCH_APPLY = "Battle.Attack.DueBatch.Apply";
        public const string PROJECTILE_GET_OR_CREATE = "Battle.Projectile.GetOrCreate";
        public const string PROJECTILE_INSTANTIATE = "Battle.Projectile.Instantiate";
        public const string PROJECTILE_TURRET_UPDATE = "Battle.Projectile.TurretUpdate";
        public const string PROJECTILE_LASER_UPDATE = "Battle.Projectile.LaserUpdate";
        public const string RADAR_SCAN = "Battle.Radar.Scan";

        public static readonly ProfilerMarker ShipTick = new ProfilerMarker(SHIP_TICK);
        public static readonly ProfilerMarker WeaponTick = new ProfilerMarker(WEAPON_TICK);
        public static readonly ProfilerMarker WeaponTryFire = new ProfilerMarker(WEAPON_TRY_FIRE);
        public static readonly ProfilerMarker TargetBatch = new ProfilerMarker(TARGET_BATCH);
        public static readonly ProfilerMarker TargetBatchJob = new ProfilerMarker(TARGET_BATCH_JOB);
        public static readonly ProfilerMarker TargetBatchCapture = new ProfilerMarker(TARGET_BATCH_CAPTURE);
        public static readonly ProfilerMarker TargetBatchPrepare = new ProfilerMarker(TARGET_BATCH_PREPARE);
        public static readonly ProfilerMarker TargetBatchSerial = new ProfilerMarker(TARGET_BATCH_SERIAL);
        public static readonly ProfilerMarker TargetBatchSchedule = new ProfilerMarker(TARGET_BATCH_SCHEDULE);
        public static readonly ProfilerMarker TargetBatchComplete = new ProfilerMarker(TARGET_BATCH_COMPLETE);
        public static readonly ProfilerMarker TargetBatchApply = new ProfilerMarker(TARGET_BATCH_APPLY);
        public static readonly ProfilerMarker DueBatch = new ProfilerMarker(DUE_BATCH);
        public static readonly ProfilerMarker DueBatchJob = new ProfilerMarker(DUE_BATCH_JOB);
        public static readonly ProfilerMarker DueBatchPrepare = new ProfilerMarker(DUE_BATCH_PREPARE);
        public static readonly ProfilerMarker DueBatchSerial = new ProfilerMarker(DUE_BATCH_SERIAL);
        public static readonly ProfilerMarker DueBatchSchedule = new ProfilerMarker(DUE_BATCH_SCHEDULE);
        public static readonly ProfilerMarker DueBatchComplete = new ProfilerMarker(DUE_BATCH_COMPLETE);
        public static readonly ProfilerMarker DueBatchApply = new ProfilerMarker(DUE_BATCH_APPLY);
        public static readonly ProfilerMarker ProjectileGetOrCreate = new ProfilerMarker(PROJECTILE_GET_OR_CREATE);
        public static readonly ProfilerMarker ProjectileInstantiate = new ProfilerMarker(PROJECTILE_INSTANTIATE);
        public static readonly ProfilerMarker ProjectileTurretUpdate = new ProfilerMarker(PROJECTILE_TURRET_UPDATE);
        public static readonly ProfilerMarker ProjectileLaserUpdate = new ProfilerMarker(PROJECTILE_LASER_UPDATE);
        public static readonly ProfilerMarker RadarScan = new ProfilerMarker(RADAR_SCAN);
    }
}
