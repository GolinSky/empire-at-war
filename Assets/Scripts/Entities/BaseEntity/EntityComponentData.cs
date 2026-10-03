using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Health;
using UnityEngine;
using Utilities.ScriptUtils.Math;

namespace EmpireAtWar.Entities.BaseEntity
{
    [Serializable]
    public sealed class EntityComponentData : IHealthData, IRadarData
    {
        [SerializeField] private List<HardPointHealth> hardPointHealth = new List<HardPointHealth>();

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
        public IReadOnlyList<HardPointHealth> HardPointHealth => hardPointHealth;

        [Header("Radar Settings")]
        [field: SerializeField] public float Range { get; private set; }
        [field: SerializeField] public float Delay { get; private set; }
    }
}
