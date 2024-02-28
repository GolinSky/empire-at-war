using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Models.Weapon;
using Utilities.ScriptUtils.EditorSerialization;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;
using Utils.TimerService;
using WorkShop.LightWeightFramework.ViewComponents;
using Zenject;
using Random = System.Random;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public class WeaponViewComponent : ViewComponent<IWeaponModelObserver>, ITickable
    {
        [SerializeField] private DictionaryWrapper<WeaponType, List<TurretView>> turretDictionary;

        private Dictionary<WeaponType, List<TurretView>> TurretDictionary => turretDictionary.Dictionary;

        private List<IShipUnitView> targets;
        private IProjectileModel projectileModel;
        private ITimer attackTimer;
        private List<IShipUnitView> shipUnitViews;
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
                float attackDistance = Model.GetAttackDistance(keyValuePair.Key);
                foreach (TurretView turretView in keyValuePair.Value)
                {
                    turretView.SetData(projectileModel.ProjectileData[keyValuePair.Key], Model.ProjectileDuration, attackDistance);
                }
            }
        }

        protected override void OnRelease()
        {
            base.OnRelease();
            isDead = true;
        }
        
        public List<IShipUnitView> GenerateRandomLoop(List<IShipUnitView> listToShuffle)
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
                if (Model.MainUnitsTarget != null )
                {
                    attackTimer.StartTimer();

                    foreach (KeyValue<WeaponType, List<TurretView>> keyValue in turretDictionary.KeyValueList)
                    {
                        foreach (TurretView turretView in keyValue.Value)
                        {
                            foreach (IShipUnitView shipUnitView in Model.MainUnitsTarget)
                            {
                                if(shipUnitView.IsDestroyed) continue;
                                if (turretView.Destroyed || turretView.IsBusy ||
                                    !turretView.CanAttack(shipUnitView.Position))
                                {
                                    continue;
                                }
                                
                                turretView.Attack(shipUnitView.Position);
                                WeaponCommand.ApplyDamage(shipUnitView, keyValue.Key);
                            }
                        }
                    }
                }
                if (targets != null && targets.Count > 0)
                {
                    attackTimer.StartTimer();
                    shipUnitViews = GenerateRandomLoop(targets.Where(x => !x.IsDestroyed).ToList());
                    
                    foreach (IShipUnitView unitView in shipUnitViews)
                    {
                        if(unitView.IsDestroyed) continue;
                        
                        foreach (KeyValue<WeaponType,List<TurretView>> keyValue in turretDictionary.KeyValueList)
                        {
                            foreach (TurretView turretView in keyValue.Value)
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