using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Health.HardPointOverlay
{
    [CreateAssetMenu(fileName = nameof(HardPointOverlayData), menuName = "Data/Hard Point Overlay Data")]
    public sealed class HardPointOverlayData : ScriptableObject
    {
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
