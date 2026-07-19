using EmpireAtWar.Patterns.StateMachine;

namespace EmpireAtWar.Entities.Ship.StateMachine
{
    public class StateMachine1
    {
        public IBaseState CurrentState { get; private set; }

        public void SetState(IBaseState baseState)
        {
            CurrentState?.Exit();
            CurrentState = baseState;
            CurrentState.Enter();
        }

        public void ExitState()
        {
            CurrentState?.Exit();
            CurrentState = null;
        }
   

        public void Update()
        {
            if (CurrentState != null)
            {
                CurrentState.Update();
            }
        }
    }
}