using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Map;
using EmpireAtWar.Patterns.StateMachine;
using EmpireAtWar.Services.ComponentHub;
using LightWeightFramework.Components.Components;
using LightWeightFramework.Model;
using Zenject;

namespace EmpireAtWar.Components.Ship.AiComponent
{
    public class EnemyStateComponent: Component, IInitializable, ILateDisposable, ITickable
    {
        private readonly IMapModelObserver mapModelObserver;
        private readonly ShipStateMachine shipStateMachine;

        private readonly ShipIdleState shipIdleState;
        private readonly ShipLockMainTargetState shipLockMainTargetState;
        private readonly MoveToPointState moveToPointState;

        public EnemyStateComponent(
            IModel model,
            IShipMoveComponent shipMoveComponent,
            IWeaponComponent weaponComponent,
            IComponentHub componentHub,
            IMapModelObserver mapModelObserver)
        {
            this.mapModelObserver = mapModelObserver;
            shipStateMachine = new ShipStateMachine(
                shipMoveComponent, 
                weaponComponent,
                componentHub,
                model);
            
            shipIdleState = new ShipIdleState(shipStateMachine);
            shipLockMainTargetState = new ShipLockMainTargetState(shipStateMachine);
            moveToPointState = new MoveToPointState(shipStateMachine);

            shipStateMachine.SetDefaultState(shipIdleState);
            shipStateMachine.ChangeState(shipIdleState);
        }
        public void Initialize()
        {
            moveToPointState.SetWorldCoordinates(mapModelObserver.GetStationPosition(PlayerType.Player));
            shipStateMachine.ChangeState(moveToPointState);
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