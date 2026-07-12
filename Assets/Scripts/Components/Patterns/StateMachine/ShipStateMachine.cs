using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Models.Health;

namespace EmpireAtWar.Patterns.StateMachine
{
    public class ShipStateMachine : UnitStateMachine
    {
        public ShipStateMachine(
            IShipMoveComponent shipMoveComponent,
            IAttackComponent attackComponent,
            IRadarModelObserver radarModel,
            IHealthModelObserver healthModel,
            IShipMoveModelObserver moveModel,
            IAttackModelObserver attackModel)
            : base(attackComponent, radarModel, healthModel)
        {
            ShipMoveComponent = shipMoveComponent;
            MoveModel = moveModel;
            AttackModel = attackModel;
        }
        
        public IShipMoveComponent ShipMoveComponent { get; }
        public IShipMoveModelObserver MoveModel { get; }
        public IAttackModelObserver AttackModel { get; }
    }
}
