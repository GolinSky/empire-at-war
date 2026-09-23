using System;
using EmpireAtWar.Commands.Game;

namespace EmpireAtWar.Models.SkirmishGame
{
    public interface ISkirmishSessionModelObserver
    {
        event Action<GameTimeMode> OnGameTimeModeChanged;
        GameTimeMode GameTimeMode { get; }
        bool IsBattleEnded { get; }
    }
}
