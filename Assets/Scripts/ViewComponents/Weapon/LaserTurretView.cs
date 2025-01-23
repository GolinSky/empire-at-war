using System;
using EmpireAtWar.Models.Weapon;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;
using Utilities.ScriptUtils.Time;
using VolumetricLines;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public class LaserTurretView : BaseTurretView
    {
        [SerializeField] private VolumetricLineBehavior volumetricLineBehavior;

        public override bool IsBusy => !_delayTimer.IsComplete;
        public override float Speed => 100f;
        
        private ITimer _attackTimer = TimerFactory.ConstructTimer();
        private ITimer _delayTimer = TimerFactory.ConstructTimer();
        
        private IHardPointView _hardPointView;

        private bool _isAttacking;
        private Vector3 _targetPosition = Vector3.zero;
        private ProjectileData _projectileData;


        public override void SetData(ProjectileData projectileData, float attackDistance)
        {
            _projectileData = projectileData;
        }

        public override void Attack(IHardPointView hardPointView, float duration)
        {
            _hardPointView = hardPointView;
           
            _targetPosition.z = Vector3.Distance(hardPointView.Position, transform.position);
            volumetricLineBehavior.SetStartAndEndPoints(Vector3.zero, _targetPosition);


            _attackTimer.ChangeDelay(duration);
            _delayTimer.ChangeDelay(_projectileData.Delay + duration);
            
            _attackTimer.StartTimer();
            _delayTimer.StartTimer();
            _isAttacking = true;
            //volumetricLineBehavior.UpdateLineScale();
        }

        public override void SetParent(Transform parent)
        {
            transform.SetParent(parent);
            transform.localPosition = Vector3.zero;
        }

        public override void ResetParent()
        {
          //  transform.parent = null;
        }

        private void Update()
        {
            if (_isAttacking)
            {
                if (_attackTimer.IsComplete)
                {
                    _isAttacking = false;
                    volumetricLineBehavior.SetStartAndEndPoints(Vector3.zero, Vector3.zero);
                    volumetricLineBehavior.UpdateBounds();
                    volumetricLineBehavior.UpdateLineScale();
                }
                else
                {
                    transform.LookAt(_hardPointView.Position);
                    _targetPosition.z = Vector3.Distance(_hardPointView.Position, transform.position);
                    volumetricLineBehavior.SetStartAndEndPoints(Vector3.zero, _targetPosition);
                }
            }
        }
    }
}