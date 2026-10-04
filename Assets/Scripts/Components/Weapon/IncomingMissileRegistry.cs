using System.Collections.Generic;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Components.Weapon
{
    /// <summary>Battle-wide list of missiles in flight that point defense can still shoot down.</summary>
    public sealed class IncomingMissileRegistry : ITickable
    {
        private readonly List<IncomingMissile> _missiles = new List<IncomingMissile>();

        public IncomingMissile Launch(IHealthModelObserver targetHealth, IHardPointModel target, Vector3 origin,
            float travelTime)
        {
            IncomingMissile missile = new IncomingMissile(targetHealth, target, origin, Time.time, travelTime);
            _missiles.Add(missile);
            return missile;
        }

        /// <summary>Finds the nearest missile in range that is heading for a ship of <paramref name="owner"/>.</summary>
        public bool TryFindThreat(PlayerId owner, Vector3 position, float range, out IncomingMissile threat)
        {
            float now = Time.time;
            float bestDistance = range * range;
            threat = null;
            foreach (IncomingMissile missile in _missiles)
            {
                if (!IsInFlight(missile, now) || missile.TargetHealth.Owner != owner) continue;
                float distance = (missile.GetPosition(now) - position).sqrMagnitude;
                if (distance > bestDistance) continue;
                bestDistance = distance;
                threat = missile;
            }

            return threat != null;
        }

        public void Tick()
        {
            float now = Time.time;
            for (int i = _missiles.Count - 1; i >= 0; i--)
            {
                if (IsInFlight(_missiles[i], now)) continue;
                int last = _missiles.Count - 1;
                _missiles[i] = _missiles[last];
                _missiles.RemoveAt(last);
            }
        }

        private static bool IsInFlight(IncomingMissile missile, float now) =>
            !missile.IsIntercepted && !missile.HasLanded(now) && !missile.TargetHealth.IsDestroyed;
    }
}
