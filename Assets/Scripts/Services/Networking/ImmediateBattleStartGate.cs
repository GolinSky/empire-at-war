using System.Threading;
using UnityEngine;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>Offline: nothing to wait for.</summary>
    public sealed class ImmediateBattleStartGate : IBattleStartGate
    {
        public async Awaitable WaitAsync(CancellationToken cancellationToken)
        {
            await Awaitable.NextFrameAsync(cancellationToken);
        }
    }
}
