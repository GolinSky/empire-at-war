using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Patterns.StateMachine;
using EmpireAtWar.Services.ComponentHub;
using LightWeightFramework.Components.Components;
using LightWeightFramework.Model;
using Zenject;

namespace EmpireAtWar.Components.Ship.AiComponent
{
    public class EnemyStateComponent: Component, IInitializable, ILateDisposable, ITickable
    {
        private readonly ShipStateMachine shipStateMachine;

        private readonly ShipIdleState shipIdleState;

        public EnemyStateComponent(IModel model,
            IShipMoveComponent shipMoveComponent,
            IWeaponComponent weaponComponent,
            IComponentHub componentHub)
        {
            shipStateMachine = new ShipStateMachine(
                shipMoveComponent, 
                weaponComponent,
                componentHub,
                model);
            
            shipIdleState = new ShipIdleState(shipStateMachine);
            shipStateMachine.SetDefaultState(shipIdleState);
            shipStateMachine.ChangeState(shipIdleState);
        }
        public void Initialize()
        {
            
        }

        public void LateDispose()
        {
            
        }

        public void Tick()
        {
            shipStateMachine.Update();
        }
    }
}