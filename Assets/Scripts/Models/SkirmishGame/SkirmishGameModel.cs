using System;
using EmpireAtWar.Commands.Game;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.SkirmishGame
{
    public interface ISkirmishGameModelObserver:IModelObserver
    {
        event Action<float> OnMoneyChanged;
        event Action<GameTimeMode> OnGameTimeModeChange;
        GameTimeMode GameTimeMode { get; }
        float Money { get; }
    }
    
    [CreateAssetMenu(fileName = "SkirmishGameModel", menuName = "Model/SkirmishGameModel")]
    public class SkirmishGameModel: Model, ISkirmishGameModelObserver
    {
        public event Action<float> OnMoneyChanged;
        public event Action<GameTimeMode> OnGameTimeModeChange;

        private GameTimeMode gameTimeMode;
        
        [field: SerializeField] public float IncomeDelay { get; private set; }
        [field: SerializeField] public float StartMoneyAmount { get; private set; }

        private float money;
        public GameTimeMode GameTimeMode
        {
            get => gameTimeMode;
            set
            {
                gameTimeMode = value;
                OnGameTimeModeChange?.Invoke(gameTimeMode);
            }
        }

        public float Money
        {
            get => money;
            set
            {
                money = value;
                OnMoneyChanged?.Invoke(money);
            }
        }
    }
}