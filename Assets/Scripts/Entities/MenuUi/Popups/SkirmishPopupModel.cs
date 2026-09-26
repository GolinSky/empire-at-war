using System;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.MenuUi.Popups
{
    public class SkirmishPopupModel : PureModel, ISkirmishPopupModelObserver
    {
        private const float MIN_STARTING_MONEY = 500f;
        private const float MAX_STARTING_MONEY = 10000f;
        private const float DEFAULT_STARTING_MONEY = 2000f;
        private const float MONEY_STEP = 500f;

        private static readonly FactionType[] _factions =
            (FactionType[])Enum.GetValues(typeof(FactionType));

        public event Action Changed;

        public FactionType PlayerFaction { get; private set; } = FactionType.Republic;
        public FactionType EnemyFaction { get; private set; } = FactionType.Separatist;
        public PlanetType Planet { get; private set; }
        public BattleVictoryCondition VictoryCondition { get; private set; }
        public EnemyAiDifficulty EnemyDifficulty { get; private set; }
        public float MinStartingMoney => MIN_STARTING_MONEY;
        public float MaxStartingMoney => MAX_STARTING_MONEY;
        public float StartingMoney { get; private set; } = DEFAULT_STARTING_MONEY;

        public void SelectPlayerFaction(FactionType faction)
        {
            PlayerFaction = faction;
            if (EnemyFaction == faction)
            {
                EnemyFaction = GetNextFaction(faction);
            }

            Changed?.Invoke();
        }

        public void SelectEnemyFaction(FactionType faction)
        {
            EnemyFaction = faction;
            if (PlayerFaction == faction)
            {
                PlayerFaction = GetNextFaction(faction);
            }

            Changed?.Invoke();
        }

        public void SelectPlanet(PlanetType planet)
        {
            Planet = planet;
        }

        public void SelectVictoryCondition(BattleVictoryCondition condition)
        {
            VictoryCondition = condition;
        }

        public void SelectEnemyDifficulty(EnemyAiDifficulty difficulty)
        {
            EnemyDifficulty = difficulty;
        }

        public void SelectStartingMoney(float rawValue)
        {
            float snappedValue = (float)Math.Round(rawValue / MONEY_STEP) * MONEY_STEP;
            StartingMoney = Math.Max(MIN_STARTING_MONEY,
                Math.Min(MAX_STARTING_MONEY, snappedValue));
            Changed?.Invoke();
        }

        private static FactionType GetNextFaction(FactionType faction)
        {
            if (_factions.Length < 2)
            {
                throw new InvalidOperationException("Skirmish setup requires at least two factions.");
            }

            int index = Array.IndexOf(_factions, faction);
            return _factions[(index + 1) % _factions.Length];
        }
    }
}
