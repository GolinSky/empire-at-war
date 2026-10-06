using EmpireAtWar.Components.FogOfWar;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Squadrons.Flight;
using EmpireAtWar.Components.Squadrons.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.ShipAbilities;
using UnityEngine;

namespace EmpireAtWar.Entities.Squadrons.Data
{
    [CreateAssetMenu(fileName = "SquadronData", menuName = "Data/SquadronData")]
    public class SquadronData : Mvc.Data, IFighterFlightData,
        ISquadronHealthData, IRadarData, IFogVisionData, IWeaponRangeData
    {
        [Header("Orders")]
        [Tooltip("Radius used when compacting group move orders.")]
        [field: SerializeField] public float NavigationRadius { get; private set; } = 6f;
        [Tooltip("Enemies closer than this to the guarded unit's hull or the loiter point are engaged automatically.")]
        [field: SerializeField] public float GuardRadius { get; private set; } = 70f;

        [Header("Flight")]
        [field: SerializeField] public float CruiseSpeed { get; private set; } = 12f;
        [field: SerializeField] public float CombatSpeed { get; private set; } = 14f;
        [field: SerializeField] public float Acceleration { get; private set; } = 10f;
        [Tooltip("Degrees per second.")]
        [field: SerializeField] public float TurnRate { get; private set; } = 110f;
        [field: SerializeField] public float MaxBankAngle { get; private set; } = 60f;
        [field: SerializeField] public float BankResponse { get; private set; } = 4f;
        [field: SerializeField] public float Height { get; private set; } = 10f;
        [field: SerializeField] public float FormationSpacing { get; private set; } = 2f;
        [field: SerializeField] public float LoiterRadius { get; private set; } = 18f;

        [Header("Attack Runs")]
        [Tooltip("Distance to the aim point at which a fighter stops its approach and flies past.")]
        [field: SerializeField] public float BreakDistance { get; private set; } = 5f;
        [Tooltip("Distance a fighter extends away before turning back for another pass.")]
        [field: SerializeField] public float ExtendDistance { get; private set; } = 30f;

        [Header("Health (per fighter)")]
        [field: SerializeField] public ShipClass ShipClass { get; private set; } = ShipClass.Fighter;
        [field: SerializeField] public float MemberHull { get; private set; } = 80f;
        [field: SerializeField] public float MemberShields { get; private set; } = 25f;
        [field: SerializeField] public float ShieldRegenerateValue { get; private set; } = 2f;
        [field: SerializeField] public float ShieldRegenerateDelay { get; private set; } = 2f;
        [Tooltip("Hull points repaired per second on each surviving fighter.")]
        [field: SerializeField] public float HullRepairPerSecond { get; private set; }

        [Header("Abilities")]
        [field: SerializeField] public ShipAbilityId[] Abilities { get; private set; } = System.Array.Empty<ShipAbilityId>();

        [Header("Seeker Warhead Countermeasure")]
        [field: SerializeField] public float SeekerWarheadRange { get; private set; }
        [field: SerializeField] public float SeekerWarheadRecharge { get; private set; } = 10f;
        [field: SerializeField] public float SeekerWarheadMinimumTravel { get; private set; } = 10f;

        [Header("Radar")]
        [Tooltip("Radius in which the squadron detects enemies. Matches VisionRange.")]
        [field: SerializeField] public float Range { get; private set; } = 175f;
        [field: SerializeField] public float Delay { get; private set; } = 0.25f;

        [Header("Weapons")]
        [Tooltip("How far the fighters' hardpoints can fire.")]
        [field: SerializeField, Min(0f)] public float WeaponRange { get; private set; } = 70f;

        [Header("Vision")]
        [Tooltip("Fog of war radius the squadron reveals for its team.")]
        [field: SerializeField, Min(0f)] public float VisionRange { get; private set; } = 175f;
    }
}
