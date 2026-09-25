using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Models.Health;
using EmpireAtWar.ViewComponents.Weapon;
using UnityEngine;
using Utilities.ScriptUtils.Math;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EmpireAtWar.ViewComponents.Health
{
    public class WeaponHardPoint : HardPoint
    {
        [SerializeField] private FloatRange yAxisRange;
        [SerializeField] private int prewarmEffects;
        [field:SerializeField] public WeaponType WeaponType { get; private set; }

        private readonly AttackSequenceState _sequence = new AttackSequenceState();
        private ShotEffectPool _effectPool;
        private WeaponProfile _profile;
        private CombatAttackCoordinator _attackCoordinator;
        private CombatModifiers _modifiers;
        private ImpactEffectPresenter _impactPresenter;
        private float _maxAttackDistance;
        private float _missSpread;
        private float _readyTime;

        protected IWeaponPresenter WeaponPresenter { get; private set; }
        public bool Destroyed { get; private set; }
        public bool IsBusy => _sequence.IsBusy || Time.time < _readyTime;
        public float MaxAttackDistance => _maxAttackDistance;
        public float MinYaw => yAxisRange.Min;
        public float MaxYaw => yAxisRange.Max;

        public void SetData(FloatRange floatRange)
        {
            yAxisRange.SetValue(floatRange);
        }

        public void SetData(WeaponProfile profile, float maxAttackDistance, float missSpread,
            IWeaponPresenter weaponPresenter, CombatAttackCoordinator attackCoordinator, CombatModifiers modifiers,
            ImpactEffectPresenter impactPresenter)
        {
            _profile = profile;
            _maxAttackDistance = maxAttackDistance;
            _missSpread = missSpread;
            WeaponPresenter = weaponPresenter;
            _attackCoordinator = attackCoordinator;
            _modifiers = modifiers;
            _impactPresenter = impactPresenter;
        }

        public void ApplyAim(Quaternion worldRotation) => transform.rotation = worldRotation;

        public virtual void Attack(AttackData attackData, IHardPointModel hardPointModel)
        {
            _attackCoordinator.BeginSequence(WeaponPresenter, this, attackData, hardPointModel);
        }

        public void ReleaseAttackSequence()
        {
            if (_attackCoordinator != null)
                _attackCoordinator.CancelSequence(this, _sequence.Generation);

            if (_effectPool != null)
            {
                _effectPool.Release();
            }

            _sequence.Release();
        }

        private void OnDestroy()
        {
            ReleaseAttackSequence();
        }

        internal bool TryStartScheduledSequence(out int generation)
        {
            if (!_sequence.TryStart(out generation)) return false;
            _readyTime = Time.time + _profile.Reload * _modifiers.FireDelayMultiplier;
            return true;
        }

        internal int ShotsPerSalvo => _profile.ShotsPerSalvo;
        internal float DelayBetweenShots => _profile.ShotInterval;
        internal bool IsEmitting(int generation) => !IsDestroyed && _sequence.IsEmitting(generation);
        internal void StopEmitting(int generation) => _sequence.StopEmitting(generation);

        internal void EmitScheduledShot(AttackData attackData, IHardPointModel hardPointModel, int sequenceGeneration)
        {
            bool isHit = WeaponPresenter.RollHit(attackData, _profile);
            Vector3 aimOffset = isHit ? Vector3.zero : Random.onUnitSphere * _missSpread;
            float duration = GetPool().Play(attackData, hardPointModel, aimOffset, sequenceGeneration, isHit);

            if (!_sequence.RegisterEffect(sequenceGeneration))
            {
                return;
            }

            _sequence.RecordShotEmission(sequenceGeneration);
            if (isHit)
            {
                WeaponPresenter.ApplyDamage(attackData, hardPointModel, _profile, duration);
            }
        }

        private void OnTurretEffectCompleted(int sequenceGeneration)
        {
            _sequence.TryCompleteEffect(sequenceGeneration);
        }

        private ShotEffectPool GetPool()
        {
            if (_effectPool != null)
            {
                return _effectPool;
            }

            _effectPool = new ShotEffectPool(_profile, transform, _profile.ShotsPerSalvo,
                OnTurretEffectCompleted, _impactPresenter);
            _effectPool.Prewarm(prewarmEffects);
            return _effectPool;
        }

        protected override void OnStateUpdated(float healthPercentage)
        {
            base.OnStateUpdated(healthPercentage);
            if (healthPercentage <= 0f)
            {
                Destroyed = true;
                if (_attackCoordinator != null)
                    _attackCoordinator.CancelSequence(this, _sequence.Generation);
                _sequence.StopEmitting(_sequence.Generation);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            const float GIZMO_RADIUS = 2f;

            Quaternion parentRotation = transform.parent == null ? Quaternion.identity : transform.parent.rotation;
            Vector3 up = parentRotation * Vector3.up;
            Vector3 forward = parentRotation * Vector3.forward;
            float minYaw = Mathf.Clamp(MinYaw, -180f, 180f);
            float maxYaw = Mathf.Clamp(MaxYaw, -180f, 180f);
            if (maxYaw < minYaw)
            {
                return;
            }

            Vector3 startDirection = Quaternion.AngleAxis(minYaw, up) * forward;
            Vector3 endDirection = Quaternion.AngleAxis(maxYaw, up) * forward;
            Color previousColor = Handles.color;
            Handles.color = new Color(0.2f, 1f, 0.3f, 0.18f);
            Handles.DrawSolidArc(transform.position, up, startDirection, maxYaw - minYaw, GIZMO_RADIUS);
            Handles.color = new Color(0.2f, 1f, 0.3f, 0.9f);
            Handles.DrawWireArc(transform.position, up, startDirection, maxYaw - minYaw, GIZMO_RADIUS);
            Handles.DrawLine(transform.position, transform.position + startDirection * GIZMO_RADIUS);
            Handles.DrawLine(transform.position, transform.position + endDirection * GIZMO_RADIUS);
            Handles.color = previousColor;
        }

        public void SetWeaponType(WeaponType weaponType)
        {
            WeaponType = weaponType;
        }
#endif
    }
}
