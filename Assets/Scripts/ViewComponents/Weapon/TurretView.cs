using EmpireAtWar.Models.Weapon;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public class TurretView:MonoBehaviour
    {
        [SerializeField] private ParticleSystem vfx;
        
        private Vector3 targetPosition = Vector3.zero;

        public void SetData(ProjectileData projectileData, float speed)
        {
            var mainModule = vfx.main;

            mainModule.startColor = projectileData.Color;
            mainModule.startSize3D = true;
            mainModule.startSizeXMultiplier = projectileData.Size.x;
            mainModule.startSizeYMultiplier = projectileData.Size.y;
            mainModule.startSizeZMultiplier = projectileData.Size.z;
            mainModule.startSpeed = speed;
        }
 
        public void Attack(Vector3 targetPosition)
        {
            float distance = Vector3.Distance(targetPosition, transform.position);
            var mainModule = vfx.main;
            mainModule.startLifetime = distance / mainModule.startSpeedMultiplier;
            this.targetPosition = targetPosition;
            vfx.Play();
        }

        private void Update()
        {
            vfx.transform.LookAt(targetPosition);
        }
    }
}