using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        [field:SerializeField] public WeaponType WeaponType { get; private set; }
        
        private List<BaseTurretView> _turrets = new List<BaseTurretView>();
        private ProjectileData _projectileData;
        private Coroutine _attackCoroutine;

        private float _maxAttackDistance;
        protected IWeaponPresenter WeaponPresenter { get; private set; }
        public bool Destroyed { get; private set; }
        public bool IsBusy => _turrets.Any(x => x.IsBusy) || _attackCoroutine != null;



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
            if (_attackCoroutine != null)
            {
                Debug.LogError($"_attackCoroutine is not null on {gameObject.name}");
            }
            _attackCoroutine = StartCoroutine(AttackCoroutine(attackData, hardPointModel));

        }

        private IEnumerator AttackCoroutine(AttackData attackData, IHardPointModel hardPointModel)
        {
            for (int i = 0; i < _projectileData.ShotsPerSalvo; i++)
            {
                BaseTurretView turretView = GetTurret();
                turretView.SetParent(transform);
                float distance = Vector3.Distance(hardPointModel.Position, transform.position);
                float duration = distance / turretView.Speed;

                turretView.Attack(hardPointModel, duration);
                turretView.ResetParent();
                WeaponPresenter.ApplyDamage(attackData, hardPointModel, WeaponType, duration);
                yield return new WaitForSeconds(_projectileData.DelayBetweenShots);
            }

            _attackCoroutine = null;

        }

        protected BaseTurretView GetTurret()
        {
            BaseTurretView turret = null;
            foreach (BaseTurretView turretView in _turrets)
            {
                if (!turretView.IsBusy)
                {
                    turret = turretView;
                }
            }

            if (turret == null)
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
                var prefab =  Repository.LoadComponent<BaseTurretView>(turretPath);
                turret = Instantiate(prefab, transform);
                turret.transform.localPosition = Vector3.zero;// move it to set data method
                turret.SetData(_projectileData, _maxAttackDistance);
                _turrets.Add(turret);
            }

            return turret;
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
            }
        }
    }
}