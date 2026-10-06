using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.Models.Health;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.Squadrons
{
    public sealed class SeekerWarheadCountermeasure : IInitializable, ITickable
    {
        private readonly SquadronData _data;
        private readonly IHealthModelObserver _health;
        private readonly IncomingMissileRegistry _missiles;
        private readonly CombatModifiers _modifiers;
        private IHardPointModel[] _fighters;
        private float[] _readyTimes;

        public SeekerWarheadCountermeasure(SquadronData data, IHealthModelObserver health,
            IncomingMissileRegistry missiles, CombatModifiers modifiers)
        {
            _data = data;
            _health = health;
            _missiles = missiles;
            _modifiers = modifiers;
        }

        public void Initialize()
        {
            _fighters = _health.GetShipUnits(HardPointType.Any);
            _readyTimes = new float[_fighters.Length];
        }

        public void Tick()
        {
            if (_health.IsDestroyed || _modifiers.IsIonDisabled || _modifiers.IsCloaked) return;
            float now = Time.time;
            for (int i = 0; i < _fighters.Length; i++)
            {
                IHardPointModel fighter = _fighters[i];
                if (fighter.IsDestroyed || now < _readyTimes[i]) continue;
                if (!_missiles.TryFindThreat(_health.Owner, fighter.Position, _data.SeekerWarheadRange,
                    out IncomingMissile threat, DamageType.ConcussionMissile, _data.SeekerWarheadMinimumTravel)) continue;
                threat.Intercept();
                _readyTimes[i] = now + _data.SeekerWarheadRecharge;
            }
        }
    }
}
