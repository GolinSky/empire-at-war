using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Movement;
using EmpireAtWar.Models.Radar;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Models.Weapon;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.ComponentHub;
using EmpireAtWar.ViewComponents.Health;
using LightWeightFramework.Model;
using UnityEngine;
using Utilities.ScriptUtils.Time;
using Zenject;
using Component = LightWeightFramework.Components.Components.Component;

namespace EmpireAtWar.Components.Ship.AiComponent
{
    public class AiComponent : Component, IInitializable, ILateDisposable
    {
        private const ShipUnitType DefaultTargetType = ShipUnitType.Any;
        private readonly IMoveComponent moveComponent;
        private readonly IWeaponComponent weaponComponent;
        private readonly IComponentHub componentHub;
        private readonly ISelectionService selectionService;
        private readonly ISelectionModelObserver selectionModelObserver;
        private readonly IMoveModelObserver moveModelObserver;
        private readonly IWeaponModelObserver weaponModelObserver;
        private readonly ITimer moveAroundTimer;
        
        private IHealthModelObserver healthModelObserver;
        private IRadarModelObserver radarModelObserver;
        private IShipUnitsProvider mainTarget;


        //todo: radar component
        public AiComponent(IModel model, IMoveComponent moveComponent, IWeaponComponent weaponComponent, IComponentHub componentHub, ISelectionService selectionService)
        {
            this.moveComponent = moveComponent;
            this.weaponComponent = weaponComponent;
            this.componentHub = componentHub;
            this.selectionService = selectionService;
            healthModelObserver = model.GetModelObserver<IHealthModelObserver>();
            radarModelObserver = model.GetModelObserver<IRadarModelObserver>();
            selectionModelObserver = model.GetModelObserver<ISelectionModelObserver>();
            moveModelObserver = model.GetModelObserver<IMoveModelObserver>();
            weaponModelObserver = model.GetModelObserver<IWeaponModelObserver>();
            moveAroundTimer = TimerFactory.ConstructTimer(10f);
        }

        public void Initialize()
        {
            radarModelObserver.OnHitDetected += HandleEnemy;
            selectionService.OnHitSelected += HandleSelected;
            healthModelObserver.OnValueChanged += HandleHealth;
        }

        public void LateDispose()
        {
            radarModelObserver.OnHitDetected -= HandleEnemy;
            selectionService.OnHitSelected -= HandleSelected;
            healthModelObserver.OnValueChanged -= HandleHealth;
        }
        
        private void HandleHealth()
        {
            if(mainTarget != null) return;
            
            //add condition - if under attack
            if (moveAroundTimer.IsComplete && healthModelObserver.ShieldPercentage < 0.5f)
            {
                moveAroundTimer.ChangeDelay(moveComponent.MoveAround()); 
                moveAroundTimer.StartTimer();
            }
        }
        
        private void HandleSelected(RaycastHit raycastHit)
        {
            if(!selectionModelObserver.IsSelected) return;
            
            mainTarget = raycastHit.collider.GetComponentInChildren<IShipUnitsProvider>();
            if (mainTarget is { PlayerType: PlayerType.Opponent, HasUnits: true })
            {
                Vector3 targetPosition = raycastHit.transform.position;
                float distance = Vector3.Distance(moveModelObserver.CurrentPosition, targetPosition);
                if (!weaponComponent.HasEnoughRange(distance))
                {
                    Vector3 lookDirection = moveComponent.CalculateLookDirection(targetPosition);
                    float attackDistance = distance - (weaponModelObserver.MaxAttackDistance/2f);
                    Vector3 attackPosition = moveModelObserver.CurrentPosition +
                                             lookDirection.normalized*attackDistance;
                    moveComponent.MoveToPosition(attackPosition);
                }
                else
                {
                    moveComponent.LookAtTarget(targetPosition);
                }
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