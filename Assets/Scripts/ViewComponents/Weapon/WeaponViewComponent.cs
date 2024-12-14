using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Models.Weapon;
using Utilities.ScriptUtils.EditorSerialization;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;
using LightWeightFramework.Components.ViewComponents;
using Zenject;
using Random = System.Random;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public class WeaponViewComponent : ViewComponent<IWeaponModelObserver>
    {
        [SerializeField] private DictionaryWrapper<WeaponType, List<WeaponHardPointView>> turretDictionary;

        private Dictionary<WeaponType, List<WeaponHardPointView>> TurretDictionary => turretDictionary.Dictionary;

        private List<IHardPointView> shipUnitViews;
        private List<IHardPointView> targets;
        private IProjectileModel projectileModel;
        private Coroutine mainTargetAttackFlow;
        private Coroutine commonAttackFlow;
        private Random random = new Random();
        private bool isDead;
        
        [Inject]
        private IWeaponCommand WeaponCommand { get; }
        
        protected override void OnInit()
        {
            targets = Model.Targets;

            projectileModel = Model.ProjectileModel;
            foreach (var keyValuePair in TurretDictionary)
            {
                if(keyValuePair.Value == null) continue;
                
                float attackDistance = Model.GetAttackDistance(keyValuePair.Key);
                foreach (WeaponHardPointView turretView in keyValuePair.Value)
                {
                    if(turretView == null) continue;
                    turretView.SetData(projectileModel.ProjectileData[keyValuePair.Key], attackDistance);
                }
            }

        }

        private void OnEnable()
        {
            Model.OnMainUnitSwitched += HandleNewMainTarget;
            commonAttackFlow = StartCoroutine(CommonAttackFlow());
        }

        protected override void OnRelease()
        {
            base.OnRelease();
            Model.OnMainUnitSwitched -= HandleNewMainTarget;
            isDead = true;

            if (mainTargetAttackFlow != null)
            {
                StopCoroutine(mainTargetAttackFlow);
            }
            if(commonAttackFlow != null)
            {
                StopCoroutine(commonAttackFlow);
            }
        }
        
        private void HandleNewMainTarget()
        {
            if(isDead) return;
            if(mainTargetAttackFlow != null) StopCoroutine(mainTargetAttackFlow);
            
            if(Model.MainUnitsTarget == null || Model.MainUnitsTarget.Count == 0) return;
            
            mainTargetAttackFlow = StartCoroutine(AttackFlow(Model.MainUnitsTarget));
        }

        private IEnumerator CommonAttackFlow()
        {
            while (!isDead)
            {
                if (targets != null && targets.Count > 0)
                {
                    shipUnitViews = GetShuffledHardPoint(targets.Where(x => !x.IsDestroyed).ToList());
                    yield return AttackFlow(shipUnitViews);
                }
                else
                {
                    yield return new WaitUntil(()=> targets != null && targets.Count > 0);
                }
            }
        }

        private IEnumerator AttackFlow(List<IHardPointView> hardPointViews)
        {
            foreach (KeyValue<WeaponType, List<WeaponHardPointView>> keyValue in turretDictionary.KeyValueList)
            {
                foreach (WeaponHardPointView weaponHardPointView in keyValue.Value)
                {
                    foreach (IHardPointView shipUnitView in hardPointViews)
                    {
                        if (weaponHardPointView.Destroyed || weaponHardPointView.IsBusy ||
                            !weaponHardPointView.CanAttack(shipUnitView.Position))
                        {
                            continue;
                        }
                        
                        float duration = weaponHardPointView.Attack(shipUnitView.Position);
                        WeaponCommand.ApplyDamage(shipUnitView, keyValue.Key, duration);
                        yield return new WaitForSeconds(Model.DelayBetweenAttack);
                    }
                }
            }
        }

      
        
        private List<IHardPointView> GetShuffledHardPoint(List<IHardPointView> listToShuffle)
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
    }
}