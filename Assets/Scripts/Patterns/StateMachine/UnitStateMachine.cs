using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Services.ComponentHub;
using LightWeightFramework.Model;

namespace EmpireAtWar.Patterns.StateMachine
{
    public class UnitStateMachine : StateMachine 
    {
        public UnitStateMachine(IWeaponComponent weaponComponent, IComponentHub componentHub, IModel model)
        {
            WeaponComponent = weaponComponent;
            ComponentHub = componentHub;
            Model = model;
        }

        public IModel Model { get; }
        public IComponentHub ComponentHub { get; }
        public IWeaponComponent WeaponComponent { get; }
    }
}