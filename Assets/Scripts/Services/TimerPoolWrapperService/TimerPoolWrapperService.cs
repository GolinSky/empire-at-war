using System;
using Utilities.ScriptUtils.Time;
using LightWeightFramework.Components.Service;
using Zenject;

namespace EmpireAtWar.Services.TimerPoolWrapperService
{
    public interface ITimerPoolWrapperService:IService
    {
        void Invoke(Action action, float delay);
    }
    public class TimerPoolWrapperService:Service, ITimerPoolWrapperService, ITickable
    {
        private readonly TimerPoolService timerPoolService;

        public TimerPoolWrapperService()
        {
            timerPoolService = new TimerPoolService();
        }
        
        public void Invoke(Action action, float delay)
        {
            timerPoolService.Invoke(action,  delay);
        }

        public void Tick()
        {
            timerPoolService.Update();
        }
    }
}