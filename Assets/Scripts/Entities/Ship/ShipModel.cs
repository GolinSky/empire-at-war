using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

namespace EmpireAtWar.Ship
{
    public interface IShipModelObserver : IUnitModelObserver
    {
        ShipType ShipType { get; }
    }

    [CreateAssetMenu(fileName = "ShipModel", menuName = "Model/ShipModel")]
    public class ShipModel : Model, IShipModelObserver
    {
        [Inject] public ShipType ShipType { get; }

        [field: FormerlySerializedAs("shipShipMoveModel")]
        [field: FormerlySerializedAs("moveModel")]
        [field: FormerlySerializedAs("shipMoveModel")]
        [Header("Move Model")]
        [field: SerializeField] public ShipMoveModel ShipMoveModel { get; private set; }
        
        [Header("Health Model")]
        [field: FormerlySerializedAs("healthModel")]
        [field: SerializeField] public HealthModel HealthModel { get; private set; }
        IHealthModelObserver IUnitModelObserver.HealthModel => HealthModel;

        [field: FormerlySerializedAs("weaponModel")]
        [field: FormerlySerializedAs("attackModel")]
        [Header("Weapon Model")] 
        [field: SerializeField] public AttackModel AttackModel { get; private set; }
        
        [Header("Radar Model")]
        [field: FormerlySerializedAs("radarModel")]
        [field: SerializeField] public RadarModel RadarModel { get; private set; }
    }
}
