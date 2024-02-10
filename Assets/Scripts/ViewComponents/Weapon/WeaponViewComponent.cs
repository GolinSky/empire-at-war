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
        
        protected override void OnInit()
        {
            weaponModelObserver = ModelObserver.GetModelObserver<IWeaponModelObserver>();
            projectileModel = weaponModelObserver.ProjectileModel;
            foreach (var keyValuePair in TurretDictionary)
            {
                foreach (TurretView turretView in keyValuePair.Value)
                {
                    turretView.SetData(projectileModel.ProjectileData[keyValuePair.Key], weaponModelObserver.ProjectileSpeed);
                }
            }
            weaponModelObserver.OnAttack += Attack;
        }
        
        protected override void OnRelease()
        {
            weaponModelObserver.OnAttack -= Attack;
        }
        
        private void Attack(Vector3 targetPosition)
        {
            foreach (List<TurretView> turretDictionaryValue in TurretDictionary.Values)
            {
                foreach (var turretView in turretDictionaryValue)
                {
                    turretView.Attack(targetPosition);
                }
            }
        }
    }
}