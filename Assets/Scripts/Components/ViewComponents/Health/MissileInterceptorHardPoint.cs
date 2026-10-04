using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Models.Health;
using UnityEngine;
using Utilities.ScriptUtils.Time;
using Zenject;

namespace EmpireAtWar.ViewComponents.Health
{
    /// <summary>
    /// Point-defense mount that shoots down enemy missiles and torpedoes heading for friendly ships in range.
    /// Coverage is lost while the mount is destroyed or ion-disabled.
    /// </summary>
    public class MissileInterceptorHardPoint : HardPoint
    {
        private const float LASER_FLASH_DURATION = 0.08f;

        private readonly ITimer _reloadTimer = TimerFactory.ConstructTimer();
        private readonly ITimer _laserTimer = TimerFactory.ConstructTimer();

        [SerializeField] private float range = 40f;
        [Tooltip("Seconds between interception attempts.")]
        [SerializeField] private float reload = 0.6f;
        [Tooltip("Chance (0..1) that one attempt destroys the missile.")]
        [Range(0f, 1f)]
        [SerializeField] private float interceptChance = 0.5f;
        [Tooltip("World-space line flashed from the mount to the missile on each attempt.")]
        [SerializeField] private LineRenderer laser;

        private IncomingMissileRegistry _missiles;
        private IHealthModelObserver _ownHealth;
        private CombatModifiers _modifiers;

        [Inject]
        private void Construct(IncomingMissileRegistry missiles, IHealthModelObserver ownHealth,
            CombatModifiers modifiers)
        {
            _missiles = missiles;
            _ownHealth = ownHealth;
            _modifiers = modifiers;
        }

        private void Update()
        {
            if (laser.enabled && _laserTimer.IsComplete) laser.enabled = false;
            if (IsDestroyed || _modifiers.IsIonDisabled || !_reloadTimer.IsComplete) return;
            if (!_missiles.TryFindThreat(_ownHealth.Owner, transform.position, range, out IncomingMissile missile))
                return;

            _reloadTimer.ChangeDelay(reload * _modifiers.FireDelayMultiplier).StartTimer();
            FlashLaser(missile.GetPosition(Time.time));
            if (Random.value < interceptChance) missile.Intercept();
        }

        private void FlashLaser(Vector3 target)
        {
            laser.SetPosition(0, transform.position);
            laser.SetPosition(1, target);
            laser.enabled = true;
            _laserTimer.ChangeDelay(LASER_FLASH_DURATION).StartTimer();
        }
    }
}
