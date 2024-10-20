using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Models.Weapon;
using EmpireAtWar.ViewComponents.Weapon;
using LightWeightFramework.Components.Repository;
using Unity.VisualScripting;
using UnityEngine;
using Utilities.ScriptUtils.Math;
using Zenject;

namespace EmpireAtWar.ViewComponents.Health
{
    public class WeaponHardPointView : HardPointView
    {
        private const string TURRET_PATH = "Projectile";
        private const string DOUBLE_TURRET_PATH = "DualProjectile";
        
        private const int MAX_ATTACKING_TURRETS = 3;
        
        [SerializeField] private FloatRange yAxisRange;

        private List<TurretView> turrets = new List<TurretView>();
        private ProjectileData projectileData;
        private float projectileDuration;
        private float maxAttackDistance;
        public bool Destroyed { get; private set; }
        public bool IsBusy => turrets.Count(x => x.IsBusy) > MAX_ATTACKING_TURRETS;
        
        [Inject]
        private IRepository Repository { get; }


        public void SetData(FloatRange floatRange)
        {
            yAxisRange.SetValue(floatRange);
        }
        
        public void SetData(ProjectileData projectileData, float projectileDuration, float maxAttackDistance)
        {
            this.maxAttackDistance = maxAttackDistance;
            this.projectileDuration = projectileDuration;
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

        public void Attack(Vector3 targetPosition)
        {
            TurretView turretView = GetTurret();
            
            float distance = Vector3.Distance(targetPosition, transform.position);
            float duration = distance * turretView.Speed;

            turretView.Attack(targetPosition, duration);
        }

        private TurretView GetTurret()
        {
            TurretView turret = null;
            foreach (TurretView turretView in turrets)
            {
                if (!turretView.IsBusy)
                {
                    turret = turretView;
                }
            }

            if (turret == null)
            {
                var prefab =  Repository.LoadComponent<TurretView>(projectileData.TurretType == TurretType.Dual
                    ? DOUBLE_TURRET_PATH
                    : TURRET_PATH);
                turret = Instantiate(prefab, transform);
                turret.transform.localPosition = Vector3.zero;
                turret.SetData(projectileData, projectileDuration, maxAttackDistance);
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