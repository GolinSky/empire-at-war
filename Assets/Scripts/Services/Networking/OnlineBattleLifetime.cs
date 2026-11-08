using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Networking.Online;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// The online session ends with its battle scene, so the next battle starts offline — unless the match is
    /// restarting, when every PC reloads the battle and stays connected.
    /// </summary>
    public sealed class OnlineBattleLifetime : ILateDisposable
    {
        private readonly OnlineMatchService _online;

        public OnlineBattleLifetime(OnlineMatchService online)
        {
            _online = online;
        }

        public void LateDispose()
        {
            if (!_online.IsReloading) _online.Leave();
        }
    }
}
