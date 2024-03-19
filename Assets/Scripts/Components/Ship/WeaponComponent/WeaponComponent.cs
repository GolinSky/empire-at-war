using System.Collections.Generic;
using System.Linq;
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
    }

    public class WeaponComponent : BaseComponent<WeaponModel>, IWeaponComponent, IWeaponCommand, ILateTickable, ILateDisposable
    {
        private readonly ITimerPoolWrapperService timerPoolWrapperService;
        private readonly IShipMoveModelObserver shipMoveModelObserver;
        private readonly ITimer attackTimer;
        private float endTimeTween;

        private List<CustomCoroutine> customCoroutines = new List<CustomCoroutine>();
        private List<AttackData> attackDataList = new List<AttackData>();
        private AttackData mainAttackData = null;
        public WeaponComponent(
            IModel model,
            ITimerPoolWrapperService timerPoolWrapperService) : base(model)
        {
            this.timerPoolWrapperService = timerPoolWrapperService;
            shipMoveModelObserver = model.GetModelObserver<IShipMoveModelObserver>();
            attackTimer = TimerFactory.ConstructTimer(3f);
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
                    foreach (AttackData data in attackDataList)
                    {
                        if (attackData == data)
                        {
                            return;
                        }
                    }
                    Model.AddShipUnits(attackData.Units);
                    attackDataList.Add(attackData);
                    break;
                }
                case AttackType.MainTarget:
                {
                    mainAttackData = attackData;
                    Model.MainUnitsTarget = mainAttackData.Units;
                    break;
                }
            }
        }

        public bool HasEnoughRange(float distance)
        {
            return Model.MaxAttackDistance > distance;
        }

        public void ApplyDamage(IShipUnitView unitView, WeaponType weaponType)
        {
            for (var i = 0; i < attackDataList.Count; i++)
            {
                if (attackDataList[i].Contains(unitView))
                {
                    AttackData attackData = attackDataList[i];
                    CustomCoroutine customCoroutine = timerPoolWrapperService.Invoke(
                        ()=>
                        {   
                            if(!attackDataList.Contains(attackData)) return;
                            
                            if(unitView == null) return;// todo: fix bug when loading main menu
                            
                            ApplyDamageInternal(
                                attackData,
                                weaponType,
                                unitView.Id,
                                GetDistance(unitView.Position));
                        },
                        Model.ProjectileDuration);
                    customCoroutines.Add(customCoroutine);
                    customCoroutine.OnFinished += DeleteFromCollection;
                    break;
                }
            }
        }

        private void DeleteFromCollection(CustomCoroutine customCoroutine)
        {
            customCoroutine.OnFinished -= DeleteFromCollection;
            customCoroutines.Remove(customCoroutine);
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
            Vector3.Distance(shipMoveModelObserver.CurrentPosition, targetPosition);

        private void RemoveAttackData(AttackData attackData)
        {
            attackDataList.Remove(attackData);
            Model.RemoveShipUnits(attackData.Units);
        }

        public void LateTick()
        {
            if(attackDataList.Count == 0) return;

            if (attackTimer.IsComplete)
            {
                attackTimer.StartTimer();

                if (mainAttackData is not null)
                {
                    if (mainAttackData.IsDestroyed)
                    {
                        mainAttackData = null;
                        Model.MainUnitsTarget = null;
                    }
                }
                
                for (var i = 0; i < attackDataList.Count; i++)
                {
                    CheckAttackData(attackDataList[i]);
                }
            }
        }

        public void LateDispose()
        {
            attackDataList.Clear();
            if (customCoroutines.Count > 0)
            {
                for (var i = 0; i < customCoroutines.Count; i++)
                {
                    customCoroutines[i].Release();
                }

            }
        }
    }
}