using Utilities.ScriptUtils.Time;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Services.Timing
{
    public sealed class TimerPoolTick : ITickable
    {
        private readonly TimerPoolService _timerPoolService;

        public TimerPoolTick(TimerPoolService timerPoolService)
        {
            _timerPoolService = timerPoolService;
        }

        public void Tick()
        {
            _timerPoolService.Update();
        }
    }
}
