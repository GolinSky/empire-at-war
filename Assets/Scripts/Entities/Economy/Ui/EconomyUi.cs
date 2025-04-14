using System.Globalization;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Ui.Base;
using TMPro;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Views.Economy
{
    public class EconomyUi : BaseUi<IEconomyModelObserver>, IInitializable, ILateDisposable
    {
        [SerializeField] private TextMeshProUGUI moneyText;
        
        public void Initialize()
        {
            Model.OnMoneyChanged += UpdateMoneyText;
        }

        public void LateDispose()
        {
            Model.OnMoneyChanged -= UpdateMoneyText;
        }
        
        private void UpdateMoneyText(float money)
        {
            moneyText.text = money.ToString(CultureInfo.InvariantCulture);
        }
    }
}