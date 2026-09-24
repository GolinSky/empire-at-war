using System;
using EmpireAtWar.Utils;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities
{
    [Serializable]
    public sealed class ShipAbilityDefinition
    {
        [Header("Ui")]
        [SerializeField] private Sprite icon;
        [SerializeField] private string displayName;
        [Header("Timing")]
        [SerializeField] private float duration;
        [SerializeField] private float recoveryDelay;
        [SerializeField] private bool canCancel;
        [Header("Targeting")]
        [SerializeField] private bool requiresEnemyTarget;
        [SerializeField] private float range;
        [Header("Ai")]
        [SerializeField] private ShipAbilityAiUse aiUse;
        [Header("Ability")]
        [SerializeReference, SubclassSelector] private ShipAbilitySettings settings;

        public Sprite Icon => icon;
        public string DisplayName => displayName;
        public float Duration => duration;
        public float RecoveryDelay => recoveryDelay;
        public bool CanCancel => canCancel;
        public bool RequiresEnemyTarget => requiresEnemyTarget;
        public float Range => range;
        public ShipAbilityAiUse AiUse => aiUse;
        public ShipAbilitySettings Settings => settings;
    }
}
