using System.Threading;
using EmpireAtWar.Services.Networking.Online;
using UnityEngine;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>Client: reports loaded, then waits for the host's begin message (sent after the baseline spawns).</summary>
    public sealed class ClientBattleStartGate : IBattleStartGate
    {
        private readonly OnlineMatchService _online;

        public ClientBattleStartGate(OnlineMatchService online)
        {
            _online = online;
        }

        public async Awaitable WaitAsync(CancellationToken cancellationToken)
        {
            _online.NotifyLocalLoaded();
            while (!_online.HasBegun) await Awaitable.NextFrameAsync(cancellationToken);
        }
    }
}
