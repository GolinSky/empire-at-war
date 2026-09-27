namespace EmpireAtWar.Patterns.StateMachine
{
    public interface IBaseState
    {
        bool IsComplete { get; }
        void Enter();
        void Tick(float deltaTime);
        void Exit();
    }
}
