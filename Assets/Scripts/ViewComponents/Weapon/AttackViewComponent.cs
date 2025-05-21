using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Collections;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Models.Health;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public class AttackViewComponent : ViewComponent<IAttackModelObserver>
    {
        [SerializeField] private AttackModelDependency attackModelDependency;

        private Dictionary<WeaponType, List<WeaponHardPointView>> TurretDictionary =>
            attackModelDependency.TurretDictionary;

        private List<IHardPointModel> _targetHardPointModels;
        private List<IHardPointModel> _mainTargetHardPointModels;
        private IProjectileModel _projectileModel;
        private Coroutine _mainTargetAttackFlow;
        private Coroutine _commonAttackFlow;
        private bool _isDead;

        [Inject] private IAttackCommand AttackCommand { get; }

        private List<IHardPointModel> Targets => Model.Targets; //todo: use observable list in weapon model

        private void OnEnable()
        {
            Model.OnMainUnitSwitched += HandleNewMainTarget;
            _commonAttackFlow = StartCoroutine(AttackFlowLoop(() => Model.Targets));
        }

        protected override void OnInit()
        {
            _projectileModel = Model.ProjectileModel;
            foreach (var keyValuePair in TurretDictionary)
            {
                if (keyValuePair.Value == null) continue;

                float attackDistance = Model.GetAttackDistance(keyValuePair.Key);
                foreach (WeaponHardPointView turretView in keyValuePair.Value)
                {
                    if (turretView == null) continue;
                    turretView.SetData(_projectileModel.ProjectileData[keyValuePair.Key], attackDistance);
                }
            }
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

            if (_commonAttackFlow != null)
            {
                StopCoroutine(_commonAttackFlow);
            }
        }
        
        private void HandleNewMainTarget()
        {
            if (ShouldSkipMainTargetAttack()) return;

            StopIfRunning(ref _mainTargetAttackFlow);
            _mainTargetAttackFlow = StartCoroutine(AttackFlowLoop(() => Model.MainUnitsTarget));
        }
        
        private IEnumerator AttackFlowLoop(Func<List<IHardPointModel>> targetProvider)
        {
            while (!IsDead())
            {
                var validTargets = GetValidHardPoints(targetProvider());

                if (validTargets.Count > 0)
                {
                    yield return ExecuteAttackFlow(validTargets);
                }
                else
                {
                    yield return new WaitUntil(() => targetProvider()?.Any(x => !x.IsDestroyed) == true);
                }
            }
        }

        private List<IHardPointModel> GetValidHardPoints(List<IHardPointModel> rawTargets)
        {
            return rawTargets?.Where(x => !x.IsDestroyed).ToList().GetShuffledCollection() ?? new List<IHardPointModel>();
        }

        private IEnumerator ExecuteAttackFlow(List<IHardPointModel> hardPoints)
        {
            foreach (var kvp in TurretDictionary)
            {
                foreach (WeaponHardPointView turret in kvp.Value)
                {
                    if (turret.Destroyed || turret.IsBusy) continue;

                    foreach (IHardPointModel target in hardPoints)
                    {
                        if (target.IsDestroyed || !turret.CanAttack(target.Position)) continue;

                        float attackDuration = turret.Attack(target);
                        AttackCommand.ApplyDamage(target, kvp.Key, attackDuration);
                        yield return new WaitForSeconds(Model.DelayBetweenAttack);
                    }

                    if (hardPoints.All(x => x.IsDestroyed))
                        yield break;
                }
            }
        }
        

        private bool IsDead() => _isDead;

        private bool ShouldSkipMainTargetAttack()
        {
            return IsDead() || Model.MainUnitsTarget == null || Model.MainUnitsTarget.Count == 0;
        }

        private void StopIfRunning(ref Coroutine coroutine)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
                coroutine = null;
            }
        }
    }
}