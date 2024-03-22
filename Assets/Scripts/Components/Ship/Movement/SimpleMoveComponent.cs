using EmpireAtWar.Models.Movement;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Selection
{
    public class SimpleMoveComponent: BaseComponent<SimpleMoveModel>, ISimpleMoveComponent
    {
        public bool CanMove => Model.CanMove;

        public SimpleMoveComponent(IModel model, Vector3 startPosition) : base(model)
        {
            Model.TargetPosition = startPosition;
        }
        
        public void MoveToPosition(Vector2 screenPosition) {}
        
        
    }
}