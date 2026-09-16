using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.Timing;
using Utilities.ScriptUtils.Math;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public class TurretView:  BaseTurretView
    {
        [SerializeField] private ParticleSystem vfx;
        [SerializeField] private FloatRange yAxisRange;

        private Transform target;
        private INotifier<float> _notifier;
       
        private Vector3 _lookPosition = Vector3.zero;
        
        public float Speed => vfx.main.startSpeed.constant;
        
        public FloatRange YAxisRange => yAxisRange;
        

        public override void SetData(ProjectileData projectileData,  float attackDistance)
        {
            _projectileData = projectileData;
            var mainModule = vfx.main;
            
            mainModule.startColor = projectileData.Color;
            mainModule.startSize3D = true;
            mainModule.startSizeXMultiplier = projectileData.Size.x;
            mainModule.startSizeYMultiplier = projectileData.Size.y;
            mainModule.startSizeZMultiplier = projectileData.Size.z;
            mainModule.loop = false;
        }

        public override void Attack(IHardPointModel hardPointModel, out float duration)
        {
            float distance = Vector3.Distance(hardPointModel.Position, transform.position);
            duration = distance / Speed;
            BeginLease(_projectileData.Delay + duration);
            var mainModule = vfx.main;
    
            mainModule.startLifetime = duration;

            _lookPosition = hardPointModel.Position;
            transform.LookAt(_lookPosition);
            target = hardPointModel.Transform;
            vfx.Emit(1);
            vfx.Play();
            
            _attackTimer
                .ChangeDelay(duration)
                .StartTimer();
        }
        
        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.ProjectileTurretUpdate.Auto())
            {
#endif
            if (IsBusy)
            {
                if (target != null)
                {
                    transform.LookAt(target);
                }
            }
            UpdateLeaseCompletion();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            }
#endif
            ///transform.LookAt(_lookPosition);
        }

        public override void SetParent(Transform parent)
        {
            transform.SetParent(parent);
            transform.localPosition = Vector3.zero;
        }

        public override void ResetParent()
        {
            transform.parent = null;
        }

        protected override void OnLeaseCompleted()
        {
            target = null;
        }

        protected override bool IsVisualComplete() => !vfx.IsAlive(true);
    }
}
