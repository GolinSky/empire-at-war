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

namespace EmpireAtWar.ViewComponents.Health65
{
    public interface IShipUnitsProvider
    {
        bool HasUnits { get; }
        IShipUnitView[] GetShipUnits(ShipUnitType shipUnitType);
        IModelObserver ModelObserver { get; }
    }
    
    public class HealthViewComponent : ViewComponent<IHealthModelObserver>, IShipUnitsProvider
    {
        private static readonly Vector3 DefaultRotation = new Vector3(0, 180, 0);
        private const float TweenDuration = 0.1f;
        
        [SerializeField] private Canvas healthCanvas;
        [SerializeField] private Image shieldsFillImage;
        [SerializeField] private Image armorFillImage;

        [SerializeField] private List<ShipUnitView> shipUnitViewArray;
        
        private Sequence sequence;
        private float baseShieldsValue;
        private float baseArmorValue;
        
        public bool HasUnits => shipUnitViewArray.Any(x => !x.IsDestroyed);


        protected override void OnInit()
        {
            baseShieldsValue = Model.Shields;
            baseArmorValue = Model.Armor;
            Model.OnValueChanged += UpdateData;
            Model.OnDestroy += Destroy;
            UpdateShipUnit();
        }

        protected override void OnRelease()
        {
            Model.OnValueChanged -= UpdateData;
            Model.OnDestroy -= Destroy;
            healthCanvas.enabled = false;
        }
        
        private void Destroy()
        {
            View.Release();
        }

        private void UpdateShipUnit()
        {
            ShipUnitModel[] shipUnitModels = Model.ShipUnitModels.OrderBy(x=>x.Id).ToArray();
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
            sequence.Append(shieldsFillImage.DOFillAmount(Model.Shields / baseShieldsValue, TweenDuration));
            sequence.Append(armorFillImage.DOFillAmount(Model.Armor / baseArmorValue, TweenDuration));
        }


        public IShipUnitView[] GetShipUnits(ShipUnitType shipUnitType)
        {
            
            shipUnitViewArray = shipUnitViewArray.Where(x => !x.IsDestroyed).ToList();

            if (shipUnitViewArray.Count == 0)
            {
                return null;
            }
            if (shipUnitType == ShipUnitType.Any)
            {
                return shipUnitViewArray.ToArray();
            }
            else
            {
                return shipUnitViewArray.Where(x => x.ShipUnitType == shipUnitType).ToArray();
            }
        }
        
        public void Update()
        {
            healthCanvas.transform.rotation = Quaternion.Euler(DefaultRotation);
        }
    }
}