using System;
using EmpireAtWar.Models.Weapon;
using EmpireAtWar.Views.Ship;
using EmpireAtWar.Views.ShipUi;
using ScriptUtils.Math;
using UnityEngine;
using Utils.TimerService;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public class TurretView: MonoBehaviour, Views.ShipUi.IObserver<float>
    {
        [SerializeField] private ParticleSystem vfx;
        [SerializeField] private FloatRange yAxisRange;

        private INotifier<float> notifier;
        private ITimer attackTimer;
        private Vector3 targetPosition = Vector3.zero;
        private bool destroyed;

        private ShipView ShipView;


        private void Start()
        {
            ShipView = GetComponentInParent<ShipView>();
        }

        public bool IsBusy => !attackTimer.IsComplete && !destroyed;


        public float Distance { get; private set; }

        public bool CanAttack(Vector3 position)
        {
            vfx.transform.LookAt(position);
            float wrappedAngle = GetCorrectAngle(transform.localRotation.eulerAngles.y);
            if (!yAxisRange.IsInRange(wrappedAngle))
            {
                // Debug.Log($"{ShipView.name}, {name}: yAxisRange: {yAxisRange.Min}-> {yAxisRange.Max}, != {wrappedAngle}");
                return false;
            }

            return true;
        }

        public void SetData(ProjectileData projectileData, float duration)
        {
            var mainModule = vfx.main;
            
            mainModule.startColor = projectileData.Color;
            mainModule.startSize3D = true;
            mainModule.startSizeXMultiplier = projectileData.Size.x;
            mainModule.startSizeYMultiplier = projectileData.Size.y;
            mainModule.startSizeZMultiplier = projectileData.Size.z;
            mainModule.startLifetime = duration;
            mainModule.loop = false;
            mainModule.duration = duration + 0.1f;
            attackTimer = TimerFactory.ConstructTimer(mainModule.duration*2f);
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
            Distance = Vector3.Distance(targetPosition, transform.position);
            var mainModule = vfx.main;
            mainModule.startSpeed = Distance / mainModule.startLifetime.constant;
            this.targetPosition = targetPosition;
            attackTimer.StartTimer();
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
            vfx.transform.LookAt(targetPosition);
        }

        public void UpdateState(float value)
        {
            if (value < 0)
            {
                destroyed = true;
            }
        }
    }
}