using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Entities.Game
{
    public sealed class BattlePlayer
    {
        public PlayerSlot Slot { get; }
        public PlayerRole Role { get; }
        public BattleFeatures Features { get; }
        public bool OwnsStation => Role == PlayerRole.Human || Role == PlayerRole.AiOpponent;
        public bool CanProduce => Role != PlayerRole.WaveAttacker && Role != PlayerRole.SimulationSide;
        public bool CanResearch => CanProduce;

        public bool Has(BattleFeatures feature) => (Features & feature) == feature;

        public BattlePlayer(PlayerSlot slot, PlayerRole role, BattleFeatures features)
        {
            Slot = slot;
            Role = role;
            Features = role == PlayerRole.WaveAttacker || role == PlayerRole.SimulationSide
                ? features & ~(BattleFeatures.Economy | BattleFeatures.SuperWeapons | BattleFeatures.MiningSites)
                : features;
        }
    }
}
