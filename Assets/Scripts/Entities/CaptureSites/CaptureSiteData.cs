using EmpireAtWar.Mvc;
using UnityEngine;
using Utilities.ScriptUtils.EditorSerialization;

namespace EmpireAtWar.Entities.CaptureSites
{
    [CreateAssetMenu(fileName = nameof(CaptureSiteData), menuName = "Data/Capture Site Data")]
    public sealed class CaptureSiteData : Data
    {
        [SerializeField] private DictionaryWrapper<SiteFacilityType, SiteFacilityCost> facilityCosts;

        [field: SerializeField, Min(0.01f)]
        public float CaptureSpeedPerNetShip { get; private set; } = 1f;

        /// <summary>Capture strength of one fighter squadron relative to one ship.</summary>
        [field: SerializeField, Range(0f, 1f)]
        public float SquadronCaptureWeight { get; private set; } = 0.5f;

        public SiteFacilityCost GetCost(SiteFacilityType facilityType)
        {
            return facilityCosts.Dictionary[facilityType];
        }
    }
}
