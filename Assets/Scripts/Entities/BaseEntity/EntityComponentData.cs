using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.UnitDeathAnimation;
using EmpireAtWar.Utils.Random;
using UnityEngine;
using Utilities.ScriptUtils.Math;

namespace EmpireAtWar.Entities.BaseEntity
{
    [Serializable]
    public sealed class EntityComponentData : IUnitDeathAnimationData, IHealthData, IRadarData
    {
        [Header("Death Animation Settings")]
        [field: SerializeField] public Vector3 FallDownDirection { get; private set; }
        [field: SerializeField] public RandomVector3 FallDownRotation { get; private set; }
        [field: SerializeField] public float FallDownDuration { get; private set; }
        Vector3 IUnitDeathAnimationData.FallDownRotation => FallDownRotation.Value;

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
        [field: SerializeField] public float Distance { get; private set; }
    }
}
