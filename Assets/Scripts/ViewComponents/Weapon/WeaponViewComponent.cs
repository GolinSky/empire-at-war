using System.Collections;
using System.Collections.Generic;
using EmpireAtWar.Models.Weapon;
using EmpireAtWar.ScriptUtils.EditorSerialization;
using UnityEngine;
using WorkShop.LightWeightFramework.ViewComponents;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public class WeaponViewComponent : ViewComponent
    {
        [SerializeField] private DictionaryWrapper<WeaponType, List<TurretView>> turretDictionary;

        private Dictionary<WeaponType, List<TurretView>> TurretDictionary => turretDictionary.Dictionary;

        private IWeaponModelObserver weaponModelObserver;
        private IProjectileModel projectileModel;
        private Coroutine attackCoroutine;
        
        protected override void OnInit()
        {
            weaponModelObserver = ModelObserver.GetModelObserver<IWeaponModelObserver>();
            projectileModel = weaponModelObserver.ProjectileModel;
            foreach (var keyValuePair in TurretDictionary)
            {
                foreach (TurretView turretView in keyValuePair.Value)
                {
                    turretView.SetData(projectileModel.ProjectileData[keyValuePair.Key], weaponModelObserver.ProjectileDuration);
                }
            }
            weaponModelObserver.OnAttack += Attack;
        }
        
        protected override void OnRelease()
        {
            weaponModelObserver.OnAttack -= Attack;
        }
        
        private void Attack(Vector3 targetPosition, List<WeaponType> filter)
        {
            if (attackCoroutine != null)
            {
                return;
            }
            attackCoroutine = StartCoroutine(AttackSequence(targetPosition, filter));
        }

        private IEnumerator AttackSequence(Vector3 targetPosition, List<WeaponType> filter)
        {
            foreach (var turretDictionaryValue in TurretDictionary)
            {
                if(!filter.Contains(turretDictionaryValue.Key)) continue;
                
                foreach (TurretView turretView in turretDictionaryValue.Value)
                {
                    if(turretView.IsBusy || !turretView.CanAttack(targetPosition)) continue;
                    
                    turretView.Attack(targetPosition);
                    yield return new WaitForSeconds(1f);
                }
            }

            attackCoroutine = null;
        }
    }
}