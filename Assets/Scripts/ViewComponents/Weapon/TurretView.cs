using EmpireAtWar.Models.Weapon;
using EmpireAtWar.ViewComponents.Health;
using Utilities.ScriptUtils.Math;
using UnityEngine;
using Utilities.ScriptUtils.Time;
using Random = UnityEngine.Random;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public class TurretView:  BaseTurretView
    {
        [SerializeField] private ParticleSystem vfx;
        [SerializeField] private FloatRange yAxisRange;

        private INotifier<float> notifier;
        private ITimer attackTimer;
        private Vector3 lookPosition = Vector3.zero;


        private float Distance { get; set; }
        private float MaxAttackDistance { get; set; }
        
        public override bool IsBusy => vfx.isPlaying;

        public override float Speed => vfx.main.startSpeed.constant;
        
        public FloatRange YAxisRange => yAxisRange;


        public override void SetData(ProjectileData projectileData,  float attackDistance)
        {
            MaxAttackDistance = attackDistance;
            var mainModule = vfx.main;
            
            mainModule.startColor = projectileData.Color;
            // mainModule.startSize3D = true;
            // mainModule.startSizeXMultiplier = projectileData.Size.x;
            // mainModule.startSizeYMultiplier = projectileData.Size.y;
            // mainModule.startSizeZMultiplier = projectileData.Size.z;
            mainModule.loop = false;
         //   mainModule.duration = duration + 0.1f;
         //   attackTimer = TimerFactory.ConstructTimer(mainModule.duration + Random.Range(1f, 3f));
        }

        public override void Attack(IHardPointView hardPointView, float duration)
        {
            var mainModule = vfx.main;
            // attackTimer.ChangeDelay(duration);
            // attackTimer.StartTimer();
            mainModule.startLifetime = duration;

      //      mainModule.duration = duration;
            lookPosition = hardPointView.Position;
            vfx.Emit(1);
            vfx.Play();
        }
        
        private void Update()
        {
            transform.LookAt(lookPosition);
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