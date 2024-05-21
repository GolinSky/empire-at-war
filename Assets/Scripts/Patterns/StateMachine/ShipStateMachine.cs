using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Services.ComponentHub;
using LightWeightFramework.Model;

namespace EmpireAtWar.Patterns.StateMachine
{
    public class ShipStateMachine : UnitStateMachine
    {
        public ShipStateMachine(
            IShipMoveComponent shipMoveComponent,
            IWeaponComponent weaponComponent,
            IComponentHub componentHub,
            IModel model) 
            : base(weaponComponent, componentHub, model)
        {
            ShipMoveComponent = shipMoveComponent;
        }
        
        public IShipMoveComponent ShipMoveComponent { get; }
    }
}