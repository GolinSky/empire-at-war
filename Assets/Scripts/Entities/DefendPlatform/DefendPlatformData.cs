using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.UnitWreck;
using UnityEngine;

namespace EmpireAtWar.Entities.DefendPlatform
{
    [CreateAssetMenu(fileName = nameof(DefendPlatformData), menuName = "Data/DefendPlatformData")]
    public class DefendPlatformData : Data
    {
        [field: SerializeField] public EntityComponentData ComponentData { get; private set; }
        [Tooltip("Optional. Empty = the platform is only removed; set = a breaking wreck replaces it.")]
        [field: SerializeField] public UnitWreckData Wreck { get; private set; }
    }
}
