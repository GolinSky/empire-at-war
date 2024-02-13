using EmpireAtWar.Models.Factions;
using UnityEngine;

namespace EmpireAtWar.Models.Skirmish
{
    //test 
    public class SkirmishGameData
    {
        public FactionType PlayerFactionType { get; set; }
        public FactionType EnemyFactionType { get; set; }
        
        
        //test refactor

       

        public SkirmishGameData()
        {
            EnemyFactionType = FactionType.Separatist;
        }
        
        public Vector3 GetStationPosition(PlayerType playerType)
        {
            switch (playerType)
            {
                case PlayerType.Player:
                    return new Vector3(-30, -15, 20);
                case PlayerType.Opponent:
                    return new Vector3(250, -15, -170);
            }

            return Vector3.zero;
        }
    }
}