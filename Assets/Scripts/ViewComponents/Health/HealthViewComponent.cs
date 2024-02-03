using EmpireAtWar.Models.Health;
using UnityEngine;
using UnityEngine.UI;
using WorkShop.LightWeightFramework.ViewComponents;

namespace EmpireAtWar.ViewComponents.Health65
{
    public class HealthViewComponent : ViewComponent
    {
        [SerializeField] private Canvas healthCanvas;
        [SerializeField] private Image shieldsFillImage;
        [SerializeField] private Image armorFillImage;

        private IHealthModelObserver healthModelObserver;
        private float baseShieldsValue;
        private float baseArmorValue;

        protected override void OnInit()
        {
            healthModelObserver = ModelObserver.GetModelObserver<IHealthModelObserver>();
            baseShieldsValue = healthModelObserver.Shields;
            baseArmorValue = healthModelObserver.Armor;
            healthModelObserver.OnValueChanged += UpdateData;
        }

        protected override void OnRelease()
        {
            healthModelObserver.OnValueChanged -= UpdateData;
        }

        private void UpdateData()
        {
            shieldsFillImage.fillAmount = healthModelObserver.Shields / baseShieldsValue;
            armorFillImage.fillAmount = healthModelObserver.Armor / baseArmorValue;
        }

        public void Update()
        {
            healthCanvas.transform.rotation = Quaternion.Euler(0, 180, 0);
        }
    }
}