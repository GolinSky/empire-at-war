using System;
using UnityEngine;

namespace EmpireAtWar.Entities.Ship.Data
{
    [CreateAssetMenu(fileName = "ShipHeightTiersData", menuName = "Data/ShipHeightTiersData")]
    public class ShipHeightTiersData : Mvc.Data
    {
        [SerializeField] private float lowest = -258f;
        [SerializeField] private float low = -171f;
        [SerializeField] private float lowMid = -118f;
        [SerializeField] private float mid = -72f;
        [SerializeField] private float highMid = -12f;
        [SerializeField] private float high = 15f;
        [SerializeField] private float highest = 30f;
        [SerializeField] private float deep = -370f;

        public float GetHeight(ShipHeightTier tier)
        {
            switch (tier)
            {
                case ShipHeightTier.Lowest: return lowest;
                case ShipHeightTier.Low: return low;
                case ShipHeightTier.LowMid: return lowMid;
                case ShipHeightTier.Mid: return mid;
                case ShipHeightTier.HighMid: return highMid;
                case ShipHeightTier.High: return high;
                case ShipHeightTier.Highest: return highest;
                case ShipHeightTier.Deep: return deep;
                default: throw new ArgumentOutOfRangeException(nameof(tier), tier, null);
            }
        }
    }
}
