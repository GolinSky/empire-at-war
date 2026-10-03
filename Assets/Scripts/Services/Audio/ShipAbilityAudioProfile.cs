using System;
using EmpireAtWar.Services.ShipAbilities;
using UnityEngine;

namespace EmpireAtWar.Services.Audio
{
    [Serializable]
    public sealed class ShipAbilityAudioProfile
    {
        [SerializeField] private SfxProfile start;
        [SerializeField] private SfxProfile execution;
        [SerializeField] private SfxProfile end;
        [SerializeField] private SfxProfile restore;

        [SerializeField] private ShipAbilityId abilityId;

        public ShipAbilityId AbilityId => abilityId;
        public SfxProfile Start => start;
        public SfxProfile Execution => execution;
        public SfxProfile End => end;
        public SfxProfile Restore => restore;
    }
}
