using System;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Mvc;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Models.Economy
{
    public interface IEconomyModelObserver : IModelObserver
    {
        event Action<float> OnMoneyChanged;

        float Money { get; }
    }

    public class EconomyModel : Model, IEconomyModelObserver
    {
        private float _money;

        public event Action<float> OnMoneyChanged;

        public float Money => _money;
        /// <summary>Seconds until the next income payment; zero pays on the next running tick.</summary>
        public float IncomeTimeLeft { get; private set; }

        public EconomyModel(EconomyData data)
            : this(data, data.StartMoneyAmount)
        {
        }

        [Inject]
        public EconomyModel(IGameModelObserver gameModel, EconomyData data)
            : this(data, gameModel.StartingMoney)
        {
        }

        public EconomyModel(EconomyData data, float startingMoney)
        {

            if (startingMoney <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(startingMoney));
            }

            _money = startingMoney;
        }

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

        /// <summary>Counts the income delay down and returns true when a payment is due, starting the next delay.</summary>
        public bool AdvanceIncomeTimer(float deltaTime, float incomeDelay)
        {
            IncomeTimeLeft -= deltaTime;
            if (IncomeTimeLeft > 0f)
            {
                return false;
            }

            IncomeTimeLeft = incomeDelay;
            return true;
        }

        public void Restore(float money, float incomeTimeLeft)
        {
            IncomeTimeLeft = incomeTimeLeft;
            SetMoney(money);
        }

        private void SetMoney(float value)
        {
            _money = value;
            OnMoneyChanged?.Invoke(_money);
        }
    }
}
