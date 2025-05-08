using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Services.ComponentHub;
using LightWeightFramework.Model;

namespace EmpireAtWar.Patterns.StateMachine
{
    public class UnitStateMachine : StateMachine 
    {
        public UnitStateMachine(IWeaponComponent weaponComponent, IModel model)
        {
            WeaponComponent = weaponComponent;
            Model = model;
        }

        public IModel Model { get; }
        public IWeaponComponent WeaponComponent { get; }
    }
}