using EmpireAtWar.Models.Weapon;
using EmpireAtWar.Views.ShipUi;
using Utilities.ScriptUtils.Math;
using UnityEngine;
using Utils.TimerService;
using Random = UnityEngine.Random;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public class TurretView: MonoBehaviour, Views.ShipUi.IObserver<float>
    {
        [SerializeField] private ParticleSystem vfx;
        [SerializeField] private FloatRange yAxisRange;

        private INotifier<float> notifier;
        private ITimer attackTimer;
        private Vector3 lookPosition = Vector3.zero;


        private float Distance { get; set; }
        private float MaxAttackDistance { get; set; }
        
        public bool IsBusy => vfx.isPlaying || !attackTimer.IsComplete;
        public bool Destroyed { get; private set; }
        

        public bool CanAttack(Vector3 targetPosition)
        {
            float distance = Vector3.Distance(targetPosition, transform.position);
            if (distance > MaxAttackDistance) return false;
            
            Vector3 direction = targetPosition - transform.position;

            Quaternion lookRotation = Quaternion.LookRotation(direction, Vector3.up);

            transform.rotation = lookRotation;
            
            if (!yAxisRange.IsInRange(GetCorrectAngle(transform.localEulerAngles.y)))
            {
                // Debug.Log($"{ShipView.name}, {name}: yAxisRange: {yAxisRange.Min}-> {yAxisRange.Max}, != {wrappedAngle}");
                return false;
            }

            return true;
        }


        public void SetData(ProjectileData projectileData, float duration, float attackDistance)
        {
            MaxAttackDistance = attackDistance;
            var mainModule = vfx.main;
            
            mainModule.startColor = projectileData.Color;
            mainModule.startSize3D = true;
            mainModule.startSizeXMultiplier = projectileData.Size.x;
            mainModule.startSizeYMultiplier = projectileData.Size.y;
            mainModule.startSizeZMultiplier = projectileData.Size.z;
            mainModule.startLifetime = duration;
            mainModule.loop = false;
            mainModule.duration = duration + 0.1f;
            attackTimer = TimerFactory.ConstructTimer(mainModule.duration + Random.Range(1f, 3f));
            
            notifier = GetComponent<INotifier<float>>();
            notifier.AddObserver(this);
        }

        private void OnDestroy()
        {
            if (notifier != null)
            {
                notifier.RemoveObserver(this);
            }
        }

        public void Attack(Vector3 targetPosition)
        {
            attackTimer.StartTimer();
            Distance = Vector3.Distance(targetPosition, transform.position);
            var mainModule = vfx.main;
            mainModule.startSpeed = Distance / mainModule.startLifetime.constant;
            lookPosition = targetPosition;
            vfx.Play();
        }

        private float GetCorrectAngle(float y)
        {
            if(y > 180)
            {
                return y - 360;
            }
            else
            {
                return y;
            }
        }
        
        private void Update()
        {
            transform.LookAt(lookPosition);
        }

        public void UpdateState(float value)
        {
            if (value <= 0f)
            {
                Destroyed = true;
            }
        }
    }
}