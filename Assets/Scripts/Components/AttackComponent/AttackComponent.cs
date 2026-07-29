using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Collections;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.CoroutineService;
using EmpireAtWar.Services.TimerPoolWrapperService;
using EmpireAtWar.Mvc;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;
using Utilities.ScriptUtils.EditorSerialization;
using Utilities.ScriptUtils.Time;
using Zenject;

namespace EmpireAtWar.Components.AttackComponent
{
    public interface IAttackComponent:IComponent
    {
        void AddTargets(AttackData[] healthComponent);
        void AddTarget(AttackData healthComponent, AttackType attackType);
        bool HasEnoughRange(float distance);
        void ResetTarget();
    }

    [Obsolete]
    public class AttackComponent : MonoComponent<AttackModel>, IAttackComponent, IAttackCommand, IInitializable,
        ILateTickable, ILateDisposable, IDisposable
    {
        [SerializeField] private DictionaryWrapper<WeaponType, List<WeaponHardPointView>> turretDictionary;
        [SerializeField] private Transform viewTransform;

        private ICoroutineService _coroutineService;

        private List<Coroutine> _coroutines = new List<Coroutine>();
        private List<AttackData> _attackDataList = new List<AttackData>();
        private AttackData _mainAttackData = null;
        private float _endTimeTween;
        private Coroutine _mainTargetAttackFlow;
        private Coroutine _commonAttackFlow;
        private bool _isDead;
        private bool _isInitialized;

        public Dictionary<WeaponType, List<WeaponHardPointView>> TurretDictionary => turretDictionary.Dictionary;

        [Inject]
        private void Construct(
            AttackModel model,
            ICoroutineService coroutineService)
        {
            SetModel(model);
            _coroutineService = coroutineService;
        }

        public void Initialize()
        {
            if (viewTransform == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(AttackComponent)} requires a serialized view transform.");
            }

            Model.InjectDependency(TurretDictionary);
            Model.OnMainUnitSwitched += HandleNewMainTarget;
            _isInitialized = true;
            StartAttackFlows();
        }

        public void AddTargets(AttackData[] attackDataArray)
        {
            foreach (AttackData component in attackDataArray)
            {
                AddTarget(component, AttackType.Base);
            }
        }

        public void AddTarget(AttackData attackData, AttackType attackType)
        {
            switch (attackType)
            {
                case AttackType.Base:
                {
                    foreach (AttackData data in _attackDataList)
                    {
                        if (attackData.SameSource(data))
                        {
                            return;
                        }
                    }
                    Model.AddShipUnits(attackData.Units);
                    _attackDataList.Add(attackData);
                    break;
                }
                case AttackType.MainTarget:
                {
                    if (_mainAttackData != null)
                    {
                        _attackDataList.Remove(_mainAttackData);
                    }
                    _mainAttackData = attackData;
                    _attackDataList.Add(attackData);
                    Model.MainUnitsTarget = _mainAttackData.Units;
                    break;
                }
            }
        }

        public void ResetTarget()
        {
            ResetMainTarget();
        }
        
        public bool HasEnoughRange(float distance)
        {
            return Model.OptimalAttackRange > distance;
        }

        public void ApplyDamage(IHardPointModel unitView, WeaponType weaponType, float duration)
        {
            for (var i = 0; i < _attackDataList.Count; i++)
            {
                if (_attackDataList[i].Contains(unitView))
                {
                    AttackData attackData = _attackDataList[i];
                    Coroutine coroutine = null;
                    coroutine = _coroutineService.InvokeWithDelay(() =>
                    {
                        if(!_attackDataList.Contains(attackData)) return;
                            
                        if(unitView == null) return;// todo: fix bug when loading main menu
                            
                        ApplyDamageInternal(
                            attackData,
                            weaponType,
                            unitView.Id,
                            GetDistance(unitView.Position));
                        DeleteFromCollection(coroutine);
                    }, duration);

                    _coroutines.Add(coroutine);

                    break;
                }
            }
        }

        private void DeleteFromCollection(Coroutine customCoroutine)
        {
            _coroutines.Remove(customCoroutine);
        }

        private void ApplyDamageInternal(AttackData attackData, WeaponType weaponType, int id, float distance)
        {
            if (attackData.IsDestroyed)
            {
                RemoveAttackData(attackData);
                return;
            }
            attackData.ApplyDamage(Model.GetDamage(weaponType,distance), weaponType, id);
        }

        private void CheckAttackData(AttackData attackData)
        {
            if (attackData.IsDestroyed)
            {
                RemoveAttackData(attackData);
                return;
            }

            bool hasTargets = attackData.Units.Any(x => !x.IsDestroyed);

            if (!hasTargets)
            {
                Model.RemoveShipUnits(attackData.Units);
                if (attackData.TryUpdateNewUnits())
                {
                    Model.AddShipUnits(attackData.Units);
                }
            }
        }

        private float GetDistance(Vector3 targetPosition) =>
            Vector3.Distance(viewTransform.position, targetPosition);

        private void RemoveAttackData(AttackData attackData)
        {
            _attackDataList.Remove(attackData);
            Model.RemoveShipUnits(attackData.Units);
        }

        public void LateTick()
        {
            if (_mainAttackData?.IsDestroyed == true)
            {
                ResetMainTarget();
            }
            
            if(_attackDataList.Count == 0) return;
 
            for (var i = 0; i < _attackDataList.Count; i++)
            {
                CheckAttackData(_attackDataList[i]);
            }
        }

        private void ResetMainTarget()
        {
            if (_mainAttackData != null)
            {
                _attackDataList.Remove(_mainAttackData);
            }

            _mainAttackData = null;
            Model.MainUnitsTarget = null;
        }

        public void LateDispose()
        {
            Release();
        }

        public override void Release()
        {
            if (_isDead)
            {
                return;
            }

            _isDead = true;
            Model.OnMainUnitSwitched -= HandleNewMainTarget;
            StopIfRunning(ref _mainTargetAttackFlow);
            StopIfRunning(ref _commonAttackFlow);

            for (var i = 0; i < _attackDataList.Count; i++)
            {
                RemoveAttackData(_attackDataList[i]);
            }
            _attackDataList.Clear();
            if (_coroutines.Count > 0)
            {
                for (var i = 0; i < _coroutines.Count; i++)
                {
                    if (_coroutines[i] != null)
                    {
                        _coroutineService.StopCustomCoroutine(_coroutines[i]);
                    }
                }
            }
        }

        public void Dispose()
        {
            Release();
        }

        private void HandleNewMainTarget()
        {
            if (_isDead || !isActiveAndEnabled || Model.MainUnitsTarget == null ||
                Model.MainUnitsTarget.Count == 0)
            {
                return;
            }

            StopIfRunning(ref _mainTargetAttackFlow);
            _mainTargetAttackFlow = StartCoroutine(AttackFlowLoop(() => Model.MainUnitsTarget));
        }

        private void OnEnable()
        {
            if (_isInitialized)
            {
                StartAttackFlows();
            }
        }

        private void OnDisable()
        {
            if (!_isInitialized || _isDead)
            {
                return;
            }

            StopIfRunning(ref _mainTargetAttackFlow);
            StopIfRunning(ref _commonAttackFlow);
        }

        private void StartAttackFlows()
        {
            if (!isActiveAndEnabled || _commonAttackFlow != null)
            {
                return;
            }

            _commonAttackFlow = StartCoroutine(AttackFlowLoop(() => Model.Targets));
            HandleNewMainTarget();
        }

        private IEnumerator AttackFlowLoop(Func<List<IHardPointModel>> targetProvider)
        {
            while (!_isDead)
            {
                List<IHardPointModel> validTargets = targetProvider()?
                    .Where(x => !x.IsDestroyed)
                    .ToList()
                    .GetShuffledCollection() ?? new List<IHardPointModel>();

                if (validTargets.Count == 0)
                {
                    yield return new WaitUntil(() => targetProvider()?.Any(x => !x.IsDestroyed) == true);
                    continue;
                }

                foreach (KeyValuePair<WeaponType, List<WeaponHardPointView>> pair in TurretDictionary)
                {
                    foreach (WeaponHardPointView turret in pair.Value)
                    {
                        if (turret.Destroyed || turret.IsBusy)
                        {
                            continue;
                        }

                        IHardPointModel target = validTargets.FirstOrDefault(
                            candidate => !candidate.IsDestroyed && turret.CanAttack(candidate.Position));
                        if (target == null)
                        {
                            continue;
                        }

                        yield return new WaitForSeconds(Model.DelayBetweenAttack);
                    }
                }

                yield return null;
            }
        }

        private void StopIfRunning(ref Coroutine coroutine)
        {
            if (coroutine == null)
            {
                return;
            }

            StopCoroutine(coroutine);
            coroutine = null;
        }



    
    }
}
