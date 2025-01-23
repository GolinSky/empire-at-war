using EmpireAtWar.ViewComponents.Health;
using UnityEngine;
using VolumetricLines;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public class LaserTurretView : BaseTurretView
    {
        [SerializeField] private VolumetricLineBehavior volumetricLineBehavior;

        public override float Speed => 100f;

        private bool _isAttacking;
        private Vector3 _targetPosition = Vector3.zero;
        


        public override void Attack(IHardPointView hardPointView, float duration)
        {
            _hardPointView = hardPointView;
           
            _targetPosition.z = Vector3.Distance(hardPointView.Position, transform.position);
            volumetricLineBehavior.SetStartAndEndPoints(Vector3.zero, _targetPosition);


            _attackTimer
                .ChangeDelay(duration)
                .StartTimer();
            
            _delayTimer
                .ChangeDelay(_projectileData.Delay + duration)
                .StartTimer();
            
            _isAttacking = true;
        }

        public override void SetParent(Transform parent)
        {
            transform.SetParent(parent);
            transform.localPosition = Vector3.zero;
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