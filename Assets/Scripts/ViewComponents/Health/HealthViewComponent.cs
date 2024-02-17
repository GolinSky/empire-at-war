using DG.Tweening;
using EmpireAtWar.Models.Health;
using LightWeightFramework.Model;
using UnityEngine;
using UnityEngine.UI;
using WorkShop.LightWeightFramework.ViewComponents;

namespace EmpireAtWar.ViewComponents.Health65
{
    public class HealthViewComponent : ViewComponent
    {
        private const float TweenDuration = 0.1f;
        
        [SerializeField] private Canvas healthCanvas;
        [SerializeField] private Image shieldsFillImage;
        [SerializeField] private Image armorFillImage;

        private IHealthModelObserver healthModelObserver;
        private float baseShieldsValue;
        private float baseArmorValue;

        
        private Sequence sequence;

        public IModelObserver Model => ModelObserver;

        protected override void OnInit()
        {
            healthModelObserver = ModelObserver.GetModelObserver<IHealthModelObserver>();
            baseShieldsValue = healthModelObserver.Shields;
            baseArmorValue = healthModelObserver.Armor;
            healthModelObserver.OnValueChanged += UpdateData;
            healthModelObserver.OnDestroy += Destroy;
        }
        
        protected override void OnRelease()
        {
            healthModelObserver.OnValueChanged -= UpdateData;
            healthModelObserver.OnDestroy -= Destroy;
        }
        
        private void Destroy()
        {
            View.Release();
        }

        private void UpdateData()
        {
            if (sequence != null && sequence.IsActive())
            {
                sequence.Kill();
            }
            
            sequence = DOTween.Sequence();
            sequence.Append(shieldsFillImage.DOFillAmount(healthModelObserver.Shields / baseShieldsValue, TweenDuration));
            sequence.Append(armorFillImage.DOFillAmount(healthModelObserver.Armor / baseArmorValue, TweenDuration));
        }

        public void Update()
        {
            healthCanvas.transform.rotation = Quaternion.Euler(0, 180, 0);
        }
    }
}