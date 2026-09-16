using System;
using System.Collections;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.Timing;
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
        [field:SerializeField] public WeaponType WeaponType { get; private set; }
        
        private readonly List<BaseTurretView> _turrets = new List<BaseTurretView>();
        private readonly Dictionary<BaseTurretView, int> _activeEffectSequences = new Dictionary<BaseTurretView, int>();
        private readonly AttackSequenceState _sequence = new AttackSequenceState();
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

            foreach (BaseTurretView turret in _turrets)
            {
                turret.EffectCompleted -= OnTurretEffectCompleted;
                turret.RetireAfterCompletion();
            }

            _activeEffectSequences.Clear();
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
            BaseTurretView turretView = GetTurret();
            turretView.SetParent(transform);
            turretView.Attack(hardPointModel, out float duration);
            turretView.ResetParent();

            if (!_sequence.RegisterEffect(sequenceGeneration))
            {
                return;
            }

            if (_activeEffectSequences.ContainsKey(turretView))
            {
                throw new InvalidOperationException("An active projectile effect cannot be leased twice.");
            }

            _activeEffectSequences.Add(turretView, sequenceGeneration);
            _sequence.RecordShotEmission(sequenceGeneration);
            WeaponPresenter.ApplyDamage(attackData, hardPointModel, WeaponType, duration);
        }

        private void OnTurretEffectCompleted(BaseTurretView turretView, int leaseId)
        {
            if (turretView.LeaseId != leaseId || !_activeEffectSequences.TryGetValue(turretView, out int sequenceGeneration))
            {
                AttackSequenceDiagnostics.RecordUnmatchedCompletion();
                return;
            }

            _activeEffectSequences.Remove(turretView);
            _sequence.TryCompleteEffect(sequenceGeneration);
        }

        protected BaseTurretView GetTurret()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.ProjectileGetOrCreate.Auto())
            {
#endif
            BaseTurretView turret = null;
            foreach (BaseTurretView turretView in _turrets)
            {
                if (!turretView.IsBusy)
                {
                    turret = turretView;
                }
            }

            if (!turret)
            {
                string turretPath = TURRET_PATH;
                switch (_projectileData.TurretType)
                {
                    case TurretType.Single:
                        turretPath = TURRET_PATH;
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
                var prefab = Repository.LoadComponent<BaseTurretView>(turretPath);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using (BattleProfilerMarkers.ProjectileInstantiate.Auto())
                {
#endif
                turret = Instantiate(prefab, transform);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                }
#endif
                turret.transform.localPosition = Vector3.zero;// move it to set data method
                turret.SetData(_projectileData, _maxAttackDistance);
                turret.EffectCompleted += OnTurretEffectCompleted;
                _turrets.Add(turret);
            }

            return turret;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            }
#endif
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
