using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Models.Health;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    /// <summary>One mount's bolts: every shot is drawn, since bolts are particles of a shared layer, not pooled objects.</summary>
    internal sealed class BoltShotVisuals : IShotVisuals
    {
        private readonly BoltFlightBatch _bolts;
        private readonly BoltShot _prefab;
        private readonly WeaponProfile _profile;
        private readonly Transform _owner;
        private readonly ImpactEffectPresenter _impacts;

        internal BoltShotVisuals(BoltFlightBatch bolts, BoltShot prefab, WeaponProfile profile, Transform owner,
            ImpactEffectPresenter impacts)
        {
            _bolts = bolts;
            _prefab = prefab;
            _profile = profile;
            _owner = owner;
            _impacts = impacts;
        }

        public void Play(AttackData attackData, IHardPointModel target, Vector3 aimOffset, bool isHit,
            IncomingMissile missile, float lateSeconds)
        {
            _bolts.Launch(_prefab, _profile, _owner, target.Transform, target.Pivot, aimOffset, attackData.TargetHealth,
                _impacts, _profile.Damage, ShotEffectPool.GetImpactSize(_profile, attackData.TargetClass), isHit,
                missile, lateSeconds);
        }

        // Bolts in flight belong to the batch and finish on their own.
        public void Release() { }
    }
}
