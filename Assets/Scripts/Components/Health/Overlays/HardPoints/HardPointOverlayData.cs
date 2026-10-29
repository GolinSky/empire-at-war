using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Health.HardPointOverlay
{
    [CreateAssetMenu(fileName = nameof(HardPointOverlayData), menuName = "Data/Hard Point Overlay Data")]
    public sealed class HardPointOverlayData : ScriptableObject
    {
        [field: SerializeField, Min(0f), Tooltip("Maximum camera-to-unit distance for hardpoint markers, in world units.")]
        public float MaxVisibleDistance { get; private set; } = 1000f;

        [SerializeField] private List<HardPointOverlayEntry> entries = new List<HardPointOverlayEntry>();

        public HardPointOverlayEntry Get(HardPointType hardPointType)
        {
            foreach (HardPointOverlayEntry entry in entries)
            {
                if (entry.HardPointType == hardPointType)
                {
                    return entry;
                }
            }

            throw new InvalidOperationException(
                $"{nameof(HardPointOverlayData)} has no entry for {hardPointType}.");
        }
    }
}
