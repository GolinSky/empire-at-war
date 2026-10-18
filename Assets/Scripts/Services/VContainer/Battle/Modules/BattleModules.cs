using System.Collections.Generic;
using EmpireAtWar.Entities.Game;
using global::VContainer;
using UnityEngine;

namespace EmpireAtWar.Services.VContainer
{
    public sealed class BattleModules
    {
        private readonly IReadOnlyList<IBattleModule> _disabled;
        public IReadOnlyList<IBattleModule> Enabled { get; }

        public BattleModules(BattleDefinition definition, params IBattleModule[] available)
        {
            List<IBattleModule> enabled = new List<IBattleModule>();
            List<IBattleModule> disabled = new List<IBattleModule>();
            foreach (IBattleModule module in available)
                (definition.HasFeature(module.Feature) ? enabled : disabled).Add(module);
            Enabled = enabled.AsReadOnly();
            _disabled = disabled.AsReadOnly();
        }

        public void ConfigureBattle(IContainerBuilder builder)
        {
            foreach (IBattleModule module in Enabled) module.ConfigureBattle(builder);
            foreach (IBattleModule module in _disabled) module.ConfigureDisabled(builder);
        }

        public void ConfigurePlayer(IContainerBuilder builder, BattlePlayer player, Transform playerRoot)
        {
            foreach (IBattleModule module in Enabled)
                if (player.Has(module.Feature))
                    module.ConfigurePlayer(builder, player, playerRoot);
        }
    }
}
