namespace EmpireAtWar.Patterns.StateMachine
{
    public interface IBaseState
    {
        void Enter();
        void Update();
        void Exit();
    }
}
