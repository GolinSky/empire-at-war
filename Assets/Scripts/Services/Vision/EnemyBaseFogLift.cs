using System.Collections.Generic;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.Vision
{
    /// <summary>Lifts the fog for every player of a team once all enemy stations have fallen.</summary>
    public sealed class EnemyBaseFogLift : ITickable
    {
        private readonly BattleVictoryModel _victory;
        private readonly IPlayerRoster _roster;
        private readonly VisionService _vision;

        public EnemyBaseFogLift(BattleVictoryModel victory, IPlayerRoster roster, VisionService vision)
        {
            _victory = victory;
            _roster = roster;
            _vision = vision;
        }

        public void Tick()
        {
            IReadOnlyList<PlayerSlot> players = _roster.Players;
            for (int i = 0; i < players.Count; i++)
                _vision.SetFogLifted(players[i].Id, _victory.AreEnemyBasesDown(players[i].Team));
        }
    }
}
