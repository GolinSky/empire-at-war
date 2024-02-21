using System.Collections.Generic;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Models.Weapon;
using EmpireAtWar.ScriptUtils.EditorSerialization;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;
using Utils.TimerService;
using WorkShop.LightWeightFramework.ViewComponents;
using Zenject;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public class WeaponViewComponent : ViewComponent<IWeaponModelObserver>, ITickable
    {
        [SerializeField] private DictionaryWrapper<WeaponType, List<TurretView>> turretDictionary;

        private Dictionary<WeaponType, List<TurretView>> TurretDictionary => turretDictionary.Dictionary;

        private List<IShipUnitView> targets;
        private IProjectileModel projectileModel;
        private ITimer attackTimer;
        private bool isDead;
        
        [Inject]
        private IWeaponCommand WeaponCommand { get; }
        
        protected override void OnInit()
        {
            attackTimer = TimerFactory.ConstructTimer(0.1f);
            targets = Model.Targets;

            projectileModel = Model.ProjectileModel;
            foreach (var keyValuePair in TurretDictionary)
            {
                foreach (TurretView turretView in keyValuePair.Value)
                {
                    turretView.SetData(projectileModel.ProjectileData[keyValuePair.Key], Model.ProjectileDuration);
                }
            }
        }

        protected override void OnRelease()
        {
            base.OnRelease();
            isDead = true;
        }

        public void Tick()
        {
            if(isDead) return;
            
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
                                    WeaponCommand.ApplyDamage(
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