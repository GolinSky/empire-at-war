using System;
using EmpireAtWar.Services.ShipAbilities.Abilities;
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
        [SerializeField, TextArea] private string description;
        [Header("Timing")]
        [SerializeField] private float duration;
        [SerializeField] private float recoveryDelay;
        [SerializeField] private bool canCancel;
        [SerializeField] private bool isToggle;
        [Header("Targeting")]
        [SerializeField] private bool requiresEnemyTarget;
        [SerializeField] private float range;
        [Header("Auto Power")]
        [SerializeField] private AutoPowerRole autoPower;
        [Header("Ability")]
        [SerializeReference, SubclassSelector] private ShipAbilitySettings settings;

        public Sprite Icon => icon;
        public string DisplayName => displayName;
        public string Description => description;
        public float Duration => duration;
        public float RecoveryDelay => recoveryDelay;
        /// <summary>Game rules may end it early (Cloak on attack, Interdict on Retreat).</summary>
        public bool CanCancel => canCancel;
        /// <summary>For now players (click again, auto power) may end only toggles; other abilities run their full time.</summary>
        public bool CanPlayerCancel => canCancel && isToggle;
        public bool IsToggle => isToggle;
        public bool RequiresEnemyTarget => requiresEnemyTarget;
        public float Range => range;
        public AutoPowerRole AutoPower => autoPower;
        public ShipAbilitySettings Settings => settings;
        internal bool IssuesAttackOrder => settings is AssaultSettings or ConcentrateFireSettings or LuckyShotSettings;
    }
}
