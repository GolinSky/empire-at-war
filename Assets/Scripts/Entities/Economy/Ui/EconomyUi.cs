using System.Globalization;
using EmpireAtWar.Models.Economy;
using System;
using EmpireAtWar.Ui.Base;
using TMPro;
using UnityEngine;

namespace EmpireAtWar.Views.Economy
{
    public interface IEconomyUi
    {
        void SetModel(IEconomyModelObserver model);
        void Initialize();
        void Dispose();
    }

    public class EconomyUi : BaseUi, IEconomyUi
    {
        [SerializeField] private TextMeshProUGUI moneyText;

        private IEconomyModelObserver _model;
        private bool _isInitialized;

        public void SetModel(IEconomyModelObserver model)
        {
            _model = model;
        }

        public void Initialize()
        {
            if (_model == null)
            {
                throw new InvalidOperationException("Economy UI model must be set before initialization.");
            }

            if (moneyText == null)
            {
                throw new InvalidOperationException("Economy UI money text is not assigned.");
            }

            if (_isInitialized)
            {
                return;
            }

            UpdateMoneyText(_model.Money);
            _model.OnMoneyChanged += UpdateMoneyText;
            _isInitialized = true;
        }

        public void Dispose()
        {
            if (!_isInitialized)
            {
                return;
            }

            _model.OnMoneyChanged -= UpdateMoneyText;
            _isInitialized = false;
        }

        private void OnDestroy()
        {
            Dispose();
        }

        private void UpdateMoneyText(float money)
        {
            moneyText.text = money.ToString(CultureInfo.InvariantCulture);
        }
    }
}
