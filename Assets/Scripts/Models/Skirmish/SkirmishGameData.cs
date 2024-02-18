using EmpireAtWar.Models.Factions;
using UnityEngine;

namespace EmpireAtWar.Models.Skirmish
{
    //test 
    public class SkirmishGameData
    {
        public FactionType PlayerFactionType { get; set; }
        public FactionType EnemyFactionType { get; set; }
        
    
        public SkirmishGameData()
        {
            EnemyFactionType = FactionType.Separatist;
        }
    }
}