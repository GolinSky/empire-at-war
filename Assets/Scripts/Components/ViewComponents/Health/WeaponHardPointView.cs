using System;
using EmpireAtWar.Components.AttackComponent;
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
    public class WeaponHardPointView : HardPointView
    {
        // move to factory
        private const string TURRET_PATH = "Projectile";
        private const string DOUBLE_TURRET_PATH = "DualProjectile";
        private const string LASER_TURRET_PATH = "LaserProjectile";
        private const string TORPEDO_TURRET_PATH = "TorpedoProjectile";
        
        [SerializeField] private FloatRange yAxisRange;
        [SerializeField] private int prewarmEffects;
        [field:SerializeField] public WeaponType WeaponType { get; private set; }
        
        private readonly AttackSequenceState _sequence = new AttackSequenceState();
        private ProjectileEffectPool _effectPool;
        private ProjectileData _projectileData;
        private CombatAttackCoordinator _attackCoordinator;

        private float _maxAttackDistance;
        protected IWeaponPresenter WeaponPresenter { get; private set; }
        public bool Destroyed { get; private set; }
        public bool IsBusy => _sequence.IsBusy;
        public float MaxAttackDistance => _maxAttackDistance;
        public float MinYaw => yAxisRange.Min;
        public float MaxYaw => yAxisRange.Max;



        public void SetData(FloatRange floatRange)
        {
            yAxisRange.SetValue(floatRange);
        }
        
        public void SetData(ProjectileData projectileData, float maxAttackDistance, IWeaponPresenter weaponPresenter,
            CombatAttackCoordinator attackCoordinator)
        {
            WeaponPresenter = weaponPresenter;
            _attackCoordinator = attackCoordinator;
            _maxAttackDistance = maxAttackDistance;
            _projectileData = projectileData;
           
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

        internal bool TryStartScheduledSequence(out int generation) => _sequence.TryStart(out generation);

        internal int ShotsPerSalvo => Mathf.CeilToInt(_projectileData.ShotsPerSalvo);
        internal float DelayBetweenShots => _projectileData.DelayBetweenShots;
        internal bool IsEmitting(int generation) => !IsDestroyed && _sequence.IsEmitting(generation);
        internal void StopEmitting(int generation) => _sequence.StopEmitting(generation);

        internal void EmitScheduledShot(AttackData attackData, IHardPointModel hardPointModel, int sequenceGeneration)
        {
            EmitShot(attackData, hardPointModel, sequenceGeneration);
        }

        private void EmitShot(AttackData attackData, IHardPointModel hardPointModel, int sequenceGeneration)
        {
            ProjectileEffectPool pool = GetPool();
            float duration = pool.Play(hardPointModel, sequenceGeneration);

            if (!_sequence.RegisterEffect(sequenceGeneration))
            {
                return;
            }

            _sequence.RecordShotEmission(sequenceGeneration);
            WeaponPresenter.ApplyDamage(attackData, hardPointModel, WeaponType, duration);
        }

        private void OnTurretEffectCompleted(int sequenceGeneration)
        {
            _sequence.TryCompleteEffect(sequenceGeneration);
        }

        private ProjectileEffectPool GetPool()
        {
            if (_effectPool != null)
            {
                return _effectPool;
            }

            string turretPath = TURRET_PATH;
            switch (_projectileData.TurretType)
            {
                case TurretType.Single:
                    break;
                case TurretType.Dual:
                    turretPath = DOUBLE_TURRET_PATH;
                    break;
                case TurretType.Laser:
                    turretPath = LASER_TURRET_PATH;
                    break;
                case TurretType.Torpedo:
                    turretPath = TORPEDO_TURRET_PATH;
                    break;
                case TurretType.Rocket:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            BaseTurretView prefab = Repository.LoadComponent<BaseTurretView>(turretPath);
            int maxIdle = Mathf.CeilToInt(_projectileData.ShotsPerSalvo);
            _effectPool = new ProjectileEffectPool(prefab, transform, _projectileData, _maxAttackDistance,
                maxIdle, OnTurretEffectCompleted);
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
