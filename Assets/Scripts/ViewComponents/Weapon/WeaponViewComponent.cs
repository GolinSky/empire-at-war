using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Models.Weapon;
using Utilities.ScriptUtils.EditorSerialization;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;
using Zenject;
using Random = System.Random;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public class WeaponViewComponent : ViewComponent<IWeaponModelObserver>
    {
        [SerializeField] private DictionaryWrapper<WeaponType, List<WeaponHardPointView>> turretDictionary;
        [SerializeField] private AttackModelDependency attackModelDependency;
        private Dictionary<WeaponType, List<WeaponHardPointView>> TurretDictionary => attackModelDependency.TurretDictionary;

        private List<IHardPointView> shipUnitViews;
        private IProjectileModel projectileModel;
        private Coroutine mainTargetAttackFlow;
        private Coroutine commonAttackFlow;
        private Random random = new Random();
        private bool isDead;
        
        [Inject]
        private IWeaponCommand WeaponCommand { get; }
        
        private List<IHardPointView> Targets => Model.Targets;

        protected override void OnInit()
        {
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
                if (Targets != null && Targets.Count > 0)
                {
                    shipUnitViews = GetShuffledHardPoint(Targets.Where(x => !x.IsDestroyed).ToList());
                    if (shipUnitViews.Count > 0)
                    {
                        yield return AttackFlow(shipUnitViews);
                    }
                    else
                    {
                        yield return new WaitForEndOfFrame();
                    }
                
                }
                else
                {
                    yield return new WaitUntil(()=> Targets != null && Targets.Count > 0);
                }
            }
        }

        private IEnumerator AttackFlow(List<IHardPointView> hardPointViews)
        {
            foreach (var keyValue in TurretDictionary)
            {
                foreach (WeaponHardPointView weaponHardPointView in keyValue.Value)
                {
                    if (weaponHardPointView.Destroyed || weaponHardPointView.IsBusy)
                    {
                        continue;
                    }
                    
                    foreach (IHardPointView shipUnitView in hardPointViews)
                    {
                        if(shipUnitView.IsDestroyed) continue;
                        
                        if(!weaponHardPointView.CanAttack(shipUnitView.Position)) continue;
                        
                        float duration = weaponHardPointView.Attack(shipUnitView.Position);
                        WeaponCommand.ApplyDamage(shipUnitView, keyValue.Key, duration);
                        yield return new WaitForSeconds(Model.DelayBetweenAttack);
                    }
                    
                    bool allDestroyed = hardPointViews.All(x => x.IsDestroyed);

                    if (allDestroyed)
                    {
                        yield break;
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