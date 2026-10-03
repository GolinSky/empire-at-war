using EmpireAtWar.Entities.Tooltip;
using System;
using UnityEngine;

namespace EmpireAtWar.Models.Factions
{
    [Serializable]
    public class FactionData
    {
        [SerializeField] private UnitMatchupData matchups;

        [SerializeField, TextArea] private string description;
        [SerializeField] private string role;
        [SerializeField] private string iconKey;

        public string Description => description;
        public string Role => role;
        public string IconKey => iconKey;
        public UnitMatchupData Matchups => matchups;
        [field:SerializeField] public string Name { get; private set; }
        [field:SerializeField] public int MaxCount { get; private set; }
        [field:SerializeField] public int AvailableLevel { get; private set; }
        [field:SerializeField] public int Price { get; private set; }
        [field:SerializeField] public int BuildTime { get; private set; }
        [field:SerializeField] public int UnitCapacity { get; private set; }
        [field:SerializeField] public Sprite Icon { get; private set; }
        
    }
}
