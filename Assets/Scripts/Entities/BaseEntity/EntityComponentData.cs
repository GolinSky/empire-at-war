using System;
using System.Collections.Generic;
using EmpireAtWar.Components.FogOfWar;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Health;
using UnityEngine;
using Utilities.ScriptUtils.Math;

namespace EmpireAtWar.Entities.BaseEntity
{
    [Serializable]
    public sealed class EntityComponentData : IHealthData, IRadarData, IFogVisionData
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
        [field: SerializeField] public float Range { get; private set; }
        [field: SerializeField] public float Delay { get; private set; }

        [Header("Vision Settings")]
        [Tooltip("Fog of war radius this structure reveals for its team.")]
        [field: SerializeField, Min(0f)] public float VisionRange { get; private set; }
    }
}
