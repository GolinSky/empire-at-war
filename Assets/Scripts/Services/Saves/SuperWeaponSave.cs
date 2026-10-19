using System;
using EmpireAtWar.Entities.SuperWeapons;
using UnityEngine;

namespace EmpireAtWar.Services.Saves
{
    [Serializable]
    public sealed class SuperWeaponSave
    {
        [SerializeField] private SuperWeaponType type;
        [SerializeField] private SuperWeaponState state;
        [SerializeField] private float cooldown;

        public SuperWeaponType Type => type;
        public SuperWeaponState State => state;
        public float Cooldown => cooldown;

        public static SuperWeaponSave Create(SuperWeaponType type, SuperWeaponState state, float cooldown) =>
            new SuperWeaponSave { type = type, state = state, cooldown = cooldown };
    }
}
