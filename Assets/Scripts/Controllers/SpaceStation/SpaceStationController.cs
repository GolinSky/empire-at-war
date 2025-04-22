using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
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
    /// <summary>
    ///  add state component
    /// </summary>
    public class SpaceStationController : Controller<SpaceStationModel>, IInitializable, ILateDisposable
    {
        private const HardPointType DEFAULT_TARGET_TYPE = HardPointType.Any;
        private readonly IWeaponComponent _weaponComponent;
        private readonly IComponentHub _componentHub;
        private readonly ISelectionService _selectionService;
        private readonly ISelectionModelObserver _selectionModelObserver;
        private readonly IWeaponModelObserver _weaponModelObserver;
        
        private IRadarModelObserver _radarModelObserver;
        private IHealthModelObserver _mainTarget;
        public SpaceStationController(
            SpaceStationModel model,
            IWeaponComponent weaponComponent,
            IComponentHub componentHub,
            ISelectionService selectionService) : base(model)
        {
            _weaponComponent = weaponComponent;
            _componentHub = componentHub;
            _selectionService = selectionService;
            _selectionModelObserver = Model.GetModelObserver<ISelectionModelObserver>();
            _weaponModelObserver = Model.GetModelObserver<IWeaponModelObserver>();
            _radarModelObserver = Model.GetModelObserver<IRadarModelObserver>();
        }
        
        public void Initialize()
        {
            _radarModelObserver.OnHitDetected += HandleEnemy;
            // _selectionService.OnHitSelected += HandleSelected;
        }

        public void LateDispose()
        {
            _radarModelObserver.OnHitDetected -= HandleEnemy;
            // _selectionService.OnHitSelected -= HandleSelected;
        }
        
       

        private void HandleEnemy(RaycastHit[] raycastHit)
        {
            // List<AttackData> healthComponents = new List<AttackData>();
            // foreach (RaycastHit hit in raycastHit)
            // {
            //     IHardPointsProvider unitsProvider = hit.collider.GetComponentInChildren<IHardPointsProvider>();
            //     if (unitsProvider != null && unitsProvider.HasUnits)
            //     {
            //         healthComponents.Add(new AttackData(unitsProvider, _componentHub.GetComponent(unitsProvider.ModelObserver), DEFAULT_TARGET_TYPE));
            //     }
            // }
            //
            // if (healthComponents.Count != 0)
            // {
            //     _weaponComponent.AddTargets(healthComponents.ToArray());
            // }
        }
    }
}