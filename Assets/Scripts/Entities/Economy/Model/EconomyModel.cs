using System;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Mvc;
using Zenject;

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
            : this(data, data.StartMoneyAmount)
        {
        }

        [Inject]
        public EconomyModel(EconomyData data, IGameModelObserver gameModel)
            : this(data, gameModel.StartingMoney)
        {
        }

        public EconomyModel(EconomyData data, float startingMoney)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (startingMoney <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(startingMoney));
            }

            _money = startingMoney;
        }

        public float Money => _money;

        public void AddMoney(float amount)
        {
            SetMoney(_money + amount);
        }

        public bool TrySpend(float amount)
        {
            if (_money < amount)
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
