using System.Collections.Generic;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using EmpireAtWar.Ship;
using EmpireAtWar.Services.UnitDeathAnimation;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Utils.Random;
using UnityEngine;
using Utilities.ScriptUtils.Math;
using Zenject;

namespace EmpireAtWar.Entities.Ship.Data
{
    public interface IShipData
    {
        ParticleSystem DeathExplosionVfx { get; }
        float MinMoveCoefficient { get; }
    }

    [CreateAssetMenu(fileName = "ShipData", menuName = "Data/ShipData")]
    public class ShipData : Mvc.Data, IModel, IShipModelObserver, IShipData,
        IShipMoveData, IHealthData, IRadarData, IUnitDeathAnimationData, IHangarData
    {
        [Inject] public ShipType ShipType { get; private set; }

        [Header("Ship Settings")]
        [field: SerializeField] public ParticleSystem DeathExplosionVfx { get; private set; }
        [field: SerializeField] public float MinMoveCoefficient { get; private set; }

        [Header("Movement Settings")]
        [field: SerializeField] public float Speed { get; private set; }
        [field: SerializeField] public float Height { get; private set; }
        [field: SerializeField] public float RotationSpeed { get; private set; }
        [field: SerializeField] public float TurnAcceleration { get; private set; }
        [field: SerializeField] public float HyperSpaceDuration { get; private set; }
        [field: SerializeField] public float BodyRotationMaxAngle { get; private set; }
        [field: SerializeField] public float NavigationRadius { get; private set; } = 8f;

        [Header("Death Animation Settings")]
        [field: SerializeField] public Vector3 FallDownDirection { get; private set; }
        [field: SerializeField] public RandomVector3 FallDownRotation { get; private set; }
        [field: SerializeField] public float FallDownDuration { get; private set; }
        Vector3 IUnitDeathAnimationData.FallDownRotation => FallDownRotation.Value;

        [Header("Health Settings")]
        [field: SerializeField] public ShipClass ShipClass { get; private set; }
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

        [Header("Abilities")]
        [SerializeField] private List<ShipAbilityId> abilities = new List<ShipAbilityId>();
        public IReadOnlyList<ShipAbilityId> Abilities => abilities;

        [Header("Hangar")]
        [Tooltip("Squadron bays; a ship with bays needs a HangarComponent on its view prefab.")]
        [SerializeField] private List<HangarBay> hangarBays = new List<HangarBay>();
        [field: SerializeField] public float HangarInitialDelay { get; private set; } = 4f;
        [field: SerializeField] public float HangarLaunchInterval { get; private set; } = 8f;
        public IReadOnlyList<HangarBay> HangarBays => hangarBays;
    }
}
