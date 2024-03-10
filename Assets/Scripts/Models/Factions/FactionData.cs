using System;
using UnityEngine;

namespace EmpireAtWar.Models.Factions
{
    [Serializable]
    public class FactionData
    {
        [field:SerializeField] public string Name { get; private set; }
        [field:SerializeField] public int MaxCount { get; private set; }
        [field:SerializeField] public int AvailableLevel { get; private set; }
        [field:SerializeField] public int Price { get; private set; }
        [field:SerializeField] public int BuildTime { get; private set; }
        [field:SerializeField] public int UnitCapacity { get; private set; }
        [field:SerializeField] public Sprite Icon { get; private set; }
        
    }
}