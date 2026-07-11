using UnityEngine;

namespace EmpireAtWar.Patterns.StateMachine
{
    public abstract class StateMachine
    {
        private IBaseState _defaultState;
        public IBaseState CurrentState { get; private set; }

        public void ChangeState(IBaseState baseState)
        {
            CurrentState?.Exit();
            CurrentState = baseState;
            CurrentState.Enter();
        }
        
        public void SetDefaultState(IBaseState defaultState)
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