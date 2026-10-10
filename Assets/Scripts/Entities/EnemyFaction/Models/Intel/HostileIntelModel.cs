using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.EnemyFaction.Models.Intel
{
    /// <summary>
    /// One team's knowledge of hostile units. Every AI on the team reports what the team sees and reads the same
    /// records, so allies share intel. A newer report replaces an older one; old reports fade by
    /// <see cref="IntelConfidence"/> and are dropped once forgotten.
    /// </summary>
    public sealed class HostileIntelModel : Model
    {
        private readonly Dictionary<long, HostileSighting> _sightings = new Dictionary<long, HostileSighting>();
        private readonly List<long> _expired = new List<long>();

        public IReadOnlyCollection<HostileSighting> Sightings => _sightings.Values;

        public void Report(HostileSighting sighting)
        {
            if (_sightings.TryGetValue(sighting.EntityId, out HostileSighting known) &&
                known.SeenAt > sighting.SeenAt)
            {
                return;
            }

            _sightings[sighting.EntityId] = sighting;
        }

        public void Forget(long entityId)
        {
            _sightings.Remove(entityId);
        }

        public float GetConfidence(HostileSighting sighting, float now) =>
            IntelConfidence.Evaluate(now - sighting.SeenAt);

        public void ForgetExpired(float now)
        {
            _expired.Clear();
            foreach (HostileSighting sighting in _sightings.Values)
            {
                if (GetConfidence(sighting, now) <= 0f)
                {
                    _expired.Add(sighting.EntityId);
                }
            }

            foreach (long entityId in _expired)
            {
                _sightings.Remove(entityId);
            }
        }

        /// <summary>
        /// Where a scout learns the most: the last position of the record whose refresh is worth most,
        /// its hull and shields times how much trust it has lost. Fresh records need no scout.
        /// </summary>
        public bool TryGetScoutTarget(float now, out FormationPoint position)
        {
            position = default;
            float bestValue = 0f;
            foreach (HostileSighting sighting in _sightings.Values)
            {
                if (now - sighting.SeenAt <= IntelConfidence.FRESH_AGE)
                {
                    continue;
                }

                float value = (sighting.Hull + sighting.Shields) * (1f - GetConfidence(sighting, now));
                if (value > bestValue)
                {
                    bestValue = value;
                    position = sighting.Position;
                }
            }

            return bestValue > 0f;
        }
    }
}
