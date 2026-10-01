using System;
using EmpireAtWar.Entities.Game;

namespace EmpireAtWar.Views.Game
{
    public interface IEndGameView
    {
        event Action ReturnToMenuRequested;
        void ShowResult(BattleResult result);
        void Hide();
    }
}
