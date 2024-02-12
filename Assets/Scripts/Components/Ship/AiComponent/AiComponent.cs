using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Radar;
using EmpireAtWar.Services.ComponentHub;
using EmpireAtWar.ViewComponents.Health65;
using LightWeightFramework.Model;
using UnityEngine;
using Zenject;
using Component = WorkShop.LightWeightFramework.Components.Component;

namespace EmpireAtWar.Components.Ship.AiComponent
{
    public class AiComponent : Component, IInitializable, ILateDisposable
    {
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

        private void HandleEnemy(RaycastHit raycastHit)
        {
            HealthViewComponent healthViewComponent = raycastHit.collider.GetComponentInChildren<HealthViewComponent>();
            IHealthComponent healthComponent = componentHub.GetComponent(healthViewComponent.Model);
            weaponComponent.AddTarget(healthComponent, raycastHit.transform);
        }
    }
}