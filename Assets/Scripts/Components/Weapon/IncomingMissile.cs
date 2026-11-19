using System;
using System.Collections.Generic;
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
        private readonly float _impactTime;
        private List<object> _attemptedBy;

        public event Action Intercepted;

        public IHealthModelObserver TargetHealth { get; }
        public Vector3 Position { get; private set; }
        public bool IsIntercepted { get; private set; }

        public IncomingMissile(IHealthModelObserver targetHealth, Vector3 origin, float launchTime, float travelTime)
        {
            TargetHealth = targetHealth;
            _impactTime = launchTime + travelTime;
            Position = origin;
        }

        public bool HasLanded(float now) => now >= _impactTime;

        /// <summary>Each point-defense mount gets one interception roll per missile.</summary>
        public bool WasAttemptedBy(object interceptor) => _attemptedBy != null && _attemptedBy.Contains(interceptor);

        public void RecordAttempt(object interceptor)
        {
            _attemptedBy ??= new List<object>(2);
            _attemptedBy.Add(interceptor);
        }

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
