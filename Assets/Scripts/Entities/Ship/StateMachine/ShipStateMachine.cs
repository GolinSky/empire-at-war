using EmpireAtWar.Patterns.StateMachine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using EmpireAtWar.Services.Timing;
using Unity.Profiling;
#endif

namespace EmpireAtWar.Entities.Ship.StateMachine
{
    public sealed class ShipStateMachine
    {
        public IBaseState CurrentState { get; private set; }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private ProfilerMarker _stateMarker;
#endif

        public void SetState(IBaseState state)
        {
            CurrentState?.Exit();
            CurrentState = state;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _stateMarker = BattleProfilerMarkers.ForShipState(state.GetType());
#endif
            CurrentState.Enter();
        }

        public void Tick(float deltaTime)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (_stateMarker.Auto())
#endif
            {
                CurrentState.Tick(deltaTime);
            }
        }
    }
}
