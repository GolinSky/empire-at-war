using System;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Services.ShipAbilities.Abilities;
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
        [Header("Stats")]
        [SerializeField] private CombatStatModifier statModifier;
        [Header("Beam")]
        [SerializeField] private float beamDamage;
        [SerializeField] private WeaponType beamWeaponType;
        [SerializeField] private ProtonBeamView beamViewPrefab;
        [Header("Command")]
        [SerializeField] private float commandRadius;

        public Sprite Icon => icon;
        public string DisplayName => displayName;
        public float Duration => duration;
        public float RecoveryDelay => recoveryDelay;
        public bool CanCancel => canCancel;
        public bool RequiresEnemyTarget => requiresEnemyTarget;
        public float Range => range;
        public ShipAbilityAiUse AiUse => aiUse;
        public CombatStatModifier StatModifier => statModifier;
        public float BeamDamage => beamDamage;
        public WeaponType BeamWeaponType => beamWeaponType;
        public ProtonBeamView BeamViewPrefab => beamViewPrefab;
        public float CommandRadius => commandRadius;
    }
}
