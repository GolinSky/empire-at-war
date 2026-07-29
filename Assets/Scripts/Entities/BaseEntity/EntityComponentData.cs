using System;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.UnitDeathAnimation;
using EmpireAtWar.Utils.Random;
using UnityEngine;
using Utilities.ScriptUtils.Math;

namespace EmpireAtWar.Entities.BaseEntity
{
    [Serializable]
    public sealed class EntityComponentData : IUnitDeathAnimationData, IHealthData, IAttackData,
        IRadarData
    {
        [Header("Death Animation Settings")]
        [field: SerializeField] public Vector3 FallDownDirection { get; private set; }
        [field: SerializeField] public RandomVector3 FallDownRotation { get; private set; }
        [field: SerializeField] public float FallDownDuration { get; private set; }
        Vector3 IUnitDeathAnimationData.FallDownRotation => FallDownRotation.Value;

        [Header("Health Settings")]
        [field: SerializeField] public float Armor { get; private set; }
        [field: SerializeField, Range(0f, 1f)] public float Dexterity { get; private set; }
        [field: SerializeField] public float Shields { get; private set; }
        [field: SerializeField] public float ShieldRegenerateValue { get; private set; }
        [field: SerializeField] public float ShieldRegenerateDelay { get; private set; }
        [field: SerializeField] public FloatRange ShieldDangerStateRange { get; private set; }

        [Header("Attack Settings")]
        [field: SerializeField] public float DelayBetweenAttack { get; private set; }

        [Header("Radar Settings")]
        [field: SerializeField] public float Range { get; private set; }
        [field: SerializeField] public float Delay { get; private set; }
        [field: SerializeField] public float Distance { get; private set; }
    }
}
