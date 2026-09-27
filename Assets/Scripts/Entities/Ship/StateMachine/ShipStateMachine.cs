using EmpireAtWar.Patterns.StateMachine;

namespace EmpireAtWar.Entities.Ship.StateMachine
{
    public sealed class ShipStateMachine
    {
        public IBaseState CurrentState { get; private set; }

        public void SetState(IBaseState state)
        {
            CurrentState?.Exit();
            CurrentState = state;
            CurrentState.Enter();
        }

        public void Tick(float deltaTime) => CurrentState.Tick(deltaTime);
    }
}
