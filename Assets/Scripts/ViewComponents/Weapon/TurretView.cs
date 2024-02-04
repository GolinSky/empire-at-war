using EmpireAtWar.Models.Weapon;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public class TurretView:MonoBehaviour
    {
        [SerializeField] private ParticleSystem vfx;

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

        public void Attack(Vector3 position)
        {
            float distance = Vector3.Distance(position, transform.position);
            var mainModule = vfx.main;
            mainModule.startLifetime = distance / mainModule.startSpeedMultiplier;
            vfx.transform.LookAt(position);
            vfx.Play();
        }
    }
}