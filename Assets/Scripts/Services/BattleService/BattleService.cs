using System;
using LightWeightFramework.Model;
using WorkShop.LightWeightFramework.Service;

namespace EmpireAtWar.Services.BattleService
{
    public interface IBattleService : IService
    {
        event Action<IModel> OnTargetAdded; 
        void NotifyAttack(IModel model);
    }

    public class BattleService : Service, IBattleService
    {
        public event Action<IModel> OnTargetAdded;

        public void NotifyAttack(IModel model)
        {
            OnTargetAdded?.Invoke(model);
        }
    }
}