using System.Collections.Generic;
using EmpireAtWar.Entities.Game;
using global::VContainer;

namespace EmpireAtWar.Services.VContainer
{
    public sealed class BattleModules
    {
        private readonly BattleDefinition _definition;
        public IReadOnlyList<IBattleModule> Enabled { get; }

        public BattleModules(BattleDefinition definition, params IBattleModule[] available)
        {
            _definition = definition;
            List<IBattleModule> enabled = new List<IBattleModule>();
            foreach (IBattleModule module in available)
                if (definition.HasFeature(module.Feature)) enabled.Add(module);
            Enabled = enabled.AsReadOnly();
        }

        public void ConfigureBattle(IContainerBuilder builder)
        {
            foreach (IBattleModule module in Enabled) module.ConfigureBattle(builder);
        }

        public void ConfigurePlayer(IContainerBuilder builder, BattlePlayer player)
        {
            BattlePlayer effectivePlayer = new BattlePlayer(player.Slot, player.Role, player.Features & _definition.Features);
            foreach (IBattleModule module in Enabled)
                if ((player.Features & module.Feature) == module.Feature)
                    module.ConfigurePlayer(builder, effectivePlayer);
        }
    }
}
