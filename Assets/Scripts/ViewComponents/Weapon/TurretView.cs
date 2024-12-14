using EmpireAtWar.Models.Weapon;
using Utilities.ScriptUtils.Math;
using UnityEngine;
using Utilities.ScriptUtils.Time;
using Random = UnityEngine.Random;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public class TurretView: MonoBehaviour
    {
        [SerializeField] private ParticleSystem vfx;
        [SerializeField] private FloatRange yAxisRange;

        private INotifier<float> notifier;
        private ITimer attackTimer;
        private Vector3 lookPosition = Vector3.zero;


        private float Distance { get; set; }
        private float MaxAttackDistance { get; set; }
        
        public bool IsBusy => vfx.isPlaying;

        public float Speed => vfx.main.startSpeed.constant;
        
        public FloatRange YAxisRange => yAxisRange;


        public void SetData(ProjectileData projectileData,  float attackDistance)
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

        public void Attack(Vector3 targetPosition, float duration)
        {
            var mainModule = vfx.main;
            // attackTimer.ChangeDelay(duration);
            // attackTimer.StartTimer();
            mainModule.startLifetime = duration;

      //      mainModule.duration = duration;
            lookPosition = targetPosition;
            vfx.Emit(1);
            vfx.Play();
        }
        
        private void Update()
        {
            transform.LookAt(lookPosition);
        }

        public void SetParent(Transform parent)
        {
            transform.SetParent(parent);
            transform.localPosition = Vector3.zero;
        }

        public void ResetParent()
        {
            transform.parent = null;
        }
    }
}