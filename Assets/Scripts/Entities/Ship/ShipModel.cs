using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Movement;
using LightWeightFramework.Model;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

namespace EmpireAtWar.Ship
{
    public interface IShipModelObserver : IUnitModelObserver
    {
        ParticleSystem DeathExplosionVfx { get; }
        ShipType ShipType { get; }
    }

    [CreateAssetMenu(fileName = "ShipModel", menuName = "Model/ShipModel")]
    public class ShipModel : Model, IShipModelObserver
    {
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

        [field:SerializeField] public ParticleSystem DeathExplosionVfx { get; private set; }
        
        [field:SerializeField] public float MinMoveCoefficient { get; private set; }

        
        [Inject]
        public ShipType ShipType { get; }

        public ShipMoveModel ShipMoveModel => shipMoveModel;

        public HealthModel HealthModel => healthModel;

        public AttackModel AttackModel => attackModel;

        public RadarModel RadarModel => radarModel;


        protected override void Awake()
        {
            base.Awake();
            AddInnerModels(shipMoveModel, healthModel, attackModel, radarModel);
        }
    }
}