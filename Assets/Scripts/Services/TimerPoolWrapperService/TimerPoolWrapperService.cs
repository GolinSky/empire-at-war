using System;
using System.Collections.Generic;
using Utilities.ScriptUtils.Time;
using LightWeightFramework.Components.Service;
using Zenject;

namespace EmpireAtWar.Services.TimerPoolWrapperService
{
    public interface ITimerPoolWrapperService:IService
    {
        CustomCoroutine Invoke(Action action, float delay);
    }
    
    public class TimerPoolWrapperService: Service, ITimerPoolWrapperService, ITickable
    {
        private readonly TimerPoolService _timerPoolService;

        public TimerPoolWrapperService()
        {
            _timerPoolService = new TimerPoolService();
        }
        public CustomCoroutine Invoke(Action action, float delay)
        {
            return _timerPoolService.Invoke(action, delay);
        }
        

        public void Tick()
        {
            _timerPoolService.Update();
        }
    }
}