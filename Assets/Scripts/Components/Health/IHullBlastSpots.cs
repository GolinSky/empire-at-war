using System;
using EmpireAtWar.Components.AttackComponent;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Health
{
    /// <summary>Hull-surface points where short blasts can play, read from the unit's hardpoint VFX anchors.</summary>
    public interface IHullBlastSpots
    {
        /// <summary>A hit took hull and the unit survived: hit spot index and damage type.</summary>
        event Action<int, DamageType> HullDamaged;

        int SpotCount { get; }

        /// <summary>False when the spot shows no VFX (disabled or not yet installed hardpoint).</summary>
        bool TryGetSpot(int index, out Transform anchor, out float scale);
    }
}
