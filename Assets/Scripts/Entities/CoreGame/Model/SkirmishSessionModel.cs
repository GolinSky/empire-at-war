using System;
using EmpireAtWar.Commands.Game;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Models.SkirmishGame
{
    public class SkirmishSessionModel : PureModel, ISkirmishSessionModelObserver
    {
        public event Action<GameTimeMode> OnGameTimeModeChanged;

        public GameTimeMode GameTimeMode { get; private set; } = GameTimeMode.Common;
        public bool IsBattleEnded { get; private set; }

        public void SetGameTimeMode(GameTimeMode mode)
        {
            GameTimeMode = mode;
            OnGameTimeModeChanged?.Invoke(mode);
        }

        public void MarkBattleEnded()
        {
            IsBattleEnded = true;
        }
    }
}
