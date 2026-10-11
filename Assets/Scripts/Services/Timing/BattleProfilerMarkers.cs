using Unity.Profiling;

namespace EmpireAtWar.Services.Timing
{
    internal static class BattleProfilerMarkers
    {
        public const string SHIP_TICK = "Battle.Ship.Tick";
        public const string SHIP_ORDER_TICK = "Battle.Ship.OrderTick";
        public const string SHIP_AUDIO = "Battle.Ship.Audio";
        public const string SQUADRON_ORDER = "Battle.Squadron.UpdateOrder";
        public const string SQUADRON_PILOT = "Battle.Squadron.Pilot";
        public const string SQUADRON_FLIGHT = "Battle.Squadron.Flight";
        public const string DUE_SORT = "Battle.Attack.DueBatch.Sort";
        public const string DUE_LOOKUP = "Battle.Attack.DueBatch.Lookup";
        public const string DUE_EMISSION = "Battle.Attack.DueBatch.Emission";
        public const string DUE_IMPACT = "Battle.Attack.DueBatch.Impact";
        public const string SFX_APPLY_MIX = "Battle.Audio.ApplyMix";
        public const string NAVIGATION_GRID_BUILD = "Battle.Navigation.GridBuild";
        public const string NAVIGATION_STATIONARY_REUSE = "Battle.Navigation.StationaryReuse";
        public const string NAVIGATION_PRESERVE_COURSE = "Battle.Navigation.PreserveCourse";
        public const string NAVIGATION_NEW_COURSE = "Battle.Navigation.NewCourse";
        public const string NAVIGATION_ARRIVAL = "Battle.Navigation.Arrival";
        public const string TARGET_FALLBACK_NON_FINITE = "Battle.Attack.TargetFallback.NonFinite";
        public const string TARGET_FALLBACK_PARENT_TILT = "Battle.Attack.TargetFallback.ParentTilt";
        public const string TARGET_FALLBACK_RANGE_BOUNDARY = "Battle.Attack.TargetFallback.RangeBoundary";
        public const string TARGET_FALLBACK_YAW_BOUNDARY = "Battle.Attack.TargetFallback.YawBoundary";
        public const string TARGET_FALLBACK_DEGENERATE_DIRECTION = "Battle.Attack.TargetFallback.DegenerateDirection";
        public const string TARGET_FALLBACK_TARGET_VERSION = "Battle.Attack.TargetFallback.TargetVersion";
        public const string TARGET_FALLBACK_INVALID_CANDIDATE = "Battle.Attack.TargetFallback.InvalidCandidate";
        public const string TARGET_FALLBACK_OUT_OF_SPAN = "Battle.Attack.TargetFallback.OutOfSpan";
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
        public const string PROJECTILE_TRACK = "Battle.Projectile.Track";
        public const string PROJECTILE_MOVE = "Battle.Projectile.Move";
        public const string PROJECTILE_EFFECT = "Battle.Projectile.Effect";
        public const string SHIP_FACING_SOLVE = "Battle.Ship.FacingSolve";
        public const string RADAR_SCAN = "Battle.Radar.Scan";
        public const string RADAR_SNAPSHOT = "Battle.Radar.Snapshot";
        public const string RADAR_JOB = "Battle.Radar.Job";
        // One sample per delivered radar scan: reconciliation plus enemy-added listeners.
        public const string RADAR_APPLY = "Battle.Radar.Apply";
        public const string NAVIGATION_PLAN = "Battle.Navigation.Plan";
        public const string NAVIGATION_CANDIDATES = "Battle.Navigation.Candidates";
        public const string NAVIGATION_ROUTE_BUILD = "Battle.Navigation.RouteBuild";
        public const string NAVIGATION_FLOOD = "Battle.Navigation.Flood";
        public const string NAVIGATION_NEAREST_REACHABLE = "Battle.Navigation.NearestReachable";

        public static readonly ProfilerMarker ShipTick = new ProfilerMarker(SHIP_TICK);
        public static readonly ProfilerMarker ShipOrderTick = new ProfilerMarker(SHIP_ORDER_TICK);
        public static readonly ProfilerMarker ShipAudio = new ProfilerMarker(SHIP_AUDIO);
        public static readonly ProfilerMarker SquadronOrder = new ProfilerMarker(SQUADRON_ORDER);
        public static readonly ProfilerMarker SquadronPilot = new ProfilerMarker(SQUADRON_PILOT);
        public static readonly ProfilerMarker SquadronFlight = new ProfilerMarker(SQUADRON_FLIGHT);
        public static readonly ProfilerMarker DueSort = new ProfilerMarker(DUE_SORT);
        public static readonly ProfilerMarker DueLookup = new ProfilerMarker(DUE_LOOKUP);
        public static readonly ProfilerMarker DueEmission = new ProfilerMarker(DUE_EMISSION);
        public static readonly ProfilerMarker DueImpact = new ProfilerMarker(DUE_IMPACT);
        public static readonly ProfilerMarker SfxApplyMix = new ProfilerMarker(SFX_APPLY_MIX);
        public static readonly ProfilerMarker NavigationGridBuild = new ProfilerMarker(NAVIGATION_GRID_BUILD);
        public static readonly ProfilerMarker NavigationStationaryReuse = new ProfilerMarker(NAVIGATION_STATIONARY_REUSE);
        public static readonly ProfilerMarker NavigationPreserveCourse = new ProfilerMarker(NAVIGATION_PRESERVE_COURSE);
        public static readonly ProfilerMarker NavigationNewCourse = new ProfilerMarker(NAVIGATION_NEW_COURSE);
        public static readonly ProfilerMarker NavigationArrival = new ProfilerMarker(NAVIGATION_ARRIVAL);
        public static readonly ProfilerMarker TargetFallbackNonFinite = new ProfilerMarker(TARGET_FALLBACK_NON_FINITE);
        public static readonly ProfilerMarker TargetFallbackParentTilt = new ProfilerMarker(TARGET_FALLBACK_PARENT_TILT);
        public static readonly ProfilerMarker TargetFallbackRangeBoundary = new ProfilerMarker(TARGET_FALLBACK_RANGE_BOUNDARY);
        public static readonly ProfilerMarker TargetFallbackYawBoundary = new ProfilerMarker(TARGET_FALLBACK_YAW_BOUNDARY);
        public static readonly ProfilerMarker TargetFallbackDegenerateDirection = new ProfilerMarker(TARGET_FALLBACK_DEGENERATE_DIRECTION);
        public static readonly ProfilerMarker TargetFallbackTargetVersion = new ProfilerMarker(TARGET_FALLBACK_TARGET_VERSION);
        public static readonly ProfilerMarker TargetFallbackInvalidCandidate = new ProfilerMarker(TARGET_FALLBACK_INVALID_CANDIDATE);
        public static readonly ProfilerMarker TargetFallbackOutOfSpan = new ProfilerMarker(TARGET_FALLBACK_OUT_OF_SPAN);
        public static readonly ProfilerMarker[] TargetFallbackMarkers =
        {
            TargetFallbackNonFinite,
            TargetFallbackParentTilt,
            TargetFallbackRangeBoundary,
            TargetFallbackYawBoundary,
            TargetFallbackDegenerateDirection,
            TargetFallbackTargetVersion,
            TargetFallbackInvalidCandidate,
            TargetFallbackOutOfSpan,
        };
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
        public static readonly ProfilerMarker ProjectileTrack = new ProfilerMarker(PROJECTILE_TRACK);
        public static readonly ProfilerMarker ProjectileMove = new ProfilerMarker(PROJECTILE_MOVE);
        public static readonly ProfilerMarker ProjectileEffect = new ProfilerMarker(PROJECTILE_EFFECT);
        public static readonly ProfilerMarker ShipFacingSolve = new ProfilerMarker(SHIP_FACING_SOLVE);
        public static readonly ProfilerMarker RadarScan = new ProfilerMarker(RADAR_SCAN);
        public static readonly ProfilerMarker RadarSnapshot = new ProfilerMarker(RADAR_SNAPSHOT);
        public static readonly ProfilerMarker RadarJob = new ProfilerMarker(RADAR_JOB);
        public static readonly ProfilerMarker RadarApply = new ProfilerMarker(RADAR_APPLY);
        public static readonly ProfilerMarker NavigationPlan = new ProfilerMarker(NAVIGATION_PLAN);
        public static readonly ProfilerMarker NavigationCandidates = new ProfilerMarker(NAVIGATION_CANDIDATES);
        public static readonly ProfilerMarker NavigationRouteBuild = new ProfilerMarker(NAVIGATION_ROUTE_BUILD);
        public static readonly ProfilerMarker NavigationFlood = new ProfilerMarker(NAVIGATION_FLOOD);
        public static readonly ProfilerMarker NavigationNearestReachable =
            new ProfilerMarker(NAVIGATION_NEAREST_REACHABLE);
    }
}
