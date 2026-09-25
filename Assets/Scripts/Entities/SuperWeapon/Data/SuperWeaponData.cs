using EmpireAtWar.Mvc;
using UnityEngine;
using Utilities.ScriptUtils.EditorSerialization;

namespace EmpireAtWar.Entities.SuperWeapons
{
    [CreateAssetMenu(fileName = nameof(SuperWeaponData), menuName = "Data/SuperWeaponData")]
    public class SuperWeaponData : Data
    {
        [SerializeField] private DictionaryWrapper<SuperWeaponType, SuperWeaponProfile> profiles;
        [Tooltip("Off-map firing point relative to the target: below and outside the battlefield, like a planetary gun.")]
        [SerializeField] private Vector3 originOffset = new Vector3(0f, -400f, -600f);

        public Vector3 OriginOffset => originOffset;

        public SuperWeaponProfile GetProfile(SuperWeaponType type) => profiles.Dictionary[type];
    }
}
