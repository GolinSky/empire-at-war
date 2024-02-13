using EmpireAtWar.Models.Weapon;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public class TurretView:MonoBehaviour
    {
        [SerializeField] private ParticleSystem vfx;
        
        private Vector3 targetPosition = Vector3.zero;

        public void SetData(ProjectileData projectileData, float duration)
        {
            var mainModule = vfx.main;
            
            mainModule.startColor = projectileData.Color;
            mainModule.startSize3D = true;
            mainModule.startSizeXMultiplier = projectileData.Size.x;
            mainModule.startSizeYMultiplier = projectileData.Size.y;
            mainModule.startSizeZMultiplier = projectileData.Size.z;
            mainModule.startLifetime = duration;
            mainModule.duration = duration + 0.1f;
        }
 
        public void Attack(Vector3 targetPosition)
        {
            float distance = Vector3.Distance(targetPosition, transform.position);
            var mainModule = vfx.main;
            mainModule.startSpeed = distance / mainModule.startLifetime.constant;
            this.targetPosition = targetPosition;
            vfx.Play();
        }

        private void Update()
        {
            vfx.transform.LookAt(targetPosition);
        }
    }
}