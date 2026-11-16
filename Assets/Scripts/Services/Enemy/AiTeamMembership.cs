using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>Registers an AI player's team membership on scope init and removes it on disposal.</summary>
    public sealed class AiTeamMembership : IInitializable, ILateDisposable
    {
        private readonly PlayerSlot _owner;
        private readonly TeamHostileIntelService _intelService;
        private readonly TeamAiPictureRegistry _pictureRegistry;

        public AiTeamMembership(
            PlayerSlot owner,
            TeamHostileIntelService intelService,
            TeamAiPictureRegistry pictureRegistry)
        {
            _owner = owner;
            _intelService = intelService;
            _pictureRegistry = pictureRegistry;
        }

        public void Initialize()
        {
            _intelService.RegisterPlayer(_owner);
            _pictureRegistry.GetOrCreate(_owner);
        }

        public void LateDispose()
        {
            _intelService.UnregisterPlayer(_owner);
        }
    }
}
