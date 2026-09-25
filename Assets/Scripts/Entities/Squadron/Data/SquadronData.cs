using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Squadrons.Flight;
using EmpireAtWar.Components.Squadrons.Health;
using EmpireAtWar.Mvc;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.Squadrons.Data
{
    [CreateAssetMenu(fileName = "SquadronData", menuName = "Data/SquadronData")]
    public class SquadronData : Mvc.Data, IModel, ISquadronModelObserver, IFighterFlightData,
        ISquadronHealthData, IRadarData
    {
        [Inject] public SquadronType SquadronType { get; private set; }

        [Header("Orders")]
        [Tooltip("Radius used when compacting group move orders.")]
        [field: SerializeField] public float NavigationRadius { get; private set; } = 6f;
        [Tooltip("Enemies closer than this to the guarded unit or loiter point are engaged automatically.")]
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

        [Header("Radar")]
        [field: SerializeField] public float Range { get; private set; } = 80f;
        [field: SerializeField] public float Delay { get; private set; } = 0.25f;
        [field: SerializeField] public float Distance { get; private set; } = 200f;
    }
}
