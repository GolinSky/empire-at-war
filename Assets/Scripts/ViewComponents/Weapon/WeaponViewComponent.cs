using System;
using System.Collections;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Models.Weapon;
using EmpireAtWar.ScriptUtils.EditorSerialization;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;
using Utils.TimerService;
using WorkShop.LightWeightFramework.Command;
using WorkShop.LightWeightFramework.ViewComponents;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public class WeaponViewComponent : ViewComponent
    {
        [SerializeField] private DictionaryWrapper<WeaponType, List<TurretView>> turretDictionary;

        private Dictionary<WeaponType, List<TurretView>> TurretDictionary => turretDictionary.Dictionary;

        private List<IShipUnitView> targets;
        private IWeaponModelObserver weaponModelObserver;
        private IProjectileModel projectileModel;
        private ITimer attackTimer;
        private IWeaponCommand weaponCommand;
        

        protected override void OnInit()
        {
            attackTimer = TimerFactory.ConstructTimer(0.1f);
            weaponModelObserver = ModelObserver.GetModelObserver<IWeaponModelObserver>();
            targets = weaponModelObserver.Targets;

            projectileModel = weaponModelObserver.ProjectileModel;
            foreach (var keyValuePair in TurretDictionary)
            {
                foreach (TurretView turretView in keyValuePair.Value)
                {
                    turretView.SetData(projectileModel.ProjectileData[keyValuePair.Key], weaponModelObserver.ProjectileDuration);
                }
            }
        }
        
        protected override void OnRelease()
        {
        }

        protected override void OnCommandSet(ICommand command)
        {
            base.OnCommandSet(command);
            command.TryGetCommand(out weaponCommand);
        }


        // todo : inject all viewcomponent - delete template methods and refactor viewcomponents
        private void Update()
        {
            if (targets != null && targets.Count > 0)
            {
                if (attackTimer.IsComplete)
                {
                    attackTimer.StartTimer();

                    for (var i = 0; i < targets.Count; i++)
                    {
                        if (!targets[i].IsDestroyed)
                        {
                            foreach (var turretDictionaryValue in TurretDictionary)
                            {
                                foreach (TurretView turretView in turretDictionaryValue.Value)
                                {
                                    if (turretView.IsBusy || !turretView.CanAttack(targets[i].Position))
                                    {
                                        continue;
                                    }

                                    //todo: put real distance in command param
                                    turretView.Attack(targets[i].Position);
                                    weaponCommand.ApplyDamage(
                                        targets[i], 
                                        turretDictionaryValue.Key);
                                }
                            }
                            //List<WeaponType> filter = weaponModelObserver.Filter()
                        }
                    }
                }
            }
        }
        
    }
}