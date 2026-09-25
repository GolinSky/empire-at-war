using System;
using EmpireAtWar.Components.Ship.Health;
using UnityEngine;

namespace EmpireAtWar.Services.Camera
{
    [Serializable]
    public class CinematicCameraSettings
    {
        [SerializeField] private float minShotDuration = 3f;
        [SerializeField] private float maxShotDuration = 8f;
        [SerializeField] private float destroyedTargetLinger = 1.5f;
        [SerializeField] private float activitySampleInterval = 0.5f;
        [SerializeField] private float followSharpness = 3f;
        [SerializeField] private float wideShotDistanceMultiplier = 2.5f;
        [SerializeField] private float damageMemorySeconds = 4f;
        [SerializeField] private float damageWeight = 6f;
        [SerializeField] private float engagementRadius = 250f;
        [SerializeField] private float nearbyEnemyWeight = 1f;
        [SerializeField] private int maxCountedEnemies = 5;
        [SerializeField] private float repeatTargetPenalty = 0.35f;
        [SerializeField] private float scoreJitter = 0.3f;
        [SerializeField] private CinematicClassProfile[] classProfiles =
        {
            new CinematicClassProfile(ShipClass.Fighter, 1.5f, 20f),
            new CinematicClassProfile(ShipClass.Bomber, 1.5f, 20f),
            new CinematicClassProfile(ShipClass.Corvette, 2f, 45f),
            new CinematicClassProfile(ShipClass.Frigate, 3f, 70f),
            new CinematicClassProfile(ShipClass.Capital, 4f, 110f),
            new CinematicClassProfile(ShipClass.HeavyCapital, 5f, 170f),
            new CinematicClassProfile(ShipClass.Structure, 2f, 200f),
        };

        public float MinShotDuration => minShotDuration;
        public float MaxShotDuration => maxShotDuration;
        public float DestroyedTargetLinger => destroyedTargetLinger;
        public float ActivitySampleInterval => activitySampleInterval;
        public float FollowSharpness => followSharpness;
        public float WideShotDistanceMultiplier => wideShotDistanceMultiplier;
        public float DamageMemorySeconds => damageMemorySeconds;
        public float DamageWeight => damageWeight;
        public float EngagementRadius => engagementRadius;
        public float NearbyEnemyWeight => nearbyEnemyWeight;
        public int MaxCountedEnemies => maxCountedEnemies;
        public float RepeatTargetPenalty => repeatTargetPenalty;
        public float ScoreJitter => scoreJitter;

        public CinematicClassProfile GetClassProfile(ShipClass shipClass)
        {
            for (int i = 0; i < classProfiles.Length; i++)
            {
                if (classProfiles[i].ShipClass == shipClass)
                {
                    return classProfiles[i];
                }
            }

            throw new ArgumentOutOfRangeException(
                nameof(shipClass), shipClass, "No cinematic class profile configured.");
        }
    }
}
