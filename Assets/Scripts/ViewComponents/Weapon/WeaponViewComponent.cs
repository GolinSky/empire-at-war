using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Models.Weapon;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;
using Zenject;
using Random = System.Random;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public class WeaponViewComponent : ViewComponent<IWeaponModelObserver>
    {
        [SerializeField] private AttackModelDependency attackModelDependency;
        private Dictionary<WeaponType, List<WeaponHardPointView>> TurretDictionary => attackModelDependency.TurretDictionary;

        private List<IHardPointView> _shipUnitViews;
        private IProjectileModel _projectileModel;
        private Coroutine _mainTargetAttackFlow;
        private Coroutine _commonAttackFlow;
        private Random _random = new Random();
        private bool _isDead;
        
        [Inject]
        private IWeaponCommand WeaponCommand { get; }
        
        private List<IHardPointView> Targets => Model.Targets;

        protected override void OnInit()
        {
            _projectileModel = Model.ProjectileModel;
            foreach (var keyValuePair in TurretDictionary)
            {
                if(keyValuePair.Value == null) continue;
                
                float attackDistance = Model.GetAttackDistance(keyValuePair.Key);
                foreach (WeaponHardPointView turretView in keyValuePair.Value)
                {
                    if(turretView == null) continue;
                    turretView.SetData(_projectileModel.ProjectileData[keyValuePair.Key], attackDistance);
                }
            }

        }

        private void OnEnable()
        {
            Model.OnMainUnitSwitched += HandleNewMainTarget;
            _commonAttackFlow = StartCoroutine(CommonAttackFlow());
        }

        protected override void OnRelease()
        {
            base.OnRelease();
            Model.OnMainUnitSwitched -= HandleNewMainTarget;
            _isDead = true;

            if (_mainTargetAttackFlow != null)
            {
                StopCoroutine(_mainTargetAttackFlow);
            }
            if(_commonAttackFlow != null)
            {
                StopCoroutine(_commonAttackFlow);
            }
        }
        
        private void HandleNewMainTarget()
        {
            if(_isDead) return;
            
            if(_mainTargetAttackFlow != null) StopCoroutine(_mainTargetAttackFlow);

            if(Model.MainUnitsTarget == null || Model.MainUnitsTarget.Count == 0) return;
            
            _mainTargetAttackFlow = StartCoroutine(AttackFlow(Model.MainUnitsTarget));
        }

        private IEnumerator CommonAttackFlow()
        {
            while (!_isDead)
            {
                if (Targets != null && Targets.Count > 0)
                {
                    _shipUnitViews = GetShuffledHardPoint(Targets.Where(x => !x.IsDestroyed).ToList());
                    if (_shipUnitViews.Count > 0)
                    {
                        yield return AttackFlow(_shipUnitViews);
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
                        
                        if(!weaponHardPointView.CanAttack(shipUnitView.Position) || weaponHardPointView.Destroyed || weaponHardPointView.IsBusy) continue;
                        
                        float duration = weaponHardPointView.Attack(shipUnitView);
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
                var k = _random.Next(i + 1);
                var value = listToShuffle[k];
                listToShuffle[k] = listToShuffle[i];
                listToShuffle[i] = value;
            }

            listToShuffle.Reverse();
            return listToShuffle;
        }
    }
}