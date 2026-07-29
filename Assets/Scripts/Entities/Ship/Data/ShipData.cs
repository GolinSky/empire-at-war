using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Utils.Random;
using UnityEngine;
using Utilities.ScriptUtils.Math;

namespace EmpireAtWar.Entities.Ship.Data
{
    public interface IShipData
    {
        ParticleSystem DeathExplosionVfx { get; }
        float MinMoveCoefficient { get; }
    }

    [CreateAssetMenu(fileName = "ShipData", menuName = "Data/ShipData")]
    public class ShipData : Mvc.Data, IShipData, IShipMoveData, IHealthData, IAttackData,
        IRadarData, IWeaponContext
    {
        [Header("Ship Settings")]
        [field: SerializeField] public ParticleSystem DeathExplosionVfx { get; private set; }
        [field: SerializeField] public float MinMoveCoefficient { get; private set; }

        [Header("Movement Settings")]
        [field: SerializeField] public float Speed { get; private set; }
        [field: SerializeField] public float Height { get; private set; }
        [field: SerializeField] public Vector3 FallDownDirection { get; private set; }
        [field: SerializeField] public RandomVector3 FallDownRotation { get; private set; }
        [field: SerializeField] public float FallDownDuration { get; private set; }
        [field: SerializeField] public bool CanMove { get; private set; } = true;
        [field: SerializeField] public float RotationSpeed { get; private set; }
        [field: SerializeField] public float HyperSpaceDuration { get; private set; }
        [field: SerializeField] public float BodyRotationMaxAngle { get; private set; }
        [field: SerializeField] public float NavigationRadius { get; private set; } = 8f;

        [Header("Health Settings")]
        [field: SerializeField] public float Armor { get; private set; }
        [field: SerializeField, Range(0f, 1f)] public float Dexterity { get; private set; }
        [field: SerializeField] public float Shields { get; private set; }
        [field: SerializeField] public float ShieldRegenerateValue { get; private set; }
        [field: SerializeField] public float ShieldRegenerateDelay { get; private set; }
        [field: SerializeField] public FloatRange ShieldDangerStateRange { get; private set; }

        [Header("Attack Settings")]
        [field: SerializeField] public float AttackDelayBetweenAttack { get; private set; }
        [field: SerializeField] public float DelayBetweenAttack { get; private set; }

        float IAttackData.DelayBetweenAttack => AttackDelayBetweenAttack;

        [Header("Radar Settings")]
        [field: SerializeField] public float Range { get; private set; }
        [field: SerializeField] public float Delay { get; private set; }
        [field: SerializeField] public float Distance { get; private set; }
    }
}
