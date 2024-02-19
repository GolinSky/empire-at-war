using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Health;
using EmpireAtWar.ViewComponents.Health;
using LightWeightFramework.Model;
using UnityEngine;
using UnityEngine.UI;
using WorkShop.LightWeightFramework.ViewComponents;
using Random = System.Random;

namespace EmpireAtWar.ViewComponents.Health65
{
    public class HealthViewComponent : ViewComponent
    {
        private static readonly Vector3 DefaultRotation = new Vector3(0, 180, 0);
        private const float TweenDuration = 0.1f;
        
        [SerializeField] private Canvas healthCanvas;
        [SerializeField] private Image shieldsFillImage;
        [SerializeField] private Image armorFillImage;

        [SerializeField] private List<ShipUnitView> shipUnitViewArray;
        
        private IHealthModelObserver healthModelObserver;
        private Sequence sequence;
        private Random random;
        private float baseShieldsValue;
        private float baseArmorValue;

        public IModelObserver Model => ModelObserver;

        protected override void OnInit()
        {
            
            healthModelObserver = ModelObserver.GetModelObserver<IHealthModelObserver>();
            baseShieldsValue = healthModelObserver.Shields;
            baseArmorValue = healthModelObserver.Armor;
            random = new Random();
            healthModelObserver.OnValueChanged += UpdateData;
            healthModelObserver.OnDestroy += Destroy;
            UpdateShipUnit();
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

        private void UpdateShipUnit()
        {
            ShipUnitModel[] shipUnitModels = healthModelObserver.ShipUnitModels.OrderBy(x=>x.Id).ToArray();
            ShipUnitView[] shipUnitViews = shipUnitViewArray.OrderBy(x => x.Id).ToArray();
            for (var i = 0; i < shipUnitModels.Length; i++)
            {
                shipUnitViews[i].Init(shipUnitModels[i]);
            }
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

        public IShipUnitView GetShipUnit(ShipUnitType shipUnitType)
        {
            shipUnitViewArray = shipUnitViewArray.Where(x => !x.IsDestroyed).ToList();

            if (shipUnitViewArray.Count == 0)
            {
                return null;
            }
            if (shipUnitType == ShipUnitType.Any)
            {
                return shipUnitViewArray[random.Next(shipUnitViewArray.Count)];
            }
            else
            {
                foreach (ShipUnitView unitView in shipUnitViewArray)
                {
                    if (unitView.ShipUnitType == shipUnitType)
                    {
                        return unitView;
                    }
                }
                return shipUnitViewArray[random.Next(shipUnitViewArray.Count)];
            }
        }

        public void Update()
        {
            healthCanvas.transform.rotation = Quaternion.Euler(DefaultRotation);
        }
    }
}