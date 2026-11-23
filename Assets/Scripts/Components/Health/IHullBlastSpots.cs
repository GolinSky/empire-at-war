using System;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Health
{
    /// <summary>Hull-surface points where short blasts can play, read from the unit's hardpoint VFX anchors.</summary>
    public interface IHullBlastSpots
    {
        /// <summary>A loaded save set the hull; raised before the restored hull is reported as a value change.</summary>
        event Action Restored;

        int SpotCount { get; }

        /// <summary>False when the spot shows no VFX (disabled or not yet installed hardpoint).</summary>
        bool TryGetSpot(int index, out Transform anchor, out float scale);
    }
}
