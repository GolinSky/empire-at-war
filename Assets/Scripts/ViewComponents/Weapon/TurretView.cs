using EmpireAtWar.Models.Weapon;
using ScriptUtils.Math;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public class TurretView:MonoBehaviour
    {
        [SerializeField] private ParticleSystem vfx;
        [SerializeField] private FloatRange yAxisRange;
        [SerializeField] private bool block;
        private Vector3 targetPosition = Vector3.zero;

        public bool IsBusy => vfx.isPlaying;

        public bool CanAttack(Vector3 position)
        {
            vfx.transform.LookAt(position);
            float wrappedAngle = GetCorrectAngle(transform.localRotation.eulerAngles.y);
            if (!yAxisRange.IsInRange(wrappedAngle))
            {
                block = true;
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
            mainModule.startDelay = duration;
            mainModule.duration = duration + 0.1f;
        }

        public void Attack(Vector3 targetPosition)
        {
           

            block = false;
            float distance = Vector3.Distance(targetPosition, transform.position);
            var mainModule = vfx.main;
            mainModule.startSpeed = distance / mainModule.startLifetime.constant;
            this.targetPosition = targetPosition;
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
    }
}