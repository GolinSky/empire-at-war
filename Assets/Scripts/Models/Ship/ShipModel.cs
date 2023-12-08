using System;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.Ship
{
    public interface IShipModelObserver:IModelObserver
    {
        event Action<Vector3> OnTargetPositionChanged;
        event Action<bool> OnSelected;
        event Action<Vector3> OnHyperSpaceJump;
        Vector3 Position { get; }
        Vector3 StartPosition { get; }
        float Speed { get; }
        float HyperSpaceSpeed { get; }
        Vector3 HyperSpacePosition { get; }
        
    }
    
    [CreateAssetMenu(fileName = "ShipModel", menuName = "Model/ShipModel")]
    public class ShipModel : Model, IShipModelObserver
    {
        public event Action<Vector3> OnTargetPositionChanged;
        public event Action<Vector3> OnHyperSpaceJump;
        public event Action<bool> OnSelected;

        [field:SerializeField] public Vector3 StartPosition { get; private set; }
        [field:SerializeField] public float Speed { get; private set; }
        [field:SerializeField]  public float HyperSpaceSpeed { get; private set; }

        public Vector3 HyperSpacePosition
        {
            get => hyperSpacePosition;
            set
            {
                hyperSpacePosition = value;
                OnHyperSpaceJump?.Invoke(hyperSpacePosition);
            } 
        }

        private Vector3 position;
        private Vector3 hyperSpacePosition;


        public Vector3 Position
        {
            get => position;
            set
            {
                position = value;
                OnTargetPositionChanged?.Invoke(position);
            } 
        }

        public bool IsSelected
        {
            set => OnSelected?.Invoke(value);
        }

    }
}