using EmpireAtWar.Components.Ui.Tooltip;
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
        void Initialize();

        void Dispose();

        void SetModel(IEconomyModelObserver model);

        void SetParent(Transform parent);

        void Show();

        void Hide();
    }

    public class EconomyUi : BaseUi, IEconomyUi, ITooltipHoverView
    {
        private IEconomyModelObserver _model;

        [SerializeField] private TextMeshProUGUI moneyText;
        [SerializeField] private TooltipHoverView tooltipHover;

        private bool _isInitialized;

        public TooltipHoverView TooltipHover => tooltipHover;

        public void Initialize()
        {
            if (_model == null)
            {
                throw new InvalidOperationException("Economy UI model must be set before initialization.");
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

        public void SetModel(IEconomyModelObserver model)
        {
            _model = model;
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
