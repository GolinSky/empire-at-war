using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Movement;
using EmpireAtWar.Models.Weapon;
using EmpireAtWar.Services.TimerPoolWrapperService;
using EmpireAtWar.ViewComponents.Health;
using LightWeightFramework.Model;
using UnityEngine;
using Utilities.ScriptUtils.Time;
using LightWeightFramework.Components.Components;
using Zenject;

namespace EmpireAtWar.Components.Ship.WeaponComponent
{
    public interface IWeaponComponent:IComponent
    {
        void AddTargets(AttackData[] healthComponent);
        void AddTarget(AttackData healthComponent, AttackType attackType);
        bool HasEnoughRange(float distance);
        float OptimalAttackRange { get; }
        void ResetTarget();
    }

    public class WeaponComponent : BaseComponent<WeaponModel>, IWeaponComponent, IWeaponCommand, ILateTickable, ILateDisposable, IDisposable
    {
        private readonly ITimerPoolWrapperService _timerPoolWrapperService;
        private readonly ISimpleMoveModelObserver _simpleMoveModelObserver;
        private readonly ITimer _attackTimer;

        private List<CustomCoroutine> _customCoroutines = new List<CustomCoroutine>();
        private List<AttackData> _attackDataList = new List<AttackData>();
        private AttackData _mainAttackData = null;
        private float _endTimeTween;

        public float OptimalAttackRange { get; }

        public WeaponComponent(IModel model, ITimerPoolWrapperService timerPoolWrapperService) : base(model)
        {
            _timerPoolWrapperService = timerPoolWrapperService;
            _simpleMoveModelObserver = model.GetModelObserver<ISimpleMoveModelObserver>();
            _attackTimer = TimerFactory.ConstructTimer(3f);
            OptimalAttackRange = Model.MaxAttackDistance * 0.5f;
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
                        if (attackData == data)
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
            return OptimalAttackRange > distance;
        }

        public void ApplyDamage(IHardPointModel unitView, WeaponType weaponType, float duration)
        {
            for (var i = 0; i < _attackDataList.Count; i++)
            {
                if (_attackDataList[i].Contains(unitView))
                {
                    AttackData attackData = _attackDataList[i];
                    CustomCoroutine customCoroutine = _timerPoolWrapperService.Invoke(
                        ()=>
                        {   
                            if(!_attackDataList.Contains(attackData)) return;
                            
                            if(unitView == null) return;// todo: fix bug when loading main menu
                            
                            ApplyDamageInternal(
                                attackData,
                                weaponType,
                                unitView.Id,
                                GetDistance(unitView.Position));
                        },
                        duration);
                    _customCoroutines.Add(customCoroutine);
                    customCoroutine.OnFinished += DeleteFromCollection;
                    break;
                }
            }
        }

        private void DeleteFromCollection(CustomCoroutine customCoroutine)
        {
            customCoroutine.OnFinished -= DeleteFromCollection;
            _customCoroutines.Remove(customCoroutine);
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
            Vector3.Distance(_simpleMoveModelObserver.CurrentPosition, targetPosition);

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
            for (var i = 0; i < _attackDataList.Count; i++)
            {
                RemoveAttackData(_attackDataList[i]);
            }
            _attackDataList.Clear();
            if (_customCoroutines.Count > 0)
            {
                for (var i = 0; i < _customCoroutines.Count; i++)
                {
                    _customCoroutines[i].Release();
                }
            }
        }

        public void Dispose()
        {
            // TODO release managed resources here
        }
    }
}