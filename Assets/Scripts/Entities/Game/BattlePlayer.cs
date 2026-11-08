using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Entities.Game
{
    public sealed class BattlePlayer
    {
        public PlayerSlot Slot { get; }
        public PlayerRole Role { get; }
        public BattleFeatures Features { get; }
        /// <summary>The player whose production bases this player buys from: itself, or the human hosting an AI ally.</summary>
        public PlayerId ProductionHostId { get; }
        public bool OwnsStation => Role == PlayerRole.Human || Role == PlayerRole.AiOpponent;
        public bool CanProduce => Role != PlayerRole.WaveAttacker && Role != PlayerRole.SimulationSide;
        public bool CanResearch => CanProduce;
        public bool IsHosted => Role == PlayerRole.AiAlly;
        public bool HasDirectCombatAi => Role == PlayerRole.WaveAttacker || Role == PlayerRole.SimulationSide;

        public bool Has(BattleFeatures feature) => (Features & feature) == feature;

        /// <param name="productionHost">Required for <see cref="PlayerRole.AiAlly"/>; ignored for other roles.</param>
        public BattlePlayer(PlayerSlot slot, PlayerRole role, BattleFeatures features, PlayerId productionHost = default)
        {
            Slot = slot;
            Role = role;
            Features = role == PlayerRole.WaveAttacker || role == PlayerRole.SimulationSide
                ? features & ~(BattleFeatures.Economy | BattleFeatures.SuperWeapons | BattleFeatures.MiningSites)
                : features;
            ProductionHostId = role == PlayerRole.AiAlly ? productionHost : slot.Id;
        }
    }
}
