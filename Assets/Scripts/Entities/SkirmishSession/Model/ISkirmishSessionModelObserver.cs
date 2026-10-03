using System;
using EmpireAtWar.Commands.Game;

namespace EmpireAtWar.Models.SkirmishGame
{
    public interface ISkirmishSessionModelObserver
    {
        event Action<GameTimeMode> OnGameTimeModeChanged;

        GameTimeMode EffectiveTimeMode { get; }
        bool IsBattleEnded { get; }
    }
}
