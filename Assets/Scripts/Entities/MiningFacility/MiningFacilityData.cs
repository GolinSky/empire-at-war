using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.UnitWreck;
using UnityEngine;

namespace EmpireAtWar.Entities.MiningFacility
{
    [CreateAssetMenu(fileName = nameof(MiningFacilityData), menuName = "Data/MiningFacilityData")]
    public class MiningFacilityData : Data
    {
        [field: SerializeField] public EntityComponentData ComponentData { get; private set; }
        [Tooltip("Optional. Empty = the facility is only removed; set = a breaking wreck replaces it.")]
        [field: SerializeField] public UnitWreckData Wreck { get; private set; }

        [field:SerializeField] public float Income { get; private set; }
    }
}
