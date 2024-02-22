using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Radar;
using EmpireAtWar.Services.ComponentHub;
using EmpireAtWar.ViewComponents.Health;
using LightWeightFramework.Model;
using UnityEngine;
using Zenject;
using Component = WorkShop.LightWeightFramework.Components.Component;

namespace EmpireAtWar.Components.Ship.AiComponent
{
    public class AiComponent : Component, IInitializable, ILateDisposable
    {
        private const ShipUnitType DefaultTargetType = ShipUnitType.Any;
        private readonly IMoveComponent moveComponent;
        private readonly IWeaponComponent weaponComponent;
        private readonly IComponentHub componentHub;

        private IHealthModelObserver healthModelObserver;
        private IRadarModelObserver radarModelObserver;

        //todo: radar component
        public AiComponent(IModel model, IMoveComponent moveComponent, IWeaponComponent weaponComponent, IComponentHub componentHub)
        {
            this.moveComponent = moveComponent;
            this.weaponComponent = weaponComponent;
            this.componentHub = componentHub;
            healthModelObserver = model.GetModelObserver<IHealthModelObserver>();
            radarModelObserver = model.GetModelObserver<IRadarModelObserver>();
        }

        public void Initialize()
        {
            radarModelObserver.OnHitDetected += HandleEnemy;
        }

        public void LateDispose()
        {
            radarModelObserver.OnHitDetected -= HandleEnemy;
        }

        private void HandleEnemy(RaycastHit[] raycastHit)
        {
            List<AttackData> healthComponents = new List<AttackData>();
            foreach (RaycastHit hit in raycastHit)
            {
                IShipUnitsProvider unitsProvider = hit.collider.GetComponentInChildren<IShipUnitsProvider>();
                if (unitsProvider != null && unitsProvider.HasUnits)
                {
                    healthComponents.Add(new AttackData(unitsProvider, componentHub.GetComponent(unitsProvider.ModelObserver), DefaultTargetType));
                }
            }

            if (healthComponents.Count != 0)
            {
                weaponComponent.AddTarget(healthComponents.ToArray());
            }
        }
    }
}