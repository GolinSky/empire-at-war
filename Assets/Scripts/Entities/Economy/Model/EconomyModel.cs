using System;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Models.Economy
{
    public interface IEconomyModelObserver : IModelObserver
    {
        event Action<float> OnMoneyChanged;
        float Money { get; }
    }

    public class EconomyModel : PureModel, IEconomyModelObserver
    {
        public event Action<float> OnMoneyChanged;

        private float _money;

        public EconomyModel(EconomyData data)
        {
            _money = data.StartMoneyAmount;
        }

        public float Money => _money;

        public void AddMoney(float amount)
        {
            SetMoney(_money + amount);
        }

        public bool TrySpend(float amount)
        {
            if (_money <= amount)
            {
                return false;
            }

            SetMoney(_money - amount);
            return true;
        }

        private void SetMoney(float value)
        {
            _money = value;
            OnMoneyChanged?.Invoke(_money);
        }
    }
}
