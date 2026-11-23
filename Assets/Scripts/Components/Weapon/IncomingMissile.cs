using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Health;
using UnityEngine;

namespace EmpireAtWar.Components.Weapon
{
    /// <summary>
    /// A missile or torpedo that will hit when it lands. Intercepting it cancels the scheduled damage and its flight visual.
    /// The missile flies straight from launch to aim point on its own, so point defense can shoot it down even when no
    /// visual was drawn; a drawn visual reports its arc instead, so point defense aims at exactly what the player sees.
    /// </summary>
    public sealed class IncomingMissile
    {
        private readonly Vector3 _origin;
        private readonly Vector3 _aimPoint;
        private readonly float _launchTime;
        private readonly float _travelTime;
        private readonly float _impactTime;
        private Vector3 _reportedPosition;
        private bool _hasReportedPosition;
        private List<object> _attemptedBy;

        public event Action Intercepted;

        public IHealthModelObserver TargetHealth { get; }
        public Vector3 Position => _hasReportedPosition ? _reportedPosition
            : Vector3.Lerp(_origin, _aimPoint, _travelTime > 0f ? Mathf.Clamp01((Time.time - _launchTime) / _travelTime) : 1f);
        public bool IsIntercepted { get; private set; }

        public IncomingMissile(IHealthModelObserver targetHealth, Vector3 origin, Vector3 aimPoint, float launchTime,
            float travelTime)
        {
            TargetHealth = targetHealth;
            _origin = origin;
            _aimPoint = aimPoint;
            _launchTime = launchTime;
            _travelTime = travelTime;
            _impactTime = launchTime + travelTime;
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
            _reportedPosition = position;
            _hasReportedPosition = true;
        }

        public void Intercept()
        {
            IsIntercepted = true;
            Intercepted?.Invoke();
        }
    }
}
