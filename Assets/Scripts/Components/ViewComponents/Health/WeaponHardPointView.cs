using System;
using System.Collections;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Models.Health;
using EmpireAtWar.ViewComponents.Weapon;
using UnityEngine;
using Utilities.ScriptUtils.Math;

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
        private Coroutine _attackCoroutine;

        private float _maxAttackDistance;
        protected IWeaponPresenter WeaponPresenter { get; private set; }
        public bool Destroyed { get; private set; }
        public bool IsBusy => _sequence.IsBusy;



        public void SetData(FloatRange floatRange)
        {
            yAxisRange.SetValue(floatRange);
        }
        
        public void SetData(ProjectileData projectileData, float maxAttackDistance, IWeaponPresenter weaponPresenter)
        {
            WeaponPresenter = weaponPresenter;
            _maxAttackDistance = maxAttackDistance;
            _projectileData = projectileData;
           
        }

        public bool CanAttack(Vector3 targetPosition)
        {
            float distance = Vector3.Distance(targetPosition, transform.position);
            if (distance > _maxAttackDistance) return false;
            

            Vector3 direction = targetPosition - transform.position;

            Quaternion lookRotation = Quaternion.LookRotation(direction, Vector3.up);

            transform.rotation = lookRotation;

            return yAxisRange.IsInRange(GetCorrectAngle(transform.localEulerAngles.y));
        }

        public virtual void Attack(AttackData attackData, IHardPointModel hardPointModel)
        {
            if (!_sequence.TryStart(out int sequenceGeneration))
            {
                return;
            }

            _attackCoroutine = StartCoroutine(AttackCoroutine(attackData, hardPointModel, sequenceGeneration));
        }

        public void ReleaseAttackSequence()
        {
            if (_attackCoroutine != null)
            {
                StopCoroutine(_attackCoroutine);
                _attackCoroutine = null;
            }

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

        private IEnumerator AttackCoroutine(AttackData attackData, IHardPointModel hardPointModel, int sequenceGeneration)
        {
            for (int i = 0; i < _projectileData.ShotsPerSalvo; i++)
            {
                if (!_sequence.IsEmitting(sequenceGeneration) || IsDestroyed)
                {
                    break;
                }

                EmitShot(attackData, hardPointModel, sequenceGeneration);
                yield return new WaitForSeconds(_projectileData.DelayBetweenShots);
            }

            _sequence.StopEmitting(sequenceGeneration);
            yield return new WaitWhile(() => _sequence.Generation == sequenceGeneration && _sequence.IsBusy);

            if (_sequence.Generation == sequenceGeneration)
            {
                _attackCoroutine = null;
            }
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


        private float GetCorrectAngle(float y)
        {
            if(y > 180)
            {
                return y - 360;
            }

            return y;
        }
        
        protected override void OnStateUpdated(float healthPercentage)
        {
            base.OnStateUpdated(healthPercentage);
            if (healthPercentage <= 0f)
            {
                Destroyed = true;
                _sequence.StopEmitting(_sequence.Generation);
            }
        }

#if UNITY_EDITOR
        public void SetWeaponType(WeaponType weaponType)
        {
            WeaponType = weaponType;
        }    
#endif
  
    }
}
