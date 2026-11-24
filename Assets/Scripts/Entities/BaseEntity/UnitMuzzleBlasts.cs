using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.UnitExplosion;
using UnityEngine;

namespace EmpireAtWar.Entities.BaseEntity
{
    /// <summary>
    /// Plays one muzzle blast per main-battery salvo, slightly above the mount and pushed towards the aim point, sized
    /// from the weapon's shot size and tinted with its shot color. Units hidden by fog play none.
    /// </summary>
    public sealed class UnitMuzzleBlasts : IInitializable, ILateDisposable
    {
        // Blast billboard diameter per unit of the weapon's shot width (WeaponProfile.Size.x).
        private const float DIAMETER_PER_SHOT_SIZE = 6f;
        // Offsets as fractions of the blast diameter: along the mount's up axis, and towards the aim point.
        private const float UP_OFFSET = 0.25f;
        private const float AIM_OFFSET = 0.15f;
        // Same white mix as the shot's streak flash (MuzzleFlashView), so blast and flash share one color.
        private const float WHITE_MIX = 0.35f;

        private readonly IWeaponFireEvents _fireEvents;
        private readonly IMuzzleBlastPool _pool;
        private readonly IFogVisibilityFacade _fogVisibility;

        public UnitMuzzleBlasts(IWeaponFireEvents fireEvents, IMuzzleBlastPool pool, IFogVisibilityFacade fogVisibility)
        {
            _fireEvents = fireEvents;
            _pool = pool;
            _fogVisibility = fogVisibility;
        }

        public void Initialize() => _fireEvents.MainBatterySalvoStarted += HandleSalvoStarted;

        public void LateDispose() => _fireEvents.MainBatterySalvoStarted -= HandleSalvoStarted;

        private void HandleSalvoStarted(WeaponProfile profile, Transform muzzle, Vector3 aimPoint)
        {
            if (_fogVisibility.IsHiddenByFog) return;
            float diameter = profile.Size.x * DIAMETER_PER_SHOT_SIZE;
            Vector3 aimDirection = (aimPoint - muzzle.position).normalized;
            Vector3 offset = (muzzle.up * UP_OFFSET + aimDirection * AIM_OFFSET) * diameter;
            _pool.Play(muzzle, offset, _fogVisibility, diameter, Color.Lerp(profile.Color, Color.white, WHITE_MIX));
        }
    }
}
