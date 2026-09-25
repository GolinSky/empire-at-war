using System.Collections.Generic;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Components.AttackComponent
{
    [CreateAssetMenu(fileName = nameof(WeaponsData), menuName = "Data/Weapon/WeaponsData")]
    public class WeaponsData : Data
    {
        [SerializeField] private List<WeaponProfile> weapons = new List<WeaponProfile>();

        private Dictionary<WeaponType, WeaponProfile> _profiles;

        public WeaponProfile GetProfile(WeaponType weaponType)
        {
            if (_profiles == null)
            {
                _profiles = new Dictionary<WeaponType, WeaponProfile>();
                foreach (WeaponProfile profile in weapons)
                    _profiles.Add(profile.WeaponType, profile);
            }

            return _profiles[weaponType];
        }
    }
}
