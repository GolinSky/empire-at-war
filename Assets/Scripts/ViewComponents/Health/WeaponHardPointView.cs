using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Models.Weapon;
using EmpireAtWar.ViewComponents.Weapon;
using UnityEngine;
using Utilities.ScriptUtils.Math;

namespace EmpireAtWar.ViewComponents.Health
{
    public class WeaponHardPointView : HardPointView
    {
        private const string TURRET_PATH = "Projectile";
        private const string DOUBLE_TURRET_PATH = "DualProjectile";
        private const string LASER_TURRET_PATH = "LaserProjectile";
        
        private const int MAX_ATTACKING_TURRETS = 1;
        
        [SerializeField] private FloatRange yAxisRange;

        private List<BaseTurretView> turrets = new List<BaseTurretView>();
        private ProjectileData projectileData;

        private float maxAttackDistance;
        public bool Destroyed { get; private set; }
        public bool IsBusy => turrets.Count(x => x.IsBusy) >= MAX_ATTACKING_TURRETS;
        

        

        public void SetData(FloatRange floatRange)
        {
            yAxisRange.SetValue(floatRange);
        }
        
        public void SetData(ProjectileData projectileData, float maxAttackDistance)
        {
            this.maxAttackDistance = maxAttackDistance;
            this.projectileData = projectileData;
        }

        public bool CanAttack(Vector3 targetPosition)
        {
            float distance = Vector3.Distance(targetPosition, transform.position);
            if (distance > maxAttackDistance) return false;
            
            Vector3 direction = targetPosition - transform.position;

            Quaternion lookRotation = Quaternion.LookRotation(direction, Vector3.up);

            transform.rotation = lookRotation;

            return yAxisRange.IsInRange(GetCorrectAngle(transform.localEulerAngles.y));
        }

        public float Attack(IHardPointView hardPointView)
        {
            BaseTurretView turretView = GetTurret();
            turretView.SetParent(transform);
            float distance = Vector3.Distance(hardPointView.Position, transform.position);
            float duration = distance / turretView.Speed;

            turretView.Attack(hardPointView, duration);
            turretView.ResetParent();
            return duration;
        }

        private BaseTurretView GetTurret()
        {
            BaseTurretView turret = null;
            foreach (BaseTurretView turretView in turrets)
            {
                if (!turretView.IsBusy)
                {
                    turret = turretView;
                }
            }

            if (turret == null)
            {
                string turretPath = TURRET_PATH;
                switch (projectileData.TurretType)
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
                        break;
                    case TurretType.Rocket:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                var prefab =  Repository.LoadComponent<BaseTurretView>(turretPath);
                turret = Instantiate(prefab, transform);
                turret.transform.localPosition = Vector3.zero;// move it to set data method
                turret.SetData(projectileData, maxAttackDistance);
                turrets.Add(turret);
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