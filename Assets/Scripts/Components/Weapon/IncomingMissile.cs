using System;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Models.Health;
using UnityEngine;

namespace EmpireAtWar.Components.Weapon
{
    /// <summary>
    /// A missile or torpedo that will hit when it lands. Intercepting it cancels the scheduled damage and its flight visual.
    /// </summary>
    public sealed class IncomingMissile
    {
        private readonly IHardPointModel _target;
        private readonly Vector3 _origin;
        private readonly float _launchTime;
        private readonly float _impactTime;

        public event Action Intercepted;

        public IHealthModelObserver TargetHealth { get; }
        public DamageType DamageType { get; }
        public float DistanceTravelled => Vector3.Distance(_origin, GetPosition(Time.time));
        public bool IsIntercepted { get; private set; }

        public IncomingMissile(IHealthModelObserver targetHealth, IHardPointModel target, Vector3 origin,
            float launchTime, float travelTime, DamageType damageType = DamageType.ConcussionMissile)
        {
            TargetHealth = targetHealth;
            DamageType = damageType;
            _target = target;
            _origin = origin;
            _launchTime = launchTime;
            _impactTime = launchTime + travelTime;
        }

        public bool HasLanded(float now) => now >= _impactTime;

        // Straight-line estimate: the visual arcs slightly, which is close enough for a point-defense range check.
        public Vector3 GetPosition(float now) =>
            Vector3.Lerp(_origin, _target.Position, Mathf.InverseLerp(_launchTime, _impactTime, now));

        public void Intercept()
        {
            IsIntercepted = true;
            Intercepted?.Invoke();
        }
    }
}
