using System;
using System.Collections.Generic;
using EmpireAtWar.Components.FogOfWar;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.SpawnBlocking;
using UnityEngine;
using Utilities.ScriptUtils.Math;

namespace EmpireAtWar.Entities.BaseEntity
{
    [Serializable]
    public sealed class EntityComponentData : IHealthData, IRadarData, IFogVisionData, ISpawnBlockerData,
        IWeaponRangeData
    {
        [Header("Destruction Settings")]
        [Tooltip("Seconds the dead unit stays under its explosion before it is removed.")]
        [field: SerializeField, Min(0f)] public float DestroyDelay { get; private set; } = 0.35f;

        [Header("Health Settings")]
        [field: SerializeField] public ShipClass ShipClass { get; private set; } = ShipClass.Structure;
        [field: SerializeField] public float Hull { get; private set; }
        [field: SerializeField] public float Shields { get; private set; }
        [field: SerializeField] public float ShieldRegenerateValue { get; private set; }
        [field: SerializeField] public float ShieldRegenerateDelay { get; private set; }
        [field: SerializeField] public FloatRange ShieldDangerStateRange { get; private set; }
        [SerializeField] private List<HardPointHealth> hardPointHealth = new List<HardPointHealth>();
        public IReadOnlyList<HardPointHealth> HardPointHealth => hardPointHealth;

        [Header("Radar Settings")]
        [Tooltip("Radius in which the structure detects enemies. Matches VisionRange.")]
        [field: SerializeField] public float Range { get; private set; }
        [field: SerializeField] public float Delay { get; private set; }

        [Header("Weapon Settings")]
        [Tooltip("How far the structure's hardpoints can fire.")]
        [field: SerializeField, Min(0f)] public float WeaponRange { get; private set; }

        [Header("Vision Settings")]
        [Tooltip("Fog of war radius this structure reveals for its team.")]
        [field: SerializeField, Min(0f)] public float VisionRange { get; private set; }

        [Header("Spawn Block Settings")]
        [Tooltip("Hostile reinforcements cannot arrive within this radius of the structure.")]
        [field: SerializeField, Min(0f)] public float SpawnBlockRadius { get; private set; }
    }
}
