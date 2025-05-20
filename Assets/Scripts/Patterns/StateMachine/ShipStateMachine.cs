using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Selection;
using LightWeightFramework.Model;

namespace EmpireAtWar.Patterns.StateMachine
{
    public class ShipStateMachine : UnitStateMachine
    {
        public ShipStateMachine(
            IShipMoveComponent shipMoveComponent,
            IAttackComponent attackComponent,
            IModel model) 
            : base(attackComponent, model)
        {
            ShipMoveComponent = shipMoveComponent;
        }
        
        public IShipMoveComponent ShipMoveComponent { get; }
    }
}