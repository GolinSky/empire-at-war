using System.Collections.Generic;

namespace EmpireAtWar.Entities.CinematicCamera.Model
{
    public class CinematicActivityTracker
    {
        private readonly Dictionary<long, float> _lastHealth = new();
        private readonly Dictionary<long, float> _lastDamageTime = new();

        public void Sample(long entityId, float health, float time)
        {
            if (_lastHealth.TryGetValue(entityId, out float previousHealth) && health < previousHealth)
            {
                _lastDamageTime[entityId] = time;
            }

            _lastHealth[entityId] = health;
        }

        public float GetSecondsSinceDamaged(long entityId, float time)
        {
            return _lastDamageTime.TryGetValue(entityId, out float damageTime)
                ? time - damageTime
                : float.PositiveInfinity;
        }

        public void Clear()
        {
            _lastHealth.Clear();
            _lastDamageTime.Clear();
        }
    }
}
