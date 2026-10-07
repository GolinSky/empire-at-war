using System;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Station
{
    /// <summary>Where one gameplay hardpoint sits on a station model, and the source artwork it destroys.</summary>
    [Serializable]
    public struct StationMount
    {
        [field: SerializeField] public int HardPointId { get; private set; }
        [field: SerializeField] public Transform Point { get; private set; }
        [Tooltip("Separate artwork, hidden while the hardpoint is destroyed. Leave empty when artwork is embedded in the hull.")]
        [field: SerializeField] public GameObject Art { get; private set; }
    }
}
