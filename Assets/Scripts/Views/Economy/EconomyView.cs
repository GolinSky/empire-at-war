using System.Globalization;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Views.ViewImpl;
using TMPro;
using UnityEngine;

namespace EmpireAtWar.Views.Economy
{
    public class EconomyView : View<IEconomyModelObserver>
    {
        [SerializeField] private TextMeshProUGUI moneyText;

        protected override void OnInitialize()
        {
            Model.OnMoneyChanged += UpdateMoneyText;
        }

        protected override void OnDispose()
        {
            Model.OnMoneyChanged -= UpdateMoneyText;
        }
        
        private void UpdateMoneyText(float money)
        {
            moneyText.text = money.ToString(CultureInfo.InvariantCulture);
        }
    }
}