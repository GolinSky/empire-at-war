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
        [SerializeField] private bool hasChargeTime;
        [SerializeField] private float chargeTimeLeft;

        public SuperWeaponType Type => type;
        public SuperWeaponState State => state;
        public float Cooldown => cooldown;
        public bool HasChargeTime => hasChargeTime;
        public float ChargeTimeLeft => chargeTimeLeft;

        public static SuperWeaponSave Create(SuperWeaponType type, SuperWeaponState state, float cooldown,
            float? chargeTimeLeft = null) => new SuperWeaponSave
            {
                type = type, state = state, cooldown = cooldown,
                hasChargeTime = chargeTimeLeft.HasValue,
                chargeTimeLeft = chargeTimeLeft.GetValueOrDefault()
            };
    }
}
