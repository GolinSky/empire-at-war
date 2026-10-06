using System;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Models.Health;
using UnityEngine;

namespace EmpireAtWar.Components.Weapon
{
    /// <summary>
    /// A missile or torpedo that will hit when it lands. Intercepting it cancels the scheduled damage and its flight visual.
    /// The flight visual reports where the missile is, so point defense aims at exactly what the player sees.
    /// </summary>
    public sealed class IncomingMissile
    {
        private readonly Vector3 _origin;
        private readonly float _impactTime;

        public event Action Intercepted;

        public IHealthModelObserver TargetHealth { get; }
        public DamageType DamageType { get; }
        public Vector3 Position { get; private set; }
        public bool IsIntercepted { get; private set; }
        public float DistanceTravelled => Vector3.Distance(_origin, Position);

        public IncomingMissile(IHealthModelObserver targetHealth, Vector3 origin, float launchTime, float travelTime,
            DamageType damageType = DamageType.ConcussionMissile)
        {
            TargetHealth = targetHealth;
            DamageType = damageType;
            _origin = origin;
            _impactTime = launchTime + travelTime;
            Position = origin;
        }

        public bool HasLanded(float now) => now >= _impactTime;

        public void ReportPosition(Vector3 position)
        {
            Position = position;
        }

        public void Intercept()
        {
            IsIntercepted = true;
            Intercepted?.Invoke();
        }
    }
}
