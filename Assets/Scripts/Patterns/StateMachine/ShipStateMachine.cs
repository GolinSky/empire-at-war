using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Services.ComponentHub;
using LightWeightFramework.Model;

namespace EmpireAtWar.Patterns.StateMachine
{
    public class ShipStateMachine : StateMachine
    {
        public ShipStateMachine(IShipMoveComponent shipMoveComponent, IWeaponComponent weaponComponent, IComponentHub componentHub, IModel model)
        {
            ShipMoveComponent = shipMoveComponent;
            WeaponComponent = weaponComponent;
            ComponentHub = componentHub;
            Model = model;
        }

        public IModel Model { get; }
        public IComponentHub ComponentHub { get; }
        public IWeaponComponent WeaponComponent { get; }
        public IShipMoveComponent ShipMoveComponent { get; }
    }
}