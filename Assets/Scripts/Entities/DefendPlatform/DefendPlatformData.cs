using EmpireAtWar.Entities.BaseEntity;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.UnitWreck;
using UnityEngine;

namespace EmpireAtWar.Entities.DefendPlatform
{
    [CreateAssetMenu(fileName = nameof(DefendPlatformData), menuName = "Data/DefendPlatformData")]
    public class DefendPlatformData : Data
    {
        [field: SerializeField] public EntityComponentData ComponentData { get; private set; }
        [field: SerializeField] public float Income { get; private set; }
        [SerializeField] private List<WeaponLoadoutEntry> weaponLoadout = new List<WeaponLoadoutEntry>();
        public IReadOnlyList<WeaponLoadoutEntry> WeaponLoadout => weaponLoadout;
        [Tooltip("Optional. Empty = the platform is only removed; set = a breaking wreck replaces it.")]
        [field: SerializeField] public UnitWreckData Wreck { get; private set; }
    }
}
