using EmpireAtWar.Components.FogOfWar;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Audio;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Squadrons.Flight;
using EmpireAtWar.Components.Squadrons.Health;
using EmpireAtWar.Components.Squadrons.Icon;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.ViewComponents.Squadrons;
using UnityEngine;

namespace EmpireAtWar.Entities.BaseEntity
{
    public sealed class EntityBindings : MonoBehaviour
    {
        [field: SerializeField] public SelectionComponent[] Selection { get; private set; }
        [field: SerializeField] public HealthComponent[] Health { get; private set; }
        [field: SerializeField] public RadarComponent[] Radar { get; private set; }
        [field: SerializeField] public WeaponComponent[] Weapons { get; private set; }
        [field: SerializeField] public FogVisibilityComponent[] FogVisibility { get; private set; }
        [field: SerializeField] public ShipMoveComponent ShipMove { get; private set; }
        [field: SerializeField] public HangarComponent Hangar { get; private set; }
        [field: SerializeField] public AudioShipComponent AudioShip { get; private set; }
        [field: SerializeField] public AudioDialogShipComponent AudioDialog { get; private set; }
        [field: SerializeField] public SquadronHealthComponent SquadronHealth { get; private set; }
        [field: SerializeField] public SquadronFlightComponent SquadronFlight { get; private set; }
        [field: SerializeField] public SquadronIconComponent SquadronIcon { get; private set; }
        [field: SerializeField] public SFoilsView SFoils { get; private set; }
        [field: SerializeField] public MonoBehaviour[] Injectables { get; private set; }
    }
}
