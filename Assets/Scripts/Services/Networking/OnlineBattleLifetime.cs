using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Networking.Online;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>The online session ends with its battle scene, so the next battle starts offline.</summary>
    public sealed class OnlineBattleLifetime : ILateDisposable
    {
        private readonly OnlineMatchService _online;

        public OnlineBattleLifetime(OnlineMatchService online)
        {
            _online = online;
        }

        public void LateDispose() => _online.Leave();
    }
}
