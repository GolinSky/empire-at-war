using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Services.Timing;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    /// <summary>Laser / turbolaser bolt: one particle flying from the muzzle to the aim point.</summary>
    public class BoltShot : ShotEffect
    {
        [SerializeField] private ParticleSystem vfx;

        private Transform _target;
        private Vector3 _aimOffset;

        protected override float Play(Transform muzzle, Transform target, Vector3 aimOffset, WeaponProfile profile)
        {
            _target = target;
            _aimOffset = aimOffset;
            Vector3 aimPoint = target.position + aimOffset;
            float travelTime = Vector3.Distance(muzzle.position, aimPoint) / profile.ProjectileSpeed;
            transform.SetPositionAndRotation(muzzle.position, Quaternion.LookRotation(aimPoint - muzzle.position));

            ParticleSystem.MainModule main = vfx.main;
            main.loop = false;
            main.startColor = profile.Color;
            main.startSize3D = true;
            main.startSizeXMultiplier = profile.Size.x;
            main.startSizeYMultiplier = profile.Size.y;
            main.startSizeZMultiplier = profile.Size.z;
            main.startSpeed = profile.ProjectileSpeed;
            main.startLifetime = travelTime;
            vfx.Emit(1);
            vfx.Play();
            return travelTime;
        }

        protected override void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.ProjectileTurretUpdate.Auto())
            {
#endif
            // Local-space particles follow the target so the bolt lands where the impact is applied.
            if (_target != null)
            {
                transform.LookAt(_target.position + _aimOffset);
            }

            base.Update();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            }
#endif
        }

        protected override void OnLeaseCompleted()
        {
            _target = null;
        }

        protected override bool IsVisualComplete() => !vfx.IsAlive(true);
    }
}
