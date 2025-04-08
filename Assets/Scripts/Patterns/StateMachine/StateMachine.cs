using UnityEngine;

namespace EmpireAtWar.Patterns.StateMachine
{
    public abstract class StateMachine
    {
        private BaseState _defaultState;
        public BaseState CurrentState { get; private set; }

        public void ChangeState(BaseState baseState)
        {
            CurrentState?.Exit();
            CurrentState = baseState;
            CurrentState.Enter();
        }
        
        public void SetDefaultState(BaseState defaultState)
        {
            _defaultState = defaultState;
        }

        public void ChangeToDefaultState()
        {
            Debug.Log("STATE:ChangeToDefaultState");
            if (_defaultState != null)
            {
                ChangeState(_defaultState);
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