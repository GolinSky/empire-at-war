using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Views.ViewImpl;
using LightWeightFramework.Model;
using UnityEngine;
using UnityEngine.UI;
using LightWeightFramework.Components.ViewComponents;
using UnityEngine.Serialization;
using Utilities.ScriptUtils.Dotween;
using Utilities.ScriptUtils.EditorSerialization;
using Zenject;

namespace EmpireAtWar.ViewComponents.Health
{
    public interface IHardPointsProvider
    {
        bool HasUnits { get; }
        IHardPointView[] GetShipUnits(HardPointType hardPointType);
        IModelObserver ModelObserver { get; }
        PlayerType PlayerType { get; }
        Transform Transform { get; }
    }

    public class HealthViewComponent : ViewComponent<IHealthModelObserver>, IHardPointsProvider, ITickable
    {
        private static readonly Vector3 DefaultRotation = new(0, 180, 0);
        private const float TWEEN_DURATION = 0.1f;
        
        [SerializeField] private Canvas healthCanvas;
        [SerializeField] private Image shieldsFillImage;
        [SerializeField] private Image armorFillImage;
        [SerializeField] private ShieldView shieldView;
        
        [SerializeField] private HealthModelDependency healthModelDependency; 
        [SerializeField] private DictionaryWrapper<PlayerType, Color> shieldColors;
        [SerializeField] private DictionaryWrapper<PlayerType, Color> hullColors;
        
        private Coroutine shieldsAnimatedCoroutine;
        private Sequence sequence;
        private Sequence shieldSequence;
        private float baseShieldsValue;
        private float baseArmorValue;
        private List<HardPointView> currentShipUnits;
        
        [Inject]
        public PlayerType PlayerType { get; }

        public Transform Transform => View.Transform;

        public bool HasUnits => ShipUnits.Any(x => !x.IsDestroyed);
        
        private List<HardPointView> ShipUnits => healthModelDependency.ShipUnits;


        protected override void OnInit()
        {
            baseShieldsValue = Model.Shields;
            baseArmorValue = Model.Armor;
            Model.OnValueChanged += UpdateData;
            Model.OnDestroy += Destroy;
            shieldsFillImage.color = shieldColors.Dictionary[PlayerType];
            armorFillImage.color = hullColors.Dictionary[PlayerType];
        }

        private void Start()
        {
            if (shieldView != null)
            {
                shieldsAnimatedCoroutine = StartCoroutine(AnimateShields()); 
            }
        }
        
        protected override void OnRelease()
        {
            Model.OnValueChanged -= UpdateData;
            Model.OnDestroy -= Destroy;
            healthCanvas.enabled = false;
        }
        
        private void Destroy()
        {
            //invoke model that is completely destroyed - use command
            if (View is View view)
            {
                view.Release();
            }
            
            if (shieldsAnimatedCoroutine != null)
            {
                StopCoroutine(shieldsAnimatedCoroutine);
            }
        }
        
        private IEnumerator AnimateShields()
        {
            while (!Model.IsDestroyed)
            {
                if (Model.IsLostShieldGenerator)
                {
                    break;
                }
                
                if (shieldView.IsVisibleToCamera)
                {
                    if (Model.Shields > 0)
                    {
                        shieldView.AnimateTextureOffset();
                    }
                }

                yield return new WaitForEndOfFrame();
            }
        }

        
        private void UpdateData()
        {
            if (shieldView != null)
            {
                shieldView.SetActive(Model.Shields>0f);
            }

            sequence.KillIfExist();
            sequence = DOTween.Sequence();
            sequence.Append(shieldsFillImage.DOFillAmount(Model.Shields / baseShieldsValue, TWEEN_DURATION));
            sequence.Append(armorFillImage.DOFillAmount(Model.Armor / baseArmorValue, TWEEN_DURATION));
        }

        public IHardPointView[] GetShipUnits(HardPointType hardPointType)
        {
            currentShipUnits = ShipUnits.Where(x => !x.IsDestroyed).ToList();

            if (currentShipUnits.Count == 0)
            {
                return null;
            }
            if (hardPointType == HardPointType.Any)
            {
                return currentShipUnits.ToArray();
            }
            else
            {
                if (currentShipUnits.Any(x => x.HardPointType == hardPointType))
                {
                    return currentShipUnits.Where(x => x.HardPointType == hardPointType).ToArray();
                }
                else
                {
                    return currentShipUnits.ToArray();
                }
            }
        }


        public void Tick()
        {
            healthCanvas.transform.rotation = Quaternion.Euler(DefaultRotation);
        }

      
    }
}