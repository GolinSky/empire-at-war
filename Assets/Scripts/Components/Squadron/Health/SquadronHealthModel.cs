using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Components.Squadrons.Health
{
    /// <summary>
    /// Every fighter has its own hull and shields; a hit only damages the fighter it lands on.
    /// The squadron is destroyed when its last fighter dies.
    /// </summary>
    public sealed class SquadronHealthModel : PureModel
    {
        private readonly ISquadronHealthData _data;
        private readonly DamageMatrixData _damageMatrix;
        private readonly CombatModifiers _modifiers;
        private float[] _shields = Array.Empty<float>();

        public event Action OnValueChanged;
        public event Action OnDestroy;

        public HardPointModel[] Members { get; private set; } = Array.Empty<HardPointModel>();
        public ShipClass ShipClass => _data.ShipClass;
        public float Hull { get; private set; }
        public float Shields { get; private set; }
        public float HullPercentage => MaxHull <= 0f ? 0f : Hull / MaxHull;
        public float ShieldPercentage => MaxShields <= 0f ? 0f : Shields / MaxShields;
        public float ShieldRegenerateValue => _data.ShieldRegenerateValue;
        public float ShieldRegenerateDelay => _data.ShieldRegenerateDelay;
        public bool IsDestroyed { get; private set; }
        public bool HasShields => Shields > 0f;
        public bool HasUnits => !IsDestroyed && Members.Length > 0;
        public bool HasLiveHardPoints => AliveCount > 0;
        public int AliveCount { get; private set; }
        private float MaxHull => _data.MemberHull * Members.Length;
        private float MaxShields => _data.MemberShields * Members.Length;

        public SquadronHealthModel(ISquadronHealthData data, DamageMatrixData damageMatrix,
            CombatModifiers modifiers)
        {
            _data = data;
            _damageMatrix = damageMatrix;
            _modifiers = modifiers;
        }

        public void InitializeMembers(IReadOnlyList<HardPointModel> members)
        {
            Members = new HardPointModel[members.Count];
            _shields = new float[members.Count];
            for (int i = 0; i < members.Count; i++)
            {
                Members[i] = members[i];
                Members[i].SetHealth(_data.MemberHull, 1f);
                _shields[i] = _data.MemberShields;
            }

            Recalculate();
        }

        public float GetMemberShields(int memberId) => _shields[memberId];

        public void ApplyDamage(float damage, DamageType damageType, int memberId)
        {
            HardPointModel member = Members[memberId];
            if (IsDestroyed || member.IsDestroyed)
            {
                return;
            }

            damage *= _modifiers.DamageTakenMultiplier;
            if (_shields[memberId] > 0f && !_damageMatrix.IsShieldPiercing(damageType))
            {
                _shields[memberId] = Math.Max(0f,
                    _shields[memberId] - damage * _damageMatrix.GetShieldMultiplier(damageType));
            }
            else
            {
                member.ApplyDamage(damage * _damageMatrix.GetDamageMultiplier(damageType, ShipClass));
                if (member.IsDestroyed)
                {
                    _shields[memberId] = 0f;
                }
            }

            Recalculate();
            OnValueChanged?.Invoke();
            if (AliveCount == 0)
            {
                IsDestroyed = true;
                OnDestroy?.Invoke();
            }
        }

        public void RegenerateShields(float value)
        {
            for (int i = 0; i < Members.Length; i++)
            {
                if (!Members[i].IsDestroyed)
                {
                    _shields[i] = Math.Min(_data.MemberShields, _shields[i] + value);
                }
            }

            Recalculate();
            OnValueChanged?.Invoke();
        }

        private void Recalculate()
        {
            float hull = 0f;
            float shields = 0f;
            int alive = 0;
            for (int i = 0; i < Members.Length; i++)
            {
                if (Members[i].IsDestroyed) continue;
                hull += Members[i].Health;
                shields += _shields[i];
                alive++;
            }

            Hull = hull;
            Shields = shields;
            AliveCount = alive;
        }
    }
}
