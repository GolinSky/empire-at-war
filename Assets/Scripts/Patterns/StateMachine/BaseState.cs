using UnityEngine;

namespace EmpireAtWar.Patterns.StateMachine
{
    public abstract class BaseState
    {
        public virtual StateMachine StateMachine { get; }

        protected BaseState(StateMachine stateMachine)
        {
            StateMachine = stateMachine;
        }


        public virtual void Enter()
        {
            Debug.Log($"STATE:{GetType().Name} Enter state");
        }
        public virtual void Update() {}

        public virtual void Exit()
        {
            Debug.Log($"STATE:{GetType().Name} Exit state");
        }
    }
}