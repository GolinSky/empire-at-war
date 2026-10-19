using System;
using UnityEngine;

namespace EmpireAtWar.Entities.Game
{
    /// <summary>
    /// What a save needs to build the same <see cref="BattleDefinition"/> again: the preset inputs and map seed.
    /// Battle Simulation is never saved.
    /// </summary>
    [Serializable]
    public sealed class BattleSetupSave
    {
        [SerializeField] private GameMode mode;
        [SerializeField] private SkirmishSetupSave skirmish;
        [SerializeField] private SurvivalSetupSave survival;

        public GameMode Mode => mode;

        public static BattleSetupSave From(BattleDefinition battle) => battle.Mode switch
        {
            GameMode.Skirmish => new BattleSetupSave { mode = battle.Mode, skirmish = SkirmishSetupSave.From(battle) },
            GameMode.Survival => new BattleSetupSave { mode = battle.Mode, survival = SurvivalSetupSave.From(battle.Survival) },
            _ => throw new InvalidOperationException($"{battle.Mode} battles are not saved.")
        };

        public BattleDefinition Rebuild() => mode switch
        {
            GameMode.Skirmish => skirmish.Rebuild(),
            GameMode.Survival => survival.Rebuild(),
            _ => throw new InvalidOperationException($"{mode} battles are not saved.")
        };
    }
}
