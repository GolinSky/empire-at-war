using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Models.Health;
using EmpireAtWar.ViewComponents.Weapon;
using UnityEngine;
using Utilities.ScriptUtils.Math;
using Utilities.ScriptUtils.Time;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EmpireAtWar.ViewComponents.Health
{
    public class WeaponHardPoint : HardPoint
    {
        private readonly ITimer _reloadTimer = TimerFactory.ConstructTimer();

        [SerializeField] private FloatRange yAxisRange = new FloatRange();
        [SerializeField] private bool mainBattery;
        private readonly AttackSequenceState _sequence = new AttackSequenceState();
        private IShotVisuals _visuals;
        private WeaponProfile _profile;
        private CombatAttackCoordinator _attackCoordinator;
        private CombatModifiers _modifiers;
        private ImpactEffectPresenter _impactPresenter;
        private ShotPoolPrewarmer _poolPrewarmer;
        private IncomingMissileRegistry _missiles;
        private bool _strikecraftOwner;
        private float _reloadMultiplier = 1f;
        private float _intervalMultiplier = 1f;

        private float _maxAttackDistance;
        private float _missSpread;
        private HitAccumulator _hits;
        // Local yaw the mount points at, inside the firing arc. Turning has no visual; it only delays the salvo.
        private float _mountYaw;
        private float _turnEndTime = float.NegativeInfinity;

        public event System.Action<WeaponProfile, Transform, float> ShotEmitted;
        public event System.Action<AttackData> AttackEmitted;
        internal event System.Action DestructionChanged;

        [field:SerializeField] public WeaponType WeaponType { get; private set; }
        protected IWeaponPresenter WeaponPresenter { get; private set; }
        public bool Destroyed { get; private set; }
        public bool IsBusy => _sequence.IsBusy || !_reloadTimer.IsComplete || Time.time < _turnEndTime;
        public float MaxAttackDistance => _maxAttackDistance * _modifiers.VisionMultiplier;
        public float MinYaw => yAxisRange.Min;
        public float MaxYaw => yAxisRange.Max;
        internal bool IsMainBattery => mainBattery;
        internal int ShotsPerSalvo => _profile.ShotsPerSalvo;
        internal bool StrikecraftOnly => _profile.StrikecraftOnly;
        internal virtual bool ShipsOnly => _profile.DamageType == DamageType.Ion || _profile.DamageType == DamageType.IonCannon;
        /// <summary>False while the mount would refuse any salvo; it then skips target selection.</summary>
        internal virtual bool CanStartSalvo => true;
        internal TargetPreference PreferredTarget => _profile.PreferredTarget;
        internal float DelayBetweenShots => _profile.ShotInterval * GetTimingMultipliers().interval;
        internal float PendingShotIntervalRatio { get; private set; } = 1f;

        private (float reload, float interval) GetTimingMultipliers()
        {
            float shared = _modifiers.GetFullSalvoFireDelayMultiplier(_profile.Interceptable) *
                _modifiers.GetTorpedoFireDelayMultiplier(_profile.Family == WeaponFamily.Torpedo) *
                _modifiers.GetMainBatteryFireDelayMultiplier(mainBattery);
            return (shared * _modifiers.FireDelayMultiplier,
                shared * _modifiers.WeaponPowerShotIntervalMultiplier * _modifiers.ShotIntervalMultiplier);
        }

        internal bool RefreshTiming()
        {
            var timing = GetTimingMultipliers();
            PendingShotIntervalRatio = timing.interval / _intervalMultiplier;
            bool changed = timing.reload != _reloadMultiplier || timing.interval != _intervalMultiplier;
            if (!changed) return false;
            _reloadTimer.AppendTime(_reloadTimer.TimeLeft * (timing.reload / _reloadMultiplier - 1f));
            _reloadMultiplier = timing.reload;
            _intervalMultiplier = timing.interval;
            return true;
        }

        internal bool CanEngage(ShipClass targetClass) =>
            (!StrikecraftOnly || targetClass.IsStrikecraft()) && (!ShipsOnly || !targetClass.IsStrikecraft());

        internal bool CanContributeToFacing(ShipClass targetClass, CombatModifiers modifiers) =>
            !IsDestroyed && modifiers.CanFireWeapon(mainBattery) && CanEngage(targetClass);

        public override bool TryGetWeaponType(out WeaponType weaponType)
        {
            weaponType = WeaponType;
            return true;
        }

        public override bool TryGetWeaponProfile(out WeaponProfile profile)
        {
            profile = _profile;
            return true;
        }

        public void SetData(FloatRange floatRange)
        {
            yAxisRange.SetValue(floatRange);
        }

        public void SetData(WeaponProfile profile, float maxAttackDistance, float missSpread,
            IWeaponPresenter weaponPresenter, CombatAttackCoordinator attackCoordinator, CombatModifiers modifiers,
            ImpactEffectPresenter impactPresenter, IncomingMissileRegistry missiles, ShotPoolPrewarmer poolPrewarmer,
            bool strikecraftOwner = false)
        {
            _strikecraftOwner = strikecraftOwner;
            _poolPrewarmer = poolPrewarmer;
            _profile = profile;
            _maxAttackDistance = maxAttackDistance;
            _missSpread = missSpread;
            _hits = new HitAccumulator(Random.value);
            WeaponPresenter = weaponPresenter;
            _attackCoordinator = attackCoordinator;
            _modifiers = modifiers;
            (_reloadMultiplier, _intervalMultiplier) = GetTimingMultipliers();
            PendingShotIntervalRatio = 1f;
            _impactPresenter = impactPresenter;
            _missiles = missiles;
            _mountYaw = (MinYaw + MaxYaw) * 0.5f;
        }

        /// <summary>
        /// Turns the mount towards the target and returns how many seconds the turn takes; the hardpoint stays
        /// busy until then. Weapons without a turn rate return 0.
        /// </summary>
        internal float TurnTowards(Vector3 targetPosition)
        {
            if (_profile.TurnRate <= 0f) return 0f;
            Quaternion parentRotation = transform.parent == null ? Quaternion.identity : transform.parent.rotation;
            Quaternion aim = Quaternion.LookRotation(targetPosition - transform.position, Vector3.up);
            float targetYaw = Mathf.Clamp(
                Mathf.DeltaAngle(0f, (Quaternion.Inverse(parentRotation) * aim).eulerAngles.y), MinYaw, MaxYaw);
            // Full-circle mounts cross the rear seam; limited arcs must turn around their dead zone.
            float yawDelta = MinYaw <= -180f && MaxYaw >= 180f
                ? Mathf.DeltaAngle(_mountYaw, targetYaw)
                : targetYaw - _mountYaw;
            float duration = Mathf.Abs(yawDelta) / _profile.TurnRate;
            _mountYaw = targetYaw;
            _turnEndTime = Time.time + duration;
            return duration;
        }

        public virtual void Attack(AttackData attackData, IHardPointModel hardPointModel)
        {
            TryAttack(attackData, hardPointModel);
        }

        protected bool TryAttack(AttackData attackData, IHardPointModel hardPointModel) =>
            CanEngage(attackData.TargetClass) &&
            _attackCoordinator.BeginSequence(WeaponPresenter, this, attackData, hardPointModel);

        public void ReleaseAttackSequence()
        {
            if (_attackCoordinator != null)
                _attackCoordinator.CancelSequence(this, _sequence.Generation);

            if (_visuals != null)
            {
                _visuals.Release();
            }

            _sequence.Release();
        }

        /// <summary>Lets a released hardpoint fire again when its squadron is reused.</summary>
        public virtual void RestartAttackSequence()
        {
            _visuals = null;
            _sequence.Restart();
            _reloadTimer.ForceFinish();
            _turnEndTime = float.NegativeInfinity;
            _mountYaw = (MinYaw + MaxYaw) * 0.5f;
        }

        private void OnDestroy()
        {
            ReleaseAttackSequence();
        }

        internal bool TryStartScheduledSequence(out int generation)
        {
            generation = _sequence.Generation;
            if (_sequence.CurrentStatus != AttackSequenceState.Status.Ready) return false;
            if (_modifiers.IsIonDisabled || _modifiers.IsCloaked || !_modifiers.CanFireWeapon(mainBattery)) return false;
            if (!_sequence.TryStart(out generation)) return false;
            _reloadTimer.ChangeDelay(_profile.Reload * GetTimingMultipliers().reload).StartTimer();
            return true;
        }

        internal bool IsEmitting(int generation) => !IsDestroyed && !_modifiers.IsIonDisabled && !_modifiers.IsCloaked &&
            _modifiers.CanFireWeapon(mainBattery) && _sequence.IsEmitting(generation);

        internal void StopEmitting(int generation)
        {
            _sequence.StopEmitting(generation);
        }

        /// <summary>
        /// Fires one shot. Combat times it from the shot prefab's flight rule and lands it on schedule; drawing it is
        /// best effort, so a shot with no free visual still fires, hits and can be intercepted.
        /// </summary>
        internal void EmitScheduledShot(AttackData attackData, IHardPointModel hardPointModel, int sequenceGeneration,
            float lateSeconds = 0f)
        {
            if (!_sequence.IsEmitting(sequenceGeneration))
            {
                AttackSequenceDiagnostics.RecordUnmatchedCompletion();
                return;
            }

            bool isHit = _hits.Roll(WeaponPresenter.GetHitChance(attackData, _profile), Random.value);
            Vector3 aimOffset = isHit ? Vector3.zero : Random.onUnitSphere * _missSpread;
            Vector3 muzzle = transform.position;
            Vector3 aimPoint = hardPointModel.Transform.position + aimOffset;
            if (isHit)
                aimPoint = _impactPresenter.GetImpactPosition(attackData.TargetHealth, _profile.DamageType, muzzle, aimPoint);
            ShotEffect shot = _profile.ShotPrefab;
            float duration = shot.GetFlightSeconds(muzzle, aimPoint, _profile, lateSeconds);
            IncomingMissile missile = isHit && _profile.Interceptable
                ? _missiles.Launch(attackData.TargetHealth, muzzle, aimPoint, duration)
                : null;
            GetVisuals().Play(attackData, hardPointModel, aimOffset, isHit, missile, lateSeconds);

            _sequence.RecordShotEmission(sequenceGeneration);
            if (AttackEmitted != null) AttackEmitted.Invoke(attackData);
            if (ShotEmitted != null) ShotEmitted.Invoke(_profile, transform, DelayBetweenShots);
            if (isHit)
                WeaponPresenter.ApplyDamage(attackData, hardPointModel, _profile, duration, shot.DamageDuration, missile);
        }

        internal void PrepareEffects() => GetVisuals();

        private IShotVisuals GetVisuals()
        {
            if (_visuals != null) return _visuals;
            _visuals = _poolPrewarmer.CreateVisuals(_profile, transform, _impactPresenter,
                ExpectedShotsInFlight(_profile, _reloadMultiplier, _intervalMultiplier), _strikecraftOwner);
            return _visuals;
        }

        /// <summary>One salvo plus every further salvo fired while the slowest shot is still flying; sizes visual stock.</summary>
        internal static int ExpectedShotsInFlight(WeaponProfile profile) => ExpectedShotsInFlight(profile, 1f, 1f);

        internal static int ExpectedShotsInFlight(WeaponProfile profile, float reloadMultiplier, float intervalMultiplier)
        {
            // A lease lasts until the visual ends: flight plus trail fade or beam hold.
            float flightSeconds = (profile.ProjectileSpeed > 0f ? profile.Range / profile.ProjectileSpeed : 0f) +
                                  profile.ShotPrefab.PostImpactSeconds;
            float cycle = Mathf.Max(profile.Reload * reloadMultiplier,
                (profile.ShotsPerSalvo - 1) * profile.ShotInterval * intervalMultiplier);
            int salvos = 1 + Mathf.CeilToInt(flightSeconds / Mathf.Max(cycle, 0.01f));
            return profile.ShotsPerSalvo * salvos;
        }

        protected override void OnStateUpdated(float healthPercentage)
        {
            base.OnStateUpdated(healthPercentage);
            bool wasDestroyed = Destroyed;
            Destroyed = healthPercentage <= 0f;
            if (Destroyed)
            {
                if (_attackCoordinator != null)
                    _attackCoordinator.CancelSequence(this, _sequence.Generation);
                _sequence.StopEmitting(_sequence.Generation);
            }

            if (Destroyed != wasDestroyed && DestructionChanged != null)
                DestructionChanged.Invoke();
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
