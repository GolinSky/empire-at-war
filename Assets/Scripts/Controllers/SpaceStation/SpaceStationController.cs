using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Radar;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Models.SpaceStation;
using EmpireAtWar.Models.Weapon;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.ComponentHub;
using EmpireAtWar.ViewComponents.Health;
using LightWeightFramework.Controller;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Controllers.SpaceStation
{
    public class SpaceStationController : Controller<SpaceStationModel>, IInitializable, ILateDisposable
    {
        private const ShipUnitType DefaultTargetType = ShipUnitType.Any;
        private readonly IWeaponComponent weaponComponent;
        private readonly IComponentHub componentHub;
        private readonly ISelectionService selectionService;
        private readonly ISelectionModelObserver selectionModelObserver;
        private readonly IWeaponModelObserver weaponModelObserver;
        
        private IRadarModelObserver radarModelObserver;
        private IShipUnitsProvider mainTarget;
        public SpaceStationController(
            SpaceStationModel model,
            IWeaponComponent weaponComponent,
            IComponentHub componentHub,
            ISelectionService selectionService) : base(model)
        {
            this.weaponComponent = weaponComponent;
            this.componentHub = componentHub;
            this.selectionService = selectionService;
            selectionModelObserver = Model.GetModelObserver<ISelectionModelObserver>();
            weaponModelObserver = Model.GetModelObserver<IWeaponModelObserver>();
            radarModelObserver = Model.GetModelObserver<IRadarModelObserver>();
        }
        
        public void Initialize()
        {
            radarModelObserver.OnHitDetected += HandleEnemy;
            selectionService.OnHitSelected += HandleSelected;
        }

        public void LateDispose()
        {
            radarModelObserver.OnHitDetected -= HandleEnemy;
            selectionService.OnHitSelected -= HandleSelected;
        }
        
        private void HandleSelected(RaycastHit raycastHit)
        {
            if(!selectionModelObserver.IsSelected) return;
            
            mainTarget = raycastHit.collider.GetComponentInChildren<IShipUnitsProvider>();// make unit not depends on ship entity
            if (mainTarget is { PlayerType: PlayerType.Opponent, HasUnits: true })
            {
                weaponComponent.AddTarget(new AttackData(mainTarget,
                    componentHub.GetComponent(mainTarget.ModelObserver),
                    DefaultTargetType), AttackType.MainTarget);
            }
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
                weaponComponent.AddTargets(healthComponents.ToArray());
            }
        }
    }
}