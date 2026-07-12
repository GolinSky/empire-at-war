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

        [FormerlySerializedAs("shipShipMoveModel")]
        [FormerlySerializedAs("moveModel")]
        [Header("Move Model")]
        [SerializeField] private ShipMoveModel shipMoveModel;
        
        [Header("Health Model")]
        [SerializeField] private HealthModel healthModel;

        [FormerlySerializedAs("weaponModel")]
        [Header("Weapon Model")] 
        [SerializeField] private AttackModel attackModel;
        
        [Header("Radar Model")] 
        [SerializeField] private RadarModel radarModel;

        protected override void Awake()
        {
            base.Awake();
            AddInnerModels(shipMoveModel, healthModel, attackModel, radarModel);
        }
    }
}
