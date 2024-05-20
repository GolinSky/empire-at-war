using UnityEngine;

namespace EmpireAtWar.Patterns.StateMachine
{
    public abstract class StateMachine
    {
        private BaseState defaultState;
        public BaseState CurrentState { get; private set; }

        public void ChangeState(BaseState baseState)
        {
            CurrentState?.Exit();
            CurrentState = baseState;
            CurrentState.Enter();
        }
        
        public void SetDefaultState(BaseState defaultState)
        {
            this.defaultState = defaultState;
        }

        public void ChangeToDefaultState()
        {
            Debug.Log("STATE:ChangeToDefaultState");
            if (defaultState != null)
            {
                ChangeState(defaultState);
            }
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