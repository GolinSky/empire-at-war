using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Models.Weapon;
using Utilities.ScriptUtils.EditorSerialization;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;
using Utilities.ScriptUtils.Time;
using LightWeightFramework.Components.ViewComponents;
using Zenject;
using Random = System.Random;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public class WeaponViewComponent : ViewComponent<IWeaponModelObserver>, ITickable
    {
        [SerializeField] private DictionaryWrapper<WeaponType, List<WeaponHardPointView>> turretDictionary;

        private Dictionary<WeaponType, List<WeaponHardPointView>> TurretDictionary => turretDictionary.Dictionary;

        private List<IHardPointView> shipUnitViews;
        private List<IHardPointView> targets;
        private IProjectileModel projectileModel;
        private ITimer attackTimer;
        private Random random = new Random();
        private bool isDead;
        
        [Inject]
        private IWeaponCommand WeaponCommand { get; }
        
        protected override void OnInit()
        {
            attackTimer = TimerFactory.ConstructTimer(1f);
            targets = Model.Targets;

            projectileModel = Model.ProjectileModel;
            foreach (var keyValuePair in TurretDictionary)
            {
                if(keyValuePair.Value == null) continue;
                
                float attackDistance = Model.GetAttackDistance(keyValuePair.Key);
                foreach (WeaponHardPointView turretView in keyValuePair.Value)
                {
                    if(turretView == null) continue;
                    turretView.SetData(projectileModel.ProjectileData[keyValuePair.Key], Model.ProjectileDuration, attackDistance);
                }
            }
        }

        protected override void OnRelease()
        {
            base.OnRelease();
            isDead = true;
        }
        
        public List<IHardPointView> GetShuffledHardPoint(List<IHardPointView> listToShuffle)
        {
            for (int i = listToShuffle.Count - 1; i > 0; i--)
            {
                var k = random.Next(i + 1);
                var value = listToShuffle[k];
                listToShuffle[k] = listToShuffle[i];
                listToShuffle[i] = value;
            }

            listToShuffle.Reverse();
            return listToShuffle;
        }

        public void Tick()
        {
            if(isDead) return;
            
            if (attackTimer.IsComplete)
            {
                if (Model.MainUnitsTarget != null && Model.MainUnitsTarget.Count != 0)
                {
                    attackTimer.StartTimer();

                    foreach (KeyValue<WeaponType, List<WeaponHardPointView>> keyValue in turretDictionary.KeyValueList)
                    {
                        foreach (WeaponHardPointView weaponHardPointView in keyValue.Value)
                        {
                            foreach (IHardPointView shipUnitView in Model.MainUnitsTarget)
                            {
                                if(shipUnitView.IsDestroyed) continue;
                                
                                if (weaponHardPointView.Destroyed || weaponHardPointView.IsBusy ||
                                    !weaponHardPointView.CanAttack(shipUnitView.Position))
                                {
                                    continue;
                                }
                                
                                weaponHardPointView.Attack(shipUnitView.Position);
                                WeaponCommand.ApplyDamage(shipUnitView, keyValue.Key);
                            }
                        }
                    }
                }
                if (targets != null && targets.Count > 0)
                {
                    attackTimer.StartTimer();
                    shipUnitViews = GetShuffledHardPoint(targets.Where(x => !x.IsDestroyed).ToList());
                    
                    foreach (IHardPointView unitView in shipUnitViews)
                    {
                        if(unitView.IsDestroyed) continue;
                        
                        foreach (KeyValue<WeaponType,List<WeaponHardPointView>> keyValue in turretDictionary.KeyValueList)
                        {
                            foreach (WeaponHardPointView turretView in keyValue.Value)
                            {
                                if (turretView.Destroyed || turretView.IsBusy || !turretView.CanAttack(unitView.Position))
                                {
                                    continue;
                                }
                                
                                turretView.Attack(unitView.Position);
                                WeaponCommand.ApplyDamage(unitView, keyValue.Key);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}