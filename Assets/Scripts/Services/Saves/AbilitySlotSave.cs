using System;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Services.ShipAbilities;
using UnityEngine;

namespace EmpireAtWar.Services.Saves
{
    [Serializable]
    public sealed class AbilitySlotSave
    {
        [SerializeField] private ShipAbilityId id;
        [SerializeField] private ShipAbilityState state;
        [SerializeField] private float timeLeft;

        // Normalize retired ability variants before validation and restoration read the slot.
        public ShipAbilityId Id => (int)id switch
        {
            13 or 15 or 17 or 26 or 34 or 45 or 53 => ShipAbilityId.BoostShieldPower,
            14 or 21 or 37 => ShipAbilityId.BoostEnginePower,
            12 or 20 or 23 or 31 or 32 or 38 => ShipAbilityId.BoostWeaponPower,
            _ => id
        };
        public ShipAbilityState State => state;
        public float TimeLeft => timeLeft;

        /// <summary>
        /// An active ability is saved as recovering for its remaining time plus the full recovery delay;
        /// its running effect is not saved.
        /// </summary>
        public static AbilitySlotSave From(ShipAbilitySlot slot) => slot.State == ShipAbilityState.Active
            ? new AbilitySlotSave
            {
                id = slot.Id, state = ShipAbilityState.Recovering,
                timeLeft = slot.TimeLeft + slot.Definition.RecoveryDelay
            }
            : new AbilitySlotSave { id = slot.Id, state = slot.State, timeLeft = slot.TimeLeft };
    }
}
