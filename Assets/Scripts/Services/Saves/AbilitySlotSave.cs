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

        public ShipAbilityId Id => id;
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
