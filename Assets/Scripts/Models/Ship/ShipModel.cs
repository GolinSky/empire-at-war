using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Movement;
using EmpireAtWar.Models.Weapon;
using LightWeightFramework.Model;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Models.Ship
{
    public interface IShipModelObserver : IModelObserver
    {
        Sprite ShipIcon { get; }
    }

    [CreateAssetMenu(fileName = "ShipModel", menuName = "Model/ShipModel")]
    public class ShipModel : Model, IShipModelObserver
    {
        [Header("Move Model")]
        [SerializeField] private MoveModel moveModel;
        
        [Header("Health Model")]
        [SerializeField] private HealthModel healthModel;

        [Header("Weapon Model")] 
        [SerializeField] private WeaponModel weaponModel;
        [field: SerializeField] public Sprite ShipIcon { get; private set; }
        
        protected override void Awake()
        {
            base.Awake();
            AddInnerModels(moveModel, healthModel, weaponModel);
        }

    }
}