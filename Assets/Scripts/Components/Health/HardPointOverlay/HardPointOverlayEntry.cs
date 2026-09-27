using System;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Health.HardPointOverlay
{
    [Serializable]
    public struct HardPointOverlayEntry
    {
        [SerializeField] private HardPointType hardPointType;
        [SerializeField] private string displayName;
        [Tooltip("What stops working when this hardpoint is destroyed.")]
        [SerializeField] private string description;
        [SerializeField] private Sprite icon;

        public HardPointType HardPointType => hardPointType;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
    }
}
