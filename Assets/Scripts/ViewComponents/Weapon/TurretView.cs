using System;
using EmpireAtWar.Models.Weapon;
using EmpireAtWar.ViewComponents.Health;
using Utilities.ScriptUtils.Math;
using UnityEngine;
using Utilities.ScriptUtils.Time;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public class TurretView:  BaseTurretView
    {
        [SerializeField] private ParticleSystem vfx;
        [SerializeField] private FloatRange yAxisRange;

        private INotifier<float> _notifier;
        private readonly ITimer _attackTimer = TimerFactory.ConstructTimer();
        private readonly ITimer _delayTimer = TimerFactory.ConstructTimer();
        private Vector3 _lookPosition = Vector3.zero;
        private ProjectileData _projectileData;


        private float Distance { get; set; }

        public override bool IsBusy => !_delayTimer.IsComplete;

        public override float Speed => vfx.main.startSpeed.constant;
        
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

        public override void Attack(IHardPointView hardPointView, float duration)
        {
            var mainModule = vfx.main;
    
            mainModule.startLifetime = duration;

      //      mainModule.duration = duration;
            _lookPosition = hardPointView.Position;
            vfx.Emit(1);
            vfx.Play();
            
            _attackTimer.ChangeDelay(duration);
            _delayTimer.ChangeDelay(_projectileData.Delay + duration);
            _attackTimer.StartTimer();
            _delayTimer.StartTimer();
        }
        
        private void Update()
        {
            transform.LookAt(_lookPosition);
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
    }
}