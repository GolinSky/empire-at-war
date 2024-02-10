using System;
using EmpireAtWar.Components.Ship.Health;
using WorkShop.LightWeightFramework.Service;

namespace EmpireAtWar.Services.Battle
{
    public interface IBattleService : IService
    {
        event Action<IHealthComponent> OnTargetAdded;
        void AddTarget(IHealthComponent healthComponent);
    }

    public class BattleService : Service, IBattleService
    {
        public event Action<IHealthComponent> OnTargetAdded;

        public void AddTarget(IHealthComponent healthComponent)
        {
            if(healthComponent.Destroyed) return;
            
            OnTargetAdded?.Invoke(healthComponent);
        }
    }
}