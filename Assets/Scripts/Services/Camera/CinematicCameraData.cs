using System;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Services.Camera
{
    [CreateAssetMenu(fileName = nameof(CinematicCameraData), menuName = "Data/Cinematic Camera Data")]
    public class CinematicCameraData : Data
    {
        [field: SerializeField] public float MinShotDuration { get; private set; } = default;
        [field: SerializeField] public float MaxShotDuration { get; private set; } = default;
        [field: SerializeField] public float DestroyedTargetLinger { get; private set; } = default;
        [field: SerializeField] public float ActivitySampleInterval { get; private set; } = default;
        [field: SerializeField] public float FollowSharpness { get; private set; } = default;
        [field: SerializeField] public float WideShotDistanceMultiplier { get; private set; } = default;
        [field: SerializeField] public float DamageMemorySeconds { get; private set; } = default;
        [field: SerializeField] public float DamageWeight { get; private set; } = default;
        [field: SerializeField] public float EngagementRadius { get; private set; } = default;
        [field: SerializeField] public float NearbyEnemyWeight { get; private set; } = default;
        [field: SerializeField] public int MaxCountedEnemies { get; private set; } = default;
        [field: SerializeField] public float RepeatTargetPenalty { get; private set; } = default;
        [field: SerializeField] public float ScoreJitter { get; private set; } = default;
        [field: SerializeField] public CinematicClassProfile[] ClassProfiles { get; private set; } = default;

        public CinematicClassProfile GetClassProfile(ShipClass shipClass)
        {
            for (int i = 0; i < ClassProfiles.Length; i++)
            {
                if (ClassProfiles[i].ShipClass == shipClass)
                {
                    return ClassProfiles[i];
                }
            }

            throw new ArgumentOutOfRangeException(
                nameof(shipClass), shipClass, "No cinematic class profile configured.");
        }
    }
}
