using EmpireAtWar.Mvc;
using UnityEngine;
using Utilities.ScriptUtils.EditorSerialization;

namespace EmpireAtWar.Entities.SuperWeapons
{
    [CreateAssetMenu(fileName = nameof(SuperWeaponData), menuName = "Data/SuperWeaponData")]
    public class SuperWeaponData : Data
    {
        [SerializeField] private DictionaryWrapper<SuperWeaponType, SuperWeaponProfile> profiles;

        public SuperWeaponProfile GetProfile(SuperWeaponType type) => profiles.Dictionary[type];
    }
}
