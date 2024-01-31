using EmpireAtWar.Models.Movement;
using LightWeightFramework.Model;
using UnityEngine;

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
        [field: SerializeField] public Sprite ShipIcon { get; private set; }
        
        protected override void Awake()
        {
            base.Awake();
            AddInnerModel(moveModel);
        }
    }
}