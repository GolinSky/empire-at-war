using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Ship.WeaponComponent;
using LightWeightFramework.Model;

namespace EmpireAtWar.Patterns.StateMachine
{
    public class ShipStateMachine : UnitStateMachine
    {
        public ShipStateMachine(
            IShipMoveComponent shipMoveComponent,
            IWeaponComponent weaponComponent,
            IModel model) 
            : base(weaponComponent, model)
        {
            ShipMoveComponent = shipMoveComponent;
        }
        
        public IShipMoveComponent ShipMoveComponent { get; }
    }
}